using Woorf.Engine.DataModel;

namespace Woorf.Engine.Gui;

public sealed partial class GuiScene
{
    public void KeyInput(string key, bool isShiftDown = false)
    {
        var item = focused;
        if (item is null || item.IsDestroyed || !Value(item, "TextEditable", true)) return;
        var text = item.Get<string>("Text");
        var cursor = Math.Clamp((int)Number(item, "CursorPosition", text.Length + 1) - 1, 0, text.Length);
        var selection = (int)Number(item, "SelectionStart", -1) - 1;
        if (key == "SelectAll")
        {
            item.SetGuiProperty("SelectionStart", 1d); item.SetGuiProperty("CursorPosition", (double)text.Length + 1); return;
        }
        if (key == "Delete")
        {
            if (selection >= 0 && selection != cursor) { TextInput("\b"); return; }
            if (cursor < text.Length) item.SetGuiProperty("Text", text.Remove(cursor, char.IsHighSurrogate(text[cursor]) && cursor + 1 < text.Length ? 2 : 1));
            return;
        }
        var next = key switch
        {
            "Left" => cursor > 1 && char.IsLowSurrogate(text[cursor - 1]) ? cursor - 2 : Math.Max(0, cursor - 1),
            "Right" => cursor + 1 < text.Length && char.IsHighSurrogate(text[cursor]) ? cursor + 2 : Math.Min(text.Length, cursor + 1),
            "Home" => 0, "End" => text.Length, _ => cursor,
        };
        if (!isShiftDown && selection >= 0 && selection != cursor)
        {
            if (key == "Left") next = Math.Clamp(Math.Min(cursor, selection), 0, text.Length);
            if (key == "Right") next = Math.Clamp(Math.Max(cursor, selection), 0, text.Length);
        }
        if (isShiftDown && selection < 0) item.SetGuiProperty("SelectionStart", (double)cursor + 1);
        if (!isShiftDown) item.SetGuiProperty("SelectionStart", -1d);
        item.SetGuiProperty("CursorPosition", (double)next + 1);
    }
}
