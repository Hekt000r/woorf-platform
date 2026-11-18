using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed partial class GuiScene
{
    private static GuiRect MeasureCanvas(GuiInstance item, GuiRect bounds, float scale)
    {
        var canvas = Value(item, "CanvasSize", UDim2.FromScale(0, 2));
        var width = Math.Max(bounds.Width, Resolve(canvas.X, bounds.Width, scale));
        var height = Math.Max(bounds.Height, Resolve(canvas.Y, bounds.Height, scale));
        var mode = EnumName(item, "AutomaticCanvasSize", "None");
        if (mode != "None")
        {
            var content = MeasureContent(item, new(0, 0, width, height), scale);
            if (mode is "X" or "XY") width = Math.Max(width, content.X);
            if (mode is "Y" or "XY") height = Math.Max(height, content.Y);
        }
        var offset = Value(item, "CanvasPosition", new Vector2());
        offset = new(Math.Clamp(offset.X, 0, Math.Max(0, width - bounds.Width)), Math.Clamp(offset.Y, 0, Math.Max(0, height - bounds.Height)));
        item.SetGuiProperty("CanvasPosition", offset);
        item.SetComputedProperty("AbsoluteCanvasSize", new Vector2(width, height));
        var gutter = Number(item, "ScrollBarThickness", 12) * scale;
        item.SetComputedProperty("AbsoluteWindowSize", new Vector2(Math.Max(0, bounds.Width - (height > bounds.Height ? gutter : 0)),
            Math.Max(0, bounds.Height - (width > bounds.Width ? gutter : 0))));
        return new(bounds.X - offset.X, bounds.Y - offset.Y, width, height);
    }

    private void DrawScrollBars(GuiInstance item, GuiRect bounds, GuiRect clip, float opacity)
    {
        var canvas = Value(item, "AbsoluteCanvasSize", new Vector2());
        var offset = Value(item, "CanvasPosition", new Vector2());
        var thickness = Math.Min(Number(item, "ScrollBarThickness", 12), Math.Min(bounds.Width, bounds.Height));
        var color = Value(item, "ScrollBarImageColor3", new Color3());
        var alpha = opacity * (1 - Number(item, "ScrollBarImageTransparency"));
        if (thickness <= 0 || alpha <= 0 || !Value(item, "ScrollingEnabled", true)) return;
        var direction = EnumName(item, "ScrollingDirection", "XY");
        if (canvas.Y > bounds.Height && direction != "X")
        {
            var height = Math.Max(thickness, bounds.Height * bounds.Height / canvas.Y);
            var y = bounds.Y + offset.Y / (canvas.Y - bounds.Height) * (bounds.Height - height);
            commands.Add(new("Rectangle", new(bounds.X + bounds.Width - thickness, y, thickness, height), clip.Intersect(bounds), color, alpha));
        }
        if (canvas.X > bounds.Width && direction != "Y")
        {
            var width = Math.Max(thickness, bounds.Width * bounds.Width / canvas.X);
            var x = bounds.X + offset.X / (canvas.X - bounds.Width) * (bounds.Width - width);
            commands.Add(new("Rectangle", new(x, bounds.Y + bounds.Height - thickness, width, thickness), clip.Intersect(bounds), color, alpha));
        }
    }
}
