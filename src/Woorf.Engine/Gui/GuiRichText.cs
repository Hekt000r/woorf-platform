using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

internal sealed record GuiTextStyle(Color3 Color, string Font, bool Bold = false, bool Italic = false,
    bool Underline = false, bool Strikethrough = false);
internal sealed record GuiGlyph(string Text, GuiTextStyle Style, int Index);

internal static class GuiRichText
{
    internal static List<GuiGlyph> Parse(string text, bool isRich, GuiTextStyle initial)
    {
        var glyphs = new List<GuiGlyph>();
        if (!isRich) { Append(glyphs, text, initial); return glyphs; }
        var style = initial;
        var stack = new List<(string Tag, GuiTextStyle Style)>();
        var cursor = 0;
        foreach (Match match in Regex.Matches(text, "<[^>]+>"))
        {
            Append(glyphs, WebUtility.HtmlDecode(text[cursor..match.Index]), style);
            var tag = Regex.Match(match.Value, "^<\\s*(/?)\\s*(b|i|u|s|font|br)\\b", RegexOptions.IgnoreCase);
            if (!tag.Success) Append(glyphs, match.Value, style);
            else if (tag.Groups[2].Value.Equals("br", StringComparison.OrdinalIgnoreCase)) Append(glyphs, "\n", style);
            else if (tag.Groups[1].Value == "/")
            {
                var name = tag.Groups[2].Value.ToLowerInvariant();
                var index = stack.FindLastIndex(entry => entry.Tag == name);
                if (index >= 0) { style = stack[index].Style; stack.RemoveRange(index, stack.Count - index); }
            }
            else
            {
                var name = tag.Groups[2].Value.ToLowerInvariant(); stack.Add((name, style));
                style = ChangeStyle(style, name, match.Value);
            }
            cursor = match.Index + match.Length;
        }
        Append(glyphs, WebUtility.HtmlDecode(text[cursor..]), style);
        return glyphs;
    }

    private static GuiTextStyle ChangeStyle(GuiTextStyle style, string tag, string source) => tag switch
    {
        "b" => style with { Bold = true }, "i" => style with { Italic = true },
        "u" => style with { Underline = true }, "s" => style with { Strikethrough = true },
        "font" => style with { Color = ReadColor(source, style.Color) }, _ => style,
    };

    private static Color3 ReadColor(string source, Color3 fallback)
    {
        var match = Regex.Match(source, "color\\s*=\\s*[\"']#([0-9a-f]{6})[\"']", RegexOptions.IgnoreCase);
        if (!match.Success) return fallback;
        var value = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new((value >> 16 & 255) / 255f, (value >> 8 & 255) / 255f, (value & 255) / 255f);
    }

    private static void Append(List<GuiGlyph> glyphs, string text, GuiTextStyle style)
    {
        var elements = StringInfo.GetTextElementEnumerator(text.Replace("\r\n", "\n").Replace('\r', '\n'));
        while (elements.MoveNext()) glyphs.Add(new(elements.GetTextElement(), style, glyphs.Count));
    }

    internal static List<List<GuiGlyph>> Wrap(IReadOnlyList<GuiGlyph> glyphs, int columns, bool shouldWrap)
    {
        var lines = new List<List<GuiGlyph>>();
        var line = new List<GuiGlyph>();
        foreach (var glyph in glyphs)
        {
            if (glyph.Text == "\n") { lines.Add(line); line = []; continue; }
            line.Add(glyph);
            if (!shouldWrap || line.Count <= columns) continue;
            var space = line.FindLastIndex(item => string.IsNullOrWhiteSpace(item.Text));
            if (space <= 0) space = columns;
            lines.Add(line.Take(space).ToList());
            line = line.Skip(space).SkipWhile(item => string.IsNullOrWhiteSpace(item.Text)).ToList();
        }
        lines.Add(line);
        return lines;
    }
}
