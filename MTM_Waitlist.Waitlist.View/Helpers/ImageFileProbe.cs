using System.Buffers.Binary;

using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// Answers whether a file on disk actually carries a picture, by reading the picture's own header.
/// </summary>
/// <remarks>
/// <para>
/// The image-location service answers with a <em>path</em>, and a path existing has been taken as proof that
/// there is a picture at the end of it. That is a different question. `Assets/RequestTypes/*.png` ship as
/// 68-byte single-white-pixel stand-ins for artwork that was never delivered, so an override that points at one
/// passes every check the resolver makes and still has nothing to show: the card took it and replaced its own
/// picture with a blank tile. Existence is not the question — "is there a picture here?" is.
/// </para>
/// <para>
/// The size that counts is the app's own floor for a picture: <see cref="ImageValidationRules.MinDimensionPixels"/>,
/// the smallest image the image screens will accept for upload. Art that the app itself would refuse to take as a
/// picture is not a picture worth drawing on a 96x96 tile either, so the two rules agree about what a picture is.
/// </para>
/// <para>
/// The header is read directly rather than decoded, so this stays synchronous, does not touch WinRT, and cannot
/// fail for reason of which thread it runs on: the list resolves every row of every load. PNG, JPEG, GIF and BMP
/// headers are understood. A file in no recognised format — or one whose frame header lies further into the file
/// than <see cref="HeaderBytesToRead"/>, which only a picture carrying an unusually large metadata block could do —
/// is not proven to carry a picture and is refused.
/// </para>
/// </remarks>
public static class ImageFileProbe
{
    /// <summary>
    /// How much of a file is read to find its picture header. Generous by design: a JPEG's frame header sits after
    /// its metadata segments, so the read has to clear an EXIF block or an embedded colour profile.
    /// </summary>
    private const int HeaderBytesToRead = 64 * 1024;

    private static readonly ImageValidationRules Rules = new();

    /// <summary>
    /// The smallest width and height the app accepts as a picture (48 pixels — the app's own preview floor).
    /// </summary>
    public static int MinimumPixels => Rules.MinDimensionPixels;

    /// <summary>
    /// Whether the file at <paramref name="path"/> carries a picture at least <see cref="MinimumPixels"/> pixels
    /// on both sides.
    /// </summary>
    /// <param name="path">A rooted path, or one relative to the application directory.</param>
    /// <returns><see langword="true"/> only when a picture was found and measured.</returns>
    public static bool CarriesPicture(string? path) => CarriesPicture(path, MinimumPixels);

