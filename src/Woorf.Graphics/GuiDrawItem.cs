using System.Numerics;

namespace Woorf.Graphics;

public readonly record struct GuiBounds(float X, float Y, float Width, float Height);
public sealed record GuiGradientFill(Vector2 Start, Vector2 End, GuiBounds Bounds, GuiGradientSample[] Stops);
public readonly record struct GuiGradientSample(float Time, Vector4 Color);
public sealed record GuiDrawItem(string Kind, GuiBounds Bounds, GuiBounds Clip, Vector4 Color,
    string Content = "", float TextSize = 14, string XAlignment = "Center", string YAlignment = "Center",
    bool Wrapped = false, float Radius = 0, float Rotation = 0, string Font = "Arial", bool Bold = false,
    bool Italic = false, bool Underline = false, bool Strikethrough = false,
    Vector2 ImageOffset = default, Vector2 ImageSize = default, string ScaleType = "Stretch", Vector2 TileSize = default,
    GuiGradientFill? Gradient = null);
