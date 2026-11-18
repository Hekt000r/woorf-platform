using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed partial class GuiScene
{
    private static float GetOwnScale(GuiInstance item)
    {
        var modifier = item.GetChildren().OfType<GuiInstance>().FirstOrDefault(child => child.ClassName == "UIScale");
        return modifier is null ? 1 : Number(modifier, "Scale", 1);
    }

    private static void ApplyAutomaticSize(GuiInstance item, GuiRect parent, float scale, ref float width, ref float height)
    {
        var mode = EnumName(item, "AutomaticSize", "None");
        if (mode == "None" || item.ClassName == "ScrollingFrame") return;
        var natural = MeasureContent(item, new(0, 0, width, height), scale);
        if (item.HasGuiProperty("Text"))
        {
            var style = new GuiTextStyle(new Color3(), "Arial");
            var glyphs = GuiRichText.Parse(Value(item, "Text", ""), Value(item, "RichText", false), style);
            var size = Number(item, "TextSize", 14) * scale;
            var shouldWrap = Value(item, "TextWrapped", false) && mode == "Y";
            var columns = Math.Max(1, (int)(width / Math.Max(0.1f, size * 0.6f)));
            var lines = GuiRichText.Wrap(glyphs, columns, shouldWrap);
            natural = new(Math.Max(natural.X, lines.Max(line => line.Count) * size * 0.6f),
                Math.Max(natural.Y, lines.Count * size * Math.Max(0.1f, Number(item, "LineHeight", 1))));
        }
        if (mode is "X" or "XY") width = Math.Max(width, Math.Min(parent.Width, natural.X));
        if (mode is "Y" or "XY") height = Math.Max(height, Math.Min(parent.Height, natural.Y));
    }

    private static Vector2 MeasureContent(GuiInstance item, GuiRect bounds, float scale)
    {
        var children = item.GetChildren().OfType<GuiInstance>().ToArray();
        var content = ApplyPadding(children, bounds, scale);
        var objects = children.Where(child => child.HasGuiProperty("Visible") && Value(child, "Visible", true)).ToArray();
        var layout = children.FirstOrDefault(child => child.ClassName is "UIListLayout" or "UIGridLayout");
        var placements = Arrange(objects, layout, content, scale);
        var width = placements.Count == 0 ? 0 : placements.Values.Max(rect => rect.X + rect.Width) - bounds.X;
        var height = placements.Count == 0 ? 0 : placements.Values.Max(rect => rect.Y + rect.Height) - bounds.Y;
        if (layout is not null && layout.GetGuiProperty("AbsoluteContentSize") is Vector2 size)
        {
            width = size.X + content.X - bounds.X;
            height = size.Y + content.Y - bounds.Y;
        }
        var padding = children.FirstOrDefault(child => child.ClassName == "UIPadding");
        if (padding is not null)
        {
            width += Resolve(Value(padding, "PaddingRight", new UDim()), bounds.Width, scale);
            height += Resolve(Value(padding, "PaddingBottom", new UDim()), bounds.Height, scale);
        }
        return new(Math.Max(0, width), Math.Max(0, height));
    }

    private static GuiRect ApplyCellConstraint(GuiInstance item, GuiRect cell, float scale)
    {
        var limits = item.GetChildren().OfType<GuiInstance>().FirstOrDefault(child => child.ClassName == "UISizeConstraint");
        if (limits is null) return cell;
        var min = Value(limits, "MinSize", new Vector2()); var max = Value(limits, "MaxSize", new Vector2(float.MaxValue, float.MaxValue));
        return cell with { Width = Math.Clamp(cell.Width, min.X * scale, Math.Max(min.X, max.X) * scale),
            Height = Math.Clamp(cell.Height, min.Y * scale, Math.Max(min.Y, max.Y) * scale) };
    }
}