    /// <summary>
    /// Whether the file at <paramref name="path"/> carries a picture at least
    /// <paramref name="minimumPixels"/> pixels on both sides.
    /// </summary>
    /// <param name="path">A rooted path, or one relative to the application directory.</param>
    /// <param name="minimumPixels">The smallest acceptable width and height, in pixels.</param>
    /// <returns><see langword="true"/> only when a picture was found and measured.</returns>
    public static bool CarriesPicture(string? path, int minimumPixels)
    {
        if (minimumPixels < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumPixels), minimumPixels, "A picture cannot be measured against a floor below one pixel.");
        }

        var file = ResolveExistingFile(path);
        if (file is null)
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var header = new byte[Math.Min(stream.Length, HeaderBytesToRead)];
            stream.ReadExactly(header, 0, header.Length);

            return TryReadDimensions(header, out var width, out var height)
                && width >= minimumPixels
                && height >= minimumPixels;
        }
        catch (IOException)
        {
            // A file that cannot be read cannot be shown, which is the answer this method exists to give.
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves a path the way the image resolver and the card's converter do: a rooted path is used as it is, and
    /// a relative one is taken against the application directory. Separators are unified first, because the
    /// service hands back backslashes for its own defaults while resolved catalog paths use forward slashes.
    /// </summary>
    /// <param name="path">The path to resolve, or null.</param>
    /// <returns>The file, or null when there is no file to read.</returns>
    private static string? ResolveExistingFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var candidate = path.Trim()
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        var file = Path.IsPathRooted(candidate)
            ? candidate
            : Path.Combine(AppContext.BaseDirectory, candidate);

        return File.Exists(file) ? file : null;
    }

    /// <summary>
    /// Reads a picture's pixel dimensions out of its header, in the formats this probe understands.
    /// </summary>
    /// <param name="header">The first bytes of the file.</param>
    /// <param name="width">The measured width, when a header was recognised.</param>
    /// <param name="height">The measured height, when a header was recognised.</param>
    /// <returns><see langword="true"/> when a picture header was found and measured.</returns>
    private static bool TryReadDimensions(ReadOnlySpan<byte> header, out int width, out int height)
        => TryReadPng(header, out width, out height)
            || TryReadGif(header, out width, out height)
            || TryReadBmp(header, out width, out height)
            || TryReadJpeg(header, out width, out height);

    /// <summary>PNG: the eight-byte signature, then the IHDR chunk's big-endian width and height.</summary>
    private static bool TryReadPng(ReadOnlySpan<byte> header, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (header.Length < 24
            || header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47
            || header[4] != 0x0D || header[5] != 0x0A || header[6] != 0x1A || header[7] != 0x0A)
        {
            return false;
        }

        // The first chunk of a valid PNG is always IHDR, whose payload starts with width then height.
        if (header[12] != (byte)'I' || header[13] != (byte)'H' || header[14] != (byte)'D' || header[15] != (byte)'R')
        {
            return false;
        }

        width = BinaryPrimitives.ReadInt32BigEndian(header.Slice(16, 4));
        height = BinaryPrimitives.ReadInt32BigEndian(header.Slice(20, 4));
        return true;
    }

    /// <summary>GIF: the version tag, then little-endian width and height.</summary>
    private static bool TryReadGif(ReadOnlySpan<byte> header, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (header.Length < 10)
        {
            return false;
        }

        var signature = System.Text.Encoding.ASCII.GetString(header.Slice(0, 6));
        if (!string.Equals(signature, "GIF87a", StringComparison.Ordinal)
            && !string.Equals(signature, "GIF89a", StringComparison.Ordinal))
        {
            return false;
        }

        width = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(6, 2));
        height = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(8, 2));
        return true;
    }

    /// <summary>BMP: the file header, then the DIB header's width and height (a negative height means top-down).</summary>
    private static bool TryReadBmp(ReadOnlySpan<byte> header, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (header.Length < 26 || header[0] != (byte)'B' || header[1] != (byte)'M')
        {
            return false;
        }

        width = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(18, 4));
        height = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(header.Slice(22, 4)));
        return true;
    }

    /// <summary>
    /// JPEG: walk the segment markers from the start-of-image to the frame header that carries the dimensions,
    /// skipping the metadata segments in between.
    /// </summary>
    private static bool TryReadJpeg(ReadOnlySpan<byte> header, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (header.Length < 4 || header[0] != 0xFF || header[1] != 0xD8)
        {
            return false;
        }

        var index = 2;
        while (index + 1 < header.Length)
        {
            if (header[index] != 0xFF)
            {
                // Not a marker: resynchronise rather than give up, because padding between segments is legal.
                index++;
                continue;
            }

            var marker = header[index + 1];
            if (marker == 0xFF)
            {
                // A fill byte, which a segment may be preceded by any number of.
                index++;
                continue;
            }

            // Markers with no payload: the start of image, the restart markers, and the reserved 0x01.
            if (marker == 0xD8 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
            {
                index += 2;
                continue;
            }

            // The end of the image, reached without ever meeting a frame header.
            if (marker == 0xD9)
            {
                return false;
            }

            if (index + 4 > header.Length)
            {
                return false;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(index + 2, 2));
            if (segmentLength < 2)
            {
                return false;
            }

            // Everything from C0 to CF is a frame header except the Huffman and arithmetic tables and the
            // reserved CC, and every frame header carries height then width after its precision byte.
            var isFrameHeader = marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
            if (isFrameHeader)
            {
                if (index + 9 > header.Length)
                {
                    return false;
                }

                height = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(index + 5, 2));
                width = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(index + 7, 2));
                return true;
            }

            index += 2 + segmentLength;
        }

        return false;
    }
}
