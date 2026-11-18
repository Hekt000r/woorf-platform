using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed partial class GuiScene
{
    private void DrawImage(GuiInstance item, GuiRect bounds, GuiRect clip, float opacity, float radius, float rotation, float scale)
    {
        var tile = Value(item, "TileSize", UDim2.FromScale(1, 1));
        commands.Add(new("Image", bounds, clip, ButtonColor(item, Value(item, "ImageColor3", new Color3(1, 1, 1))),
            opacity * (1 - Number(item, "ImageTransparency")), Value(item, "Image", ""), Radius: radius, Rotation: rotation,
            ImageOffset: Value(item, "ImageRectOffset", new Vector2()), ImageSize: Value(item, "ImageRectSize", new Vector2()),
            ScaleType: EnumName(item, "ScaleType", "Stretch"), TileSize: new(Resolve(tile.X, bounds.Width, scale), Resolve(tile.Y, bounds.Height, scale))));
    }
}
