using System.Buffers.Binary;

namespace MTM_Waitlist.Tests.Module_Waitlist.Helpers;

/// <summary>
/// Picture fixtures for the card's image rules.
/// </summary>
/// <remarks>
/// The rules being tested read a picture's <em>header</em> to find its dimensions, so every fixture here is a file
/// whose header is real: the PNG fixtures are genuinely decodable files written by
/// <see cref="MTM_Waitlist.Tests.Module_Settings.TestPngWriter"/>, and the JPEG, GIF and BMP fixtures are the
/// marker and field sequences those formats start with. Nothing decodes them, and nothing needs to.
/// </remarks>
internal static class ImageHeaderFixtures
{
    /// <summary>
    /// The 68-byte single-white-pixel PNG that ships as each <c>Assets/RequestTypes/*.png</c>: a stand-in for
    /// artwork that was never delivered, reproduced here byte for byte so the rule is tested against the real
    /// artifact rather than against a description of it.
    /// </summary>
    private static readonly byte[] ShippedSinglePixelPng =
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x04, 0x00, 0x00, 0x00, 0xB5, 0x1C, 0x0C, 0x02,
        0x00, 0x00, 0x00, 0x0B, 0x49, 0x44, 0x41, 0x54,
        0x78, 0xDA, 0x63, 0xFC, 0xFF, 0x1F, 0x00, 0x03, 0x03, 0x02, 0x00,
        0xEF, 0x56, 0x97, 0xA9,
        0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
        0xAE, 0x42, 0x60, 0x82,
    };

    /// <summary>Creates an empty directory for one test's fixtures.</summary>
    public static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "mtm-image-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>Removes a directory created by <see cref="CreateTemporaryDirectory"/>.</summary>
    public static void DeleteTemporaryDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>A byte-for-byte copy of the single-pixel PNG that ships in <c>Assets/RequestTypes/</c>.</summary>
    public static void WriteShippedSinglePixelPng(string filePath) => File.WriteAllBytes(filePath, ShippedSinglePixelPng);

    /// <summary>A genuinely decodable PNG of the given size.</summary>
    public static void WritePng(string filePath, int width, int height) =>
        MTM_Waitlist.Tests.Module_Settings.TestPngWriter.Write(filePath, width, height);

    /// <summary>
    /// The start of a JPEG: the start-of-image marker, a metadata segment, then the frame header carrying the
    /// dimensions. <paramref name="metadataBytes"/> makes that first segment long, which is what a photograph
    /// with an EXIF block looks like and what the reader has to walk past.
    /// </summary>
    public static void WriteJpegHeader(string filePath, int width, int height, int metadataBytes = 0)
    {
        using var stream = File.Create(filePath);
        stream.Write(new byte[] { 0xFF, 0xD8 });

        WriteJpegSegment(stream, 0xE0, new byte[16 + metadataBytes]);

        var frame = new byte[15];
        frame[0] = 8;
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(1, 2), (ushort)height);
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(3, 2), (ushort)width);
        frame[5] = 3;
        WriteJpegSegment(stream, 0xC0, frame);

        stream.Write(new byte[] { 0xFF, 0xD9 });
    }

    /// <summary>The GIF header: the version tag, then little-endian width and height.</summary>
    public static void WriteGifHeader(string filePath, int width, int height)
    {
        using var stream = File.Create(filePath);
        stream.Write(System.Text.Encoding.ASCII.GetBytes("GIF89a"));

        Span<byte> size = stackalloc byte[4];
        BinaryPrimitives.WriteUInt16LittleEndian(size[..2], (ushort)width);
        BinaryPrimitives.WriteUInt16LittleEndian(size[2..], (ushort)height);
        stream.Write(size);
        stream.WriteByte(0);
    }

    /// <summary>The BMP header: the file header, then the DIB header's little-endian width and height.</summary>
    public static void WriteBmpHeader(string filePath, int width, int height)
    {
        using var stream = File.Create(filePath);
        var header = new byte[26];
        header[0] = (byte)'B';
        header[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(18, 4), width);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(22, 4), height);
        stream.Write(header);
    }

    /// <summary>A file that is no kind of picture at all.</summary>
    public static void WriteTextFile(string filePath) => File.WriteAllText(filePath, "this is not a picture");

    private static void WriteJpegSegment(Stream stream, byte marker, byte[] payload)
    {
        stream.WriteByte(0xFF);
        stream.WriteByte(marker);
        stream.WriteByte((byte)((payload.Length + 2) >> 8));
        stream.WriteByte((byte)(payload.Length + 2));
        stream.Write(payload);
    }
}
