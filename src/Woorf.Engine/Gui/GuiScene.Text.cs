using System.Text;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed partial class GuiScene
{
    private void DrawText(GuiInstance item, GuiRect bounds, GuiRect clip, float opacity, float scale, float rotation)
    {
        var text = Value(item, "Text", "");
        var color = Value(item, "TextColor3", new Color3());
        var font = EnumName(item, "Font", "Legacy");
        var style = new GuiTextStyle(color, font, font.Contains("Bold") || font.Contains("Black"), font.Contains("Italic"));
        var glyphs = GuiRichText.Parse(text, Value(item, "RichText", false), style);
        item.SetComputedProperty("ContentText", string.Concat(glyphs.Select(glyph => glyph.Text)));
        if (item.ClassName == "TextBox" && glyphs.Count == 0 && focused != item)
            glyphs = GuiRichText.Parse(Value(item, "PlaceholderText", ""), false,
                style with { Color = Value(item, "PlaceholderColor3", new Color3(0.7f, 0.7f, 0.7f)) });
        var size = GetTextSize(item, glyphs, bounds, scale);
        var advance = Math.Max(0.1f, size * 0.6f);
        var columns = Math.Max(1, (int)(bounds.Width / advance));
        var lines = GuiRichText.Wrap(glyphs, columns, Value(item, "TextWrapped", false) || Value(item, "TextScaled", false));
        var height = size * Math.Max(0.1f, Number(item, "LineHeight", 1));
        var width = lines.Count == 0 ? 0 : lines.Max(line => line.Count) * advance;
        var fits = width <= bounds.Width + 0.01f && lines.Count * height <= bounds.Height + 0.01f;
        item.SetComputedProperty("TextBounds", new Vector2(width, lines.Count * height));
        item.SetComputedProperty("TextFits", fits);
        var truncate = EnumName(item, "TextTruncate", "None");
        if (!fits && truncate != "None") lines = Truncate(lines, columns, Math.Max(1, (int)(bounds.Height / Math.Max(0.1f, height))), truncate);
        var y = bounds.Y + Align(EnumName(item, "TextYAlignment", "Center"), bounds.Height - lines.Count * height);
        DrawEditing(item, lines, bounds, clip.Intersect(bounds), y, height, advance, opacity);
        foreach (var line in lines)
        {
            var x = bounds.X + Align(EnumName(item, "TextXAlignment", "Center"), bounds.Width - line.Count * advance);
            DrawTextLine(item, line, new(x, y, bounds.Width, height), clip.Intersect(bounds), size, advance, opacity, rotation);
            y += height;
        }
    }

    private static float GetTextSize(GuiInstance item, List<GuiGlyph> glyphs, GuiRect bounds, float scale)
    {
        var size = Number(item, "TextSize", 14) * scale;
        if (Value(item, "TextScaled", false))
        {
            var lines = GuiRichText.Wrap(glyphs, int.MaxValue, false);
            size = Math.Max(1, Math.Min(bounds.Height / Math.Max(1, lines.Count),
                bounds.Width / Math.Max(1, lines.Max(line => line.Count)) / 0.6f));
        }
        var constraint = item.GetChildren().OfType<GuiInstance>().FirstOrDefault(child => child.ClassName == "UITextSizeConstraint");
        if (constraint is not null) size = Math.Clamp(size, Number(constraint, "MinTextSize", 1), Math.Max(Number(constraint, "MinTextSize", 1), Number(constraint, "MaxTextSize", 100)));
        return size;
    }

    private static List<List<GuiGlyph>> Truncate(List<List<GuiGlyph>> lines, int columns, int maxLines, string mode)
    {
        var visible = lines.Take(maxLines).Select(line => line.ToList()).ToList();
        for (var index = 0; index < visible.Count; index++)
        {
            var line = visible[index];
            if (line.Count <= columns && (index < maxLines - 1 || lines.Count <= maxLines)) continue;
            var count = Math.Max(0, columns - 1);
            if (mode == "SplitWord")
            {
                var space = line.Take(count).ToList().FindLastIndex(glyph => string.IsNullOrWhiteSpace(glyph.Text));
                if (space > 0) count = space;
            }
            var shortened = line.Take(count).ToList();
            var style = line.FirstOrDefault()?.Style ?? new(new Color3(), "Arial");
            shortened.Add(new("…", style, -1)); visible[index] = shortened;
        }
        return visible;
    }

    private void DrawTextLine(GuiInstance item, List<GuiGlyph> line, GuiRect bounds, GuiRect clip, float size, float advance, float opacity, float rotation)
    {
        var maximum = (int)Number(item, "MaxVisibleGraphemes", -1);
        var index = 0;
        while (index < line.Count)
        {
            var start = index; var style = line[index].Style;
            var text = new StringBuilder();
            while (index < line.Count && line[index].Style == style)
            {
                var glyph = line[index++];
                if (maximum < 0 || glyph.Index < maximum) text.Append(glyph.Text);
            }
            if (text.Length == 0) continue;
            var rectangle = new GuiRect(bounds.X + start * advance, bounds.Y, (index - start) * advance + 2, bounds.Height + 2);
            var command = new GuiDrawCommand("Text", rectangle, clip, style.Color, opacity * (1 - Number(item, "TextTransparency")),
                text.ToString(), size, "Left", "Top", Rotation: rotation, Font: style.Font,
                Bold: style.Bold, Italic: style.Italic, Underline: style.Underline, Strikethrough: style.Strikethrough);
            DrawTextStroke(item, command, opacity);
            commands.Add(command);
        }
    }

    private void DrawTextStroke(GuiInstance item, GuiDrawCommand command, float opacity)
    {
        var alpha = opacity * (1 - Number(item, "TextStrokeTransparency", 1));
        if (alpha <= 0) return;
        foreach (var (dx, dy) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
            commands.Add(command with { Bounds = command.Bounds with { X = command.Bounds.X + dx, Y = command.Bounds.Y + dy },
                Color = Value(item, "TextStrokeColor3", new Color3()), Opacity = alpha });
    }
}
