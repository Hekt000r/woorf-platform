namespace Woorf.Graphics;

internal static class FrameCapture
{
    internal static void WriteBitmap(string path, int width, int height, byte[] pixels)
    {
        var rowBytes = checked(width * 3);
        var stride = (rowBytes + 3) & ~3;
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0x4D42); writer.Write(checked(54 + stride * height)); writer.Write(0); writer.Write(54);
        writer.Write(40); writer.Write(width); writer.Write(height); writer.Write((ushort)1); writer.Write((ushort)24);
        writer.Write(0); writer.Write(stride * height); writer.Write(2835); writer.Write(2835); writer.Write(0); writer.Write(0);
        for (var row = 0; row < height; row++)
        {
            writer.Write(pixels, row * rowBytes, rowBytes);
            for (var padding = rowBytes; padding < stride; padding++) writer.Write((byte)0);
        }
    }
}
