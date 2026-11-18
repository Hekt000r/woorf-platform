using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed partial class GuiScene
{
    private void DrawEditing(GuiInstance item, List<List<GuiGlyph>> lines, GuiRect bounds,
        GuiRect clip, float y, float height, float advance, float opacity)
    {
        if (item != focused || item.ClassName != "TextBox") return;
        var text = item.Get<string>("Text");
        var cursor = Math.Clamp((int)Number(item, "CursorPosition", 1) - 1, 0, text.Length);
        var selection = (int)Number(item, "SelectionStart", -1) - 1;
        var offset = 0;
        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];
            var x = bounds.X + Align(EnumName(item, "TextXAlignment", "Center"), bounds.Width - line.Count * advance);
            for (var column = 0; column <= line.Count; column++)
            {
                if (offset == cursor)
                    commands.Add(new("Rectangle", new(x + column * advance, y, 1, height), clip,
                        Value(item, "TextColor3", new Color3()), opacity));
                if (column == line.Count) break;
                var end = offset + line[column].Text.Length;
                if (selection >= 0 && end > Math.Min(cursor, selection) && offset < Math.Max(cursor, selection))
                    commands.Add(new("Rectangle", new(x + column * advance, y, advance, height), clip,
                        new Color3(0.2f, 0.5f, 1), opacity * 0.45f));
                offset = end;
            }
            if (offset < text.Length && text[offset] == '\n') offset++;
            y += height;
        }
    }
}
