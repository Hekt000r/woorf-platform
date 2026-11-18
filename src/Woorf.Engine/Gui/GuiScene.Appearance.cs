using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed partial class GuiScene
{
    private void DrawAppearance(GuiInstance item, GuiRect bounds, GuiRect clip, float opacity, float scale)
    {
        var decorators = item.GetChildren().OfType<GuiInstance>().ToArray();
        var corner = decorators.FirstOrDefault(child => child.ClassName == "UICorner");
        var radius = corner is null ? 0 : Resolve(Value(corner, "CornerRadius", new UDim(0, 8)), Math.Min(bounds.Width, bounds.Height), scale);
        var rotation = Number(item, "Rotation");
        var border = Number(item, "BorderSizePixel", 1) * scale;
        if (border > 0)
            commands.Add(new("Rectangle", new(bounds.X - border, bounds.Y - border, bounds.Width + border * 2, bounds.Height + border * 2),
                clip, Value(item, "BorderColor3", new Color3()), opacity * (1 - Number(item, "BackgroundTransparency")), Radius: radius, Rotation: rotation));
        commands.Add(new("Rectangle", bounds, clip, ButtonColor(item, Value(item, "BackgroundColor3", new Color3(1, 1, 1))),
            opacity * (1 - Number(item, "BackgroundTransparency")), Radius: radius, Rotation: rotation));
        if (item.HasGuiProperty("Image"))
            DrawImage(item, bounds, clip, opacity, radius, rotation, scale);
        if (item.HasGuiProperty("Text")) DrawText(item, bounds, clip, opacity, scale, rotation);
        var stroke = decorators.FirstOrDefault(child => child.ClassName == "UIStroke" && Value(child, "Enabled", true));
        if (stroke is not null)
        {
            var thickness = Number(stroke, "Thickness", 1) * scale;
            var color = Value(stroke, "Color", new Color3()); var alpha = opacity * (1 - Number(stroke, "Transparency"));
            commands.Add(new("Rectangle", new(bounds.X, bounds.Y, bounds.Width, thickness), clip, color, alpha));
            commands.Add(new("Rectangle", new(bounds.X, bounds.Y + bounds.Height - thickness, bounds.Width, thickness), clip, color, alpha));
            commands.Add(new("Rectangle", new(bounds.X, bounds.Y, thickness, bounds.Height), clip, color, alpha));
            commands.Add(new("Rectangle", new(bounds.X + bounds.Width - thickness, bounds.Y, thickness, bounds.Height), clip, color, alpha));
        }
    }

    private Color3 ButtonColor(GuiInstance item, Color3 color)
    {
        if (!item.IsA("GuiButton") || !Value(item, "AutoButtonColor", true) || !Value(item, "Active", true)) return color;
        var factor = pressed == item ? 0.7f : hovered == item || hovered?.IsDescendantOf(item) == true ? 0.9f : 1;
        return new(color.R * factor, color.G * factor, color.B * factor);
    }

}
