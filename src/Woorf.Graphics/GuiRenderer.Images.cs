using System.Numerics;

namespace Woorf.Graphics;

public sealed partial class GuiRenderer
{
    private static Vector2 ImageDimensions(GuiDrawItem item, GuiTexture texture) =>
        item.ImageSize.X > 0 && item.ImageSize.Y > 0 ? item.ImageSize : new(texture.Width, texture.Height);

    private static GuiDrawItem FitImage(GuiDrawItem item, GuiTexture texture)
    {
        if (item.Kind != "Image" || item.ScaleType != "Fit") return item;
        var source = ImageDimensions(item, texture); var bounds = item.Bounds;
        var ratio = Math.Min(bounds.Width / source.X, bounds.Height / source.Y);
        var width = source.X * ratio; var height = source.Y * ratio;
        return item with { Bounds = new(bounds.X + (bounds.Width - width) / 2, bounds.Y + (bounds.Height - height) / 2, width, height) };
    }

    private static Vector2 ImageUv(GuiDrawItem item, GuiTexture texture, Vector2 uv)
    {
        if (item.Kind != "Image") return uv;
        var source = ImageDimensions(item, texture); var offset = item.ImageOffset;
        if (item.ScaleType == "Crop")
        {
            var ratio = Math.Max(item.Bounds.Width / source.X, item.Bounds.Height / source.Y);
            var width = item.Bounds.Width / ratio; var height = item.Bounds.Height / ratio;
            offset += new Vector2((source.X - width) / 2, (source.Y - height) / 2); source = new(width, height);
        }
        if (item.ScaleType == "Tile")
        {
            var tile = new Vector2(item.TileSize.X > 0 ? item.TileSize.X : item.Bounds.Width,
                item.TileSize.Y > 0 ? item.TileSize.Y : item.Bounds.Height);
            uv *= new Vector2(item.Bounds.Width, item.Bounds.Height) / tile;
        }
        else if (item.ImageSize.X > 0 && item.ImageSize.Y > 0)
        {
            offset += new Vector2(0.5f); source = Vector2.Max(Vector2.Zero, source - Vector2.One);
        }
        return (offset + uv * source) / new Vector2(texture.Width, texture.Height);
    }
}
