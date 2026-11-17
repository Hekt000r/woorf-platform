using System.Buffers.Binary;
using System.IO.Compression;

namespace Woorf.Graphics.OpenGL;

internal sealed record SkyImage(int Width, int Height, byte[] Pixels)
{
    internal static SkyImage Load(string content)
    {
        using var stream = Open(content);
        using var memory = new MemoryStream(); stream.CopyTo(memory);
        var data = memory.ToArray();
        if (data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ReadPng(data);
        if (data.Length >= 54 && data[0] == 'B' && data[1] == 'M') return ReadBitmap(data);
        throw new InvalidDataException("Sky images must be 8-bit RGB/RGBA PNG or uncompressed 24/32-bit BMP.");
    }
    private static Stream Open(string content)
    {
        const string prefix = "rbxasset://textures/sky/";
        if (content.StartsWith(prefix, StringComparison.Ordinal))
        {
            var file = content[prefix.Length..];
            var aliases = new Dictionary<string, string> { ["rt"] = "right", ["lf"] = "left", ["up"] = "up", ["dn"] = "down", ["bk"] = "back", ["ft"] = "front" };
            if (file.StartsWith("sky512_", StringComparison.Ordinal) && file.EndsWith(".tex", StringComparison.Ordinal))
                file = aliases.GetValueOrDefault(file[7..^4], "missing") + ".png";
            return typeof(SkyImage).Assembly.GetManifestResourceStream("Woorf.Graphics.Sky." + file)
                ?? throw new FileNotFoundException("Unknown built-in sky texture.", content);
        }
        if (content.Contains("://", StringComparison.Ordinal))
            throw new InvalidDataException("Only packaged rbxasset:// sky textures and local image paths are supported.");
        return File.OpenRead(content);
    }
    private static void ValidateSize(int width, int height)
    {
        if (width <= 0 || height <= 0 || width > 4096 || height > 4096)
            throw new InvalidDataException("Sky images must be between 1 and 4096 pixels per dimension.");
    }
    private static SkyImage ReadBitmap(byte[] data)
    {
        var width = BitConverter.ToInt32(data, 18); var signedHeight = BitConverter.ToInt32(data, 22);
        var height = Math.Abs((long)signedHeight);
        if (height > 4096) throw new InvalidDataException("Sky image is too large.");
        ValidateSize(width, (int)height);
        var bits = BitConverter.ToUInt16(data, 28); var offset = BitConverter.ToInt32(data, 10);
        if (BitConverter.ToInt32(data, 14) < 40 || BitConverter.ToInt32(data, 30) != 0 || bits is not (24 or 32))
            throw new InvalidDataException("Unsupported BMP encoding.");
        var channels = bits / 8; var stride = checked((width * channels + 3) & ~3);
        if (offset < 54 || (long)offset + stride * height > data.Length) throw new InvalidDataException("Truncated BMP.");
        var pixels = new byte[checked(width * (int)height * 4)];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var source = offset + (signedHeight > 0 ? (int)height - 1 - y : y) * stride + x * channels;
                var target = (y * width + x) * 4;
                pixels[target] = data[source + 2]; pixels[target + 1] = data[source + 1];
                pixels[target + 2] = data[source]; pixels[target + 3] = 255;
            }
        return new(width, (int)height, pixels);
    }
    private static SkyImage ReadPng(byte[] data)
    {
        var width = 0; var height = 0; var channels = 0; var hasEnd = false;
        using var compressed = new MemoryStream();
        for (var offset = 8; offset + 12 <= data.Length;)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset));
            if (length < 0 || (long)offset + length + 12 > data.Length) throw new InvalidDataException("Truncated PNG.");
            var type = System.Text.Encoding.ASCII.GetString(data, offset + 4, 4); var start = offset + 8;
            if (type == "IHDR")
            {
                if (length != 13) throw new InvalidDataException("Invalid PNG header.");
                width = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(start)); height = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(start + 4));
                ValidateSize(width, height);
                channels = data[start + 9] == 2 ? 3 : data[start + 9] == 6 ? 4 : 0;
                if (data[start + 8] != 8 || channels == 0 || data[start + 10] != 0 || data[start + 11] != 0 || data[start + 12] != 0)
                    throw new InvalidDataException("PNG must be noninterlaced 8-bit RGB or RGBA.");
            }
            else if (type == "IDAT") compressed.Write(data, start, length);
            else if (type == "IEND") { hasEnd = true; break; }
            offset += length + 12;
        }
        if (!hasEnd || channels == 0) throw new InvalidDataException("Incomplete PNG.");
        compressed.Position = 0;
        var stride = checked(width * channels); var rows = new byte[checked((stride + 1) * height)];
        using var inflater = new ZLibStream(compressed, CompressionMode.Decompress);
        inflater.ReadExactly(rows);
        if (inflater.ReadByte() != -1) throw new InvalidDataException("Unexpected PNG pixel data.");
        return new(width, height, Unfilter(rows, width, height, channels));
    }
    private static byte[] Unfilter(byte[] rows, int width, int height, int channels)
    {
        var stride = width * channels; var decoded = new byte[stride * height]; var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            var filter = rows[y * (stride + 1)];
            for (var x = 0; x < stride; x++)
            {
                var index = y * stride + x;
                var left = x >= channels ? decoded[index - channels] : 0;
                var above = y > 0 ? decoded[index - stride] : 0;
                var diagonal = y > 0 && x >= channels ? decoded[index - stride - channels] : 0;
                var predictor = filter switch { 0 => 0, 1 => left, 2 => above, 3 => (left + above) / 2, 4 => Paeth(left, above, diagonal), _ => throw new InvalidDataException("Invalid PNG row filter.") };
                decoded[index] = unchecked((byte)(rows[y * (stride + 1) + x + 1] + predictor));
            }
        }
        for (var index = 0; index < width * height; index++)
        {
            Array.Copy(decoded, index * channels, pixels, index * 4, 3);
            pixels[index * 4 + 3] = channels == 4 ? decoded[index * channels + 3] : (byte)255;
        }
        return pixels;
    }
    private static int Paeth(int left, int above, int diagonal)
    {
        var prediction = left + above - diagonal;
        var a = Math.Abs(prediction - left); var b = Math.Abs(prediction - above); var c = Math.Abs(prediction - diagonal);
        return a <= b && a <= c ? left : b <= c ? above : diagonal;
    }
}
