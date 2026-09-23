using System.Buffers.Binary;

namespace MTM_Waitlist.Module_Shared.Helpers;

/// <summary>
/// The one question the whole application asks about a picture file — "is this a picture I can draw?" — and the
/// one answer it falls back to when the answer is no.
/// </summary>
/// <remarks>
/// <para>
/// Every image surface used to answer this for itself, and the answers disagreed: a path existing was taken as
/// proof that a picture was at the end of it, so a missing file, an empty file and a file that never shipped all
/// drew the same thing — nothing. The application's own floor for an accepted picture already lives in the
/// settings image rules (square, at least <see cref="MinimumPixels"/> pixels a side), because that is the shape
/// the settings screens will accept from a person. Artwork the application would refuse to accept is not artwork
/// worth drawing on a card either, so the two rules are stated once, here, and every surface asks this.
/// </para>
/// <para>
/// The file's own header is read rather than the picture being decoded, so this stays synchronous, does not touch
/// WinRT, and cannot fail for reason of which thread it runs on: the waitlist resolves every row of every load.
/// PNG, JPEG, GIF and BMP headers are understood. A file in no recognised format — or one whose frame header sits
/// further into the file than <see cref="HeaderBytesToRead"/> — cannot be proven to carry a picture and is
/// refused.
/// </para>
/// <para>
/// What "no" means is a <em>drawing</em> decision, not a data one: the caller draws
/// <see cref="NoImagePath"/>. Nothing here substitutes data or hides a failure; the surfaces that must report
/// an unavailable store still do.
/// </para>
/// </remarks>
public static class ImagePicturePolicy
{
    /// <summary>
    /// The single picture every image surface draws when it has nothing usable of its own: no image configured,
    /// the configured file not found, or the configured file holding no picture.
    /// </summary>
    public const string NoImagePath = "Assets/Placeholders/default-no-image.png";

    /// <summary>
    /// The same placeholder as a package URI, which is how it is reached when the deployment folder cannot be
    /// read (a packaged host, or a probe that runs before the app directory is known).
    /// </summary>
    public const string NoImagePackUri = "ms-appx:///Assets/Placeholders/default-no-image.png";

    /// <summary>The smallest width and height the application accepts as a picture (48 pixels — the app's own preview floor).</summary>
    public const int MinimumPixels = 48;

    /// <summary>
    /// The aspect ratio a picture must have to be accepted. The application's image screens only accept square
    /// pictures, so a picture drawn anywhere else is held to the same shape.
    /// </summary>
    public const double TargetAspectRatio = 1.0;

    /// <summary>
    /// How far a picture's aspect ratio may sit from <see cref="TargetAspectRatio"/> and still count as square
    /// (±2%, the tolerance the settings image rules use so a rounding off-by-one is not rejected).
    /// </summary>
    public const double AspectRatioTolerance = 0.02;

    /// <summary>
    /// How much of a file is read to find its picture header. Generous by design: a JPEG's frame header sits after
    /// its metadata segments, so the read has to clear an EXIF block or an embedded colour profile.
    /// </summary>
    private const int HeaderBytesToRead = 64 * 1024;

    /// <summary>
    /// Whether the file at <paramref name="path"/> is a picture this application will draw: it exists, its header
    /// can be read, it is square, and it is at least <see cref="MinimumPixels"/> pixels on both sides.
    /// </summary>
    /// <param name="path">A rooted path, or one relative to the application directory. Null or blank is not a picture.</param>
    /// <returns><see langword="true"/> only when a picture was found and measured.</returns>
    public static bool IsUsable(string? path) =>
        TryReadSize(path, out var width, out var height) && IsUsableSize(width, height);

    /// <summary>
    /// The measurement half of the rule, stated on its own so a caller that already holds the dimensions — a
    /// header reader, a decoder, an upload picker — does not have to read the file again to reach the answer.
    /// </summary>
    /// <param name="width">The picture's width in pixels.</param>
    /// <param name="height">The picture's height in pixels.</param>
    /// <returns><see langword="true"/> when the frame is large enough and square.</returns>
    public static bool IsUsableSize(int width, int height) =>
        height > 0
        && width >= MinimumPixels
        && height >= MinimumPixels
        && Math.Abs(((double)width / height) - TargetAspectRatio) <= AspectRatioTolerance;

    /// <summary>
    /// Whether the file at <paramref name="path"/> carries a picture at least <see cref="MinimumPixels"/> pixels
    /// on both sides, whatever its shape.
    /// </summary>
    /// <param name="path">A rooted path, or one relative to the application directory.</param>
    /// <returns><see langword="true"/> only when a picture was found and measured.</returns>
    public static bool CarriesPicture(string? path) => CarriesPicture(path, MinimumPixels);

    /// <summary>
    /// Whether the file at <paramref name="path"/> carries a picture at least
    /// <paramref name="minimumPixels"/> pixels on both sides, whatever its shape.
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

        return TryReadSize(path, out var width, out var height)
            && width >= minimumPixels
            && height >= minimumPixels;
    }

    /// <summary>
    /// Resolves <paramref name="path"/> to a usable picture file, or answers <see langword="null"/> when there is
    /// no file, or no picture, at the end of it. This is the method a surface that draws a picture source calls:
    /// one question, one answer, whichever way the empty answer is spelled.
    /// </summary>
    /// <param name="path">A rooted path, or one relative to the application directory.</param>
    /// <returns>The absolute path of a usable picture, or <see langword="null"/>.</returns>
    public static string? ResolveUsableFile(string? path) =>
        IsUsable(path) ? ResolveExistingFile(path) : null;

    /// <summary>
    /// Resolves a path the way the image resolver and the surfaces' converters do: a rooted path is used as it is,
    /// and a relative one is taken against the application directory. Separators are unified first, because the
    /// image service hands back backslashes for its own defaults while resolved catalog paths use forward slashes.
    /// </summary>
    /// <param name="path">The path to resolve, or null.</param>
    /// <returns>The absolute file path, or <see langword="null"/> when there is no file to read.</returns>
    /// <remarks>
    /// This answers <em>existence</em> only. The fallback picture is resolved this way on purpose: the placeholder
    /// must be reachable even if it is not itself a picture the rule would accept, or a bad placeholder would
    /// leave the surface with nothing to draw at all.
    /// </remarks>
    public static string? ResolveExistingFile(string? path)
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
    /// Measures the picture at <paramref name="path"/> from its header, without decoding it.
    /// </summary>
    /// <param name="path">A rooted path, or one relative to the application directory.</param>
    /// <param name="width">The measured width, when a header was recognised.</param>
    /// <param name="height">The measured height, when a header was recognised.</param>
    /// <returns><see langword="true"/> when a picture header was found and measured.</returns>
    public static bool TryReadSize(string? path, out int width, out int height)
    {
        width = 0;
        height = 0;

        var file = ResolveExistingFile(path);
        if (file is null)
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length < 1)
            {
                // An empty file is the plainest form of "no picture", and reading a header out of it would only
                // report the same answer more slowly.
                return false;
            }

            var header = new byte[Math.Min(stream.Length, HeaderBytesToRead)];
            stream.ReadExactly(header, 0, header.Length);

            return TryReadDimensions(header, out width, out height);
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
    /// Reads a picture's pixel dimensions out of its header, in the formats this rule understands.
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
