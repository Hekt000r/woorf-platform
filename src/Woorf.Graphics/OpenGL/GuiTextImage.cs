using System.Runtime.InteropServices;

namespace Woorf.Graphics.OpenGL;

internal static class GuiTextImage
{
    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public uint Size; public int Width, Height; public ushort Planes, Bits;
        public uint Compression, ImageSize; public int XPixels, YPixels; public uint Used, Important;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Rectangle { public int Left, Top, Right, Bottom; }
    [DllImport("gdi32")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32")] private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint usage, out nint bits, nint section, uint offset);
    [DllImport("gdi32")] private static extern nint SelectObject(nint dc, nint value);
    [DllImport("gdi32")] private static extern bool DeleteObject(nint value);
    [DllImport("gdi32")] private static extern uint SetTextColor(nint dc, uint color);
    [DllImport("gdi32")] private static extern int SetBkMode(nint dc, int mode);
    [DllImport("gdi32", CharSet = CharSet.Unicode)] private static extern nint CreateFontW(int height, int width, int escapement, int orientation,
        int weight, uint italic, uint underline, uint strike, uint charset, uint precision, uint clip, uint quality, uint family, string face);
    [DllImport("user32", CharSet = CharSet.Unicode)] private static extern int DrawTextW(nint dc, string text, int count, ref Rectangle rectangle, uint flags);

    internal static SkyImage Create(GuiDrawItem item)
    {
        var width = Math.Clamp((int)MathF.Ceiling(item.Bounds.Width), 1, 8192);
        var height = Math.Clamp((int)MathF.Ceiling(item.Bounds.Height), 1, 8192);
        var dc = CreateCompatibleDC(0);
        if (dc == 0) throw new InvalidOperationException("Could not create GUI text context.");
        nint bitmap = 0, font = 0, previousBitmap = 0, previousFont = 0;
        try
        {
            var info = new BitmapInfo { Size = 40, Width = width, Height = -height, Planes = 1, Bits = 32 };
            bitmap = CreateDIBSection(dc, ref info, 0, out var bits, 0, 0);
            if (bitmap == 0) throw new InvalidOperationException("Could not allocate GUI text bitmap.");
            var pixels = new byte[checked(width * height * 4)]; Marshal.Copy(pixels, 0, bits, pixels.Length);
            previousBitmap = SelectObject(dc, bitmap);
            var face = item.Font is "Code" ? "Consolas" : item.Font is "SourceSans" or "SourceSansBold" or "SourceSansItalic" ? "Segoe UI" : "Arial";
            var isBold = item.Bold || item.Font.EndsWith("Bold", StringComparison.Ordinal);
            var isItalic = item.Italic || item.Font.EndsWith("Italic", StringComparison.Ordinal);
            font = CreateFontW(-Math.Clamp((int)MathF.Round(item.TextSize), 1, 512), 0, 0, 0, isBold ? 700 : 400,
                isItalic ? 1u : 0u, item.Underline ? 1u : 0u, item.Strikethrough ? 1u : 0u, 1, 0, 0, 4, 0, face);
            if (font == 0) throw new InvalidOperationException("Could not create GUI font.");
            previousFont = SelectObject(dc, font); SetTextColor(dc, 0xFFFFFF); SetBkMode(dc, 1);
            var flags = 0x800u | (item.Wrapped ? 0x10u : 0x20u);
            flags |= item.XAlignment == "Center" ? 1u : item.XAlignment == "Right" ? 2u : 0u;
            var measured = new Rectangle { Right = width, Bottom = height };
            DrawTextW(dc, item.Content, item.Content.Length, ref measured, flags | 0x400);
            var top = item.YAlignment == "Center" ? (height - measured.Bottom) / 2 : item.YAlignment == "Bottom" ? height - measured.Bottom : 0;
            var rectangle = new Rectangle { Top = top, Right = width, Bottom = height };
            DrawTextW(dc, item.Content, item.Content.Length, ref rectangle, flags);
            Marshal.Copy(bits, pixels, 0, pixels.Length);
            for (var index = 0; index < pixels.Length; index += 4)
            {
                var alpha = Math.Max(pixels[index], Math.Max(pixels[index + 1], pixels[index + 2]));
                pixels[index] = 255; pixels[index + 1] = 255; pixels[index + 2] = 255; pixels[index + 3] = alpha;
            }
            return new(width, height, pixels);
        }
        finally
        {
            if (previousFont != 0) SelectObject(dc, previousFont);
            if (previousBitmap != 0) SelectObject(dc, previousBitmap);
            if (font != 0) DeleteObject(font); if (bitmap != 0) DeleteObject(bitmap); DeleteDC(dc);
        }
    }
}
