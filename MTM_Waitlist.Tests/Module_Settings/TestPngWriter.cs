using System.Buffers.Binary;
using System.IO.Compression;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// Writes minimal, genuinely decodable 8-bit grayscale PNG files so image validation
/// can be tested against real dimensions instead of stubbed metadata.
/// </summary>
internal static class TestPngWriter
{
    private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    /// The validator rejects files under 1 KB, so every fixture is padded past that floor
    /// with an ancillary chunk that decoders ignore.
    /// </summary>
    private const long DefaultMinimumBytes = 2048;

    public static void Write(string filePath, int width, int height, long minimumBytes = DefaultMinimumBytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
        header[8] = 8;  // bit depth
        header[9] = 0;  // grayscale
        header[10] = 0; // deflate
        header[11] = 0; // adaptive filtering
        header[12] = 0; // no interlace

        var raw = new byte[height * (width + 1)];
        var random = new Random(width * 7919 + height);
        for (var row = 0; row < height; row++)
        {
            var rowStart = row * (width + 1);
            raw[rowStart] = 0; // filter type: none
            for (var column = 0; column < width; column++)
            {
                raw[rowStart + 1 + column] = (byte)random.Next(256);
            }
        }

        using var compressed = new MemoryStream();
        using (var deflate = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            deflate.Write(raw, 0, raw.Length);
        }

        var imageData = compressed.ToArray();

        // 8 signature + (12 + 13) IHDR + (12 + data) IDAT + 12 IEND
        var sizeWithoutPadding = 8 + 25 + 12 + imageData.Length + 12;
        var paddingLength = Math.Max(0, minimumBytes - sizeWithoutPadding - 12);

        using var output = File.Create(filePath);
        output.Write(Signature);
        WriteChunk(output, "IHDR", header);
        WriteChunk(output, "IDAT", imageData);

        if (paddingLength > 0)
        {
            WriteChunk(output, "teXt", new byte[paddingLength]);
        }

        WriteChunk(output, "IEND", Array.Empty<byte>());
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        var lengthBuffer = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, data.Length);
        output.Write(lengthBuffer);

        var typeBytes = new[] { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] };
        output.Write(typeBytes);
        output.Write(data);

        var crcBuffer = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBuffer, Crc32(typeBytes, data));
        output.Write(crcBuffer);
    }

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in type)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        foreach (var b in data)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFFu;
    }
}
