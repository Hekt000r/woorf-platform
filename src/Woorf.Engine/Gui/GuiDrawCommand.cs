using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public readonly record struct GuiRect(float X, float Y, float Width, float Height)
{
    public bool Contains(float x, float y) => Width > 0 && Height > 0 && x >= X && y >= Y && x < X + Width && y < Y + Height;
    public GuiRect Intersect(GuiRect other)
    {
        var x = Math.Max(X, other.X); var y = Math.Max(Y, other.Y);
        return new(x, y, Math.Max(0, Math.Min(X + Width, other.X + other.Width) - x),
            Math.Max(0, Math.Min(Y + Height, other.Y + other.Height) - y));
    }
}

public sealed record GuiDrawCommand(string Kind, GuiRect Bounds, GuiRect Clip, Color3 Color, float Opacity,
    string Content = "", float TextSize = 14, string XAlignment = "Center", string YAlignment = "Center",
    bool Wrapped = false, float Radius = 0, float Rotation = 0, string Font = "Arial",
    bool Bold = false, bool Italic = false, bool Underline = false, bool Strikethrough = false,
    Vector2 ImageOffset = default, Vector2 ImageSize = default, string ScaleType = "Stretch", Vector2 TileSize = default,
    GuiGradient? Gradient = null, GuiRect GradientBounds = default);

internal sealed record GuiHitTarget(GuiInstance Instance, GuiRect Bounds, GuiRect Clip);
