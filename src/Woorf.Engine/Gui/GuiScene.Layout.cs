using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed partial class GuiScene
{
    private static GuiRect ApplyPadding(GuiInstance[] children, GuiRect bounds, float scale)
    {
        var padding = children.FirstOrDefault(item => item.ClassName == "UIPadding");
        if (padding is null) return bounds;
        var left = Resolve(Value(padding, "PaddingLeft", new UDim()), bounds.Width, scale);
        var right = Resolve(Value(padding, "PaddingRight", new UDim()), bounds.Width, scale);
        var top = Resolve(Value(padding, "PaddingTop", new UDim()), bounds.Height, scale);
        var bottom = Resolve(Value(padding, "PaddingBottom", new UDim()), bounds.Height, scale);
        return new(bounds.X + left, bounds.Y + top, Math.Max(0, bounds.Width - left - right), Math.Max(0, bounds.Height - top - bottom));
    }

    private static Dictionary<GuiInstance, GuiRect> Arrange(GuiInstance[] objects, GuiInstance? layout, GuiRect bounds, float scale)
    {
        var placements = objects.ToDictionary(item => item, item => Measure(item, bounds, scale));
        if (layout is null) return placements;
        var ordered = EnumName(layout, "SortOrder", "LayoutOrder") == "Name"
            ? objects.OrderBy(item => item.Name, StringComparer.Ordinal).ToArray()
            : objects.OrderBy(item => Number(item, "LayoutOrder")).ToArray();
        if (layout.ClassName == "UIGridLayout") ArrangeGrid(ordered, layout, bounds, scale, placements);
        else ArrangeList(ordered, layout, bounds, scale, placements);
        return placements;
    }

    private static GuiRect Measure(GuiInstance item, GuiRect parent, float scale)
    {
        var size = Value(item, "Size", new UDim2(0, 100, 0, 100));
        var mode = EnumName(item, "SizeConstraint", "RelativeXY");
        var ownScale = GetOwnScale(item);
        var width = Math.Max(0, Resolve(size.X, mode == "RelativeYY" ? parent.Height : parent.Width, scale)) * ownScale;
        var height = Math.Max(0, Resolve(size.Y, mode == "RelativeXX" ? parent.Width : parent.Height, scale)) * ownScale;
        var constraints = item.GetChildren().OfType<GuiInstance>().ToArray();
        ApplyAutomaticSize(item, parent, scale * ownScale, ref width, ref height);
        var aspect = constraints.FirstOrDefault(child => child.ClassName == "UIAspectRatioConstraint");
        if (aspect is not null)
        {
            var ratio = Number(aspect, "AspectRatio", 1);
            if (ratio > 0) { if (EnumName(aspect, "DominantAxis", "Width") == "Height") width = height * ratio; else height = width / ratio; }
        }
        var limits = constraints.FirstOrDefault(child => child.ClassName == "UISizeConstraint");
        if (limits is not null)
        {
            var min = Value(limits, "MinSize", new Vector2()); var max = Value(limits, "MaxSize", new Vector2(float.MaxValue, float.MaxValue));
            width = Math.Clamp(width, min.X * scale * ownScale, Math.Max(min.X, max.X) * scale * ownScale);
            height = Math.Clamp(height, min.Y * scale * ownScale, Math.Max(min.Y, max.Y) * scale * ownScale);
        }
        var position = Value(item, "Position", new UDim2()); var anchor = Value(item, "AnchorPoint", new Vector2());
        return new(parent.X + Resolve(position.X, parent.Width, scale) - anchor.X * width,
            parent.Y + Resolve(position.Y, parent.Height, scale) - anchor.Y * height, width, height);
    }

    private static void ArrangeList(GuiInstance[] objects, GuiInstance layout, GuiRect bounds, float scale, Dictionary<GuiInstance, GuiRect> placements)
    {
        var horizontal = EnumName(layout, "FillDirection", "Vertical") == "Horizontal";
        var gap = Resolve(Value(layout, "Padding", new UDim()), horizontal ? bounds.Width : bounds.Height, scale);
        var length = objects.Sum(item => horizontal ? placements[item].Width : placements[item].Height) + Math.Max(0, objects.Length - 1) * gap;
        var alignment = EnumName(layout, horizontal ? "HorizontalAlignment" : "VerticalAlignment", horizontal ? "Left" : "Top");
        var offset = Align(alignment, (horizontal ? bounds.Width : bounds.Height) - length);
        foreach (var item in objects)
        {
            var rectangle = placements[item];
            var cross = EnumName(layout, horizontal ? "VerticalAlignment" : "HorizontalAlignment", horizontal ? "Top" : "Left");
            placements[item] = horizontal
                ? rectangle with { X = bounds.X + offset, Y = bounds.Y + Align(cross, bounds.Height - rectangle.Height) }
                : rectangle with { X = bounds.X + Align(cross, bounds.Width - rectangle.Width), Y = bounds.Y + offset };
            offset += (horizontal ? rectangle.Width : rectangle.Height) + gap;
        }
        layout.SetComputedProperty("AbsoluteContentSize", horizontal
            ? new Vector2(length, objects.Length == 0 ? 0 : objects.Max(item => placements[item].Height))
            : new Vector2(objects.Length == 0 ? 0 : objects.Max(item => placements[item].Width), length));
    }

    private static void ArrangeGrid(GuiInstance[] objects, GuiInstance layout, GuiRect bounds, float scale, Dictionary<GuiInstance, GuiRect> placements)
    {
        var cell = Value(layout, "CellSize", new UDim2(0, 100, 0, 100)); var padding = Value(layout, "CellPadding", new UDim2(0, 5, 0, 5));
        var width = Math.Max(1, Resolve(cell.X, bounds.Width, scale)); var height = Math.Max(1, Resolve(cell.Y, bounds.Height, scale));
        var dx = width + Resolve(padding.X, bounds.Width, scale); var dy = height + Resolve(padding.Y, bounds.Height, scale);
        var horizontal = EnumName(layout, "FillDirection", "Horizontal") == "Horizontal";
        var capacity = Math.Max(1, (int)(horizontal ? (bounds.Width + dx - width) / dx : (bounds.Height + dy - height) / dy));
        var max = (int)Number(layout, "FillDirectionMaxCells"); if (max > 0) capacity = Math.Min(capacity, max);
        var columns = horizontal ? Math.Min(capacity, objects.Length) : (objects.Length + capacity - 1) / capacity;
        var rows = horizontal ? (objects.Length + capacity - 1) / capacity : Math.Min(capacity, objects.Length);
        var contentWidth = columns * dx - (columns > 0 ? dx - width : 0);
        var contentHeight = rows * dy - (rows > 0 ? dy - height : 0);
        var x = bounds.X + Align(EnumName(layout, "HorizontalAlignment", "Left"), bounds.Width - contentWidth);
        var y = bounds.Y + Align(EnumName(layout, "VerticalAlignment", "Top"), bounds.Height - contentHeight);
        for (var index = 0; index < objects.Length; index++)
        {
            var cellX = horizontal ? index % capacity : index / capacity;
            var cellY = horizontal ? index / capacity : index % capacity;
            var rectangle = new GuiRect(x + cellX * dx, y + cellY * dy, width, height);
            placements[objects[index]] = ApplyCellConstraint(objects[index], rectangle, scale);
        }
        layout.SetComputedProperty("AbsoluteContentSize", new Vector2(contentWidth, contentHeight));
    }

    private static float Align(string alignment, float space) => alignment is "Center" ? space / 2 : alignment is "Right" or "Bottom" ? space : 0;
}
