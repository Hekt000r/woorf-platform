using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed partial class GuiScene
{
    private GuiInstance? hovered, pressed, focused, inputPressed;
    private bool wasPointerDown;
    private float previousPointerX, previousPointerY;

    public bool Pointer(float x, float y, bool isDown, float wheel = 0)
    {
        var hit = targets.LastOrDefault(target => target.Bounds.Contains(x, y) && target.Clip.Contains(x, y)
            && !target.Instance.IsDestroyed && Value(target.Instance, "Interactable", true));
        var current = hit?.Instance;
        var hoverTarget = current;
        while (current is not null && !Value(current, "Active", false) && !current.IsA("GuiButton"))
            current = current.Parent is GuiInstance ancestor && ancestor.IsA("GuiObject") ? ancestor : null;
        if (hovered != hoverTarget)
        {
            hovered?.FireGuiSignal("MouseLeave", (double)x, (double)y);
            hovered = hoverTarget;
            hovered?.FireGuiSignal("MouseEnter", (double)x, (double)y);
        }
        hovered?.FireGuiSignal("MouseMoved", (double)x, (double)y);
        if (x != previousPointerX || y != previousPointerY)
            FireInput(hoverTarget, "InputChanged", new(new(x, y, 0), new(x - previousPointerX, y - previousPointerY, 0)));
        previousPointerX = x; previousPointerY = y;
        if (isDown && !wasPointerDown)
        {
            pressed = current;
            inputPressed = hoverTarget;
            FireInput(inputPressed, "InputBegan", new(new(x, y, 0), default, "MouseButton1", "Begin"));
            if (focused != current) ReleaseFocus(false);
            if (current?.ClassName == "TextBox" && Value(current, "TextEditable", true)) Focus(current);
            if (current?.IsA("GuiButton") == true && Value(current, "Active", true)) current.FireGuiSignal("MouseButton1Down", (double)x, (double)y);
        }
        if (!isDown && wasPointerDown)
        {
            ReleasePointer(current, x, y);
        }
        wasPointerDown = isDown;
        if (wheel != 0) Scroll(current, wheel);
        return current is not null && Value(current, "Active", false);
    }

    private static void FireInput(GuiInstance? item, string signal, GuiInputObject input)
    {
        while (item is not null && item.IsA("GuiObject"))
        {
            item.FireGuiSignal(signal, input); item = item.Parent as GuiInstance;
        }
    }

    private void ReleasePointer(GuiInstance? current, float x, float y)
    {
        var input = new GuiInputObject(new(x, y, 0), default, "MouseButton1", "End");
        FireInput(inputPressed, "InputEnded", input); inputPressed = null;
        var released = pressed; pressed = null;
        if (released is not { IsDestroyed: false } || !released.IsA("GuiButton") || !Value(released, "Active", true)) return;
        released.FireGuiSignal("MouseButton1Up", (double)x, (double)y);
        if (released != current) return;
        released.FireGuiSignal("MouseButton1Click");
        if (!released.IsDestroyed) released.FireGuiSignal("Activated", input, 1d);
    }

    private void Focus(GuiInstance item)
    {
        if (focused == item) return;
        ReleaseFocus(false); focused = item; item.SetFocused(true);
        item.SetGuiProperty("CursorPosition", (double)item.Get<string>("Text").Length + 1);
    }

    public void ReleaseFocus(bool submitted)
    {
        var previous = focused; focused = null;
        if (previous is null || previous.IsDestroyed) return;
        previous.SetGuiProperty("CursorPosition", -1d); previous.SetGuiProperty("SelectionStart", -1d);
        previous.SetFocused(false, submitted);
    }

    public void TextInput(string text)
    {
        if (focused is null || focused.IsDestroyed || !Value(focused, "TextEditable", true)) return;
        var item = focused;
        foreach (var character in text)
        {
            var value = item.Get<string>("Text");
            var cursor = Math.Clamp((int)Number(item, "CursorPosition", value.Length + 1) - 1, 0, value.Length);
            if (character == '\r' || character == '\n')
            {
                if (!Value(item, "MultiLine", false)) { ReleaseFocus(true); return; }
            }
            var selection = (int)Number(item, "SelectionStart", -1) - 1;
            var hasSelection = selection >= 0 && selection != cursor;
            if (hasSelection && (character == '\b' || character >= ' ' || character == '\n' || character == '\r'))
            {
                selection = Math.Clamp(selection, 0, value.Length);
                var start = Math.Min(cursor, selection); var length = Math.Abs(cursor - selection);
                value = value.Remove(start, length); cursor = start; item.SetGuiProperty("SelectionStart", -1d);
            }
            if (character == '\b')
            {
                if (!hasSelection && cursor > 0)
                {
                    var start = System.Globalization.StringInfo.ParseCombiningCharacters(value)
                        .Last(index => index < cursor);
                    value = value.Remove(start, cursor - start); cursor = start;
                }
            }
            else if (character >= ' ' || character == '\n' || character == '\r')
            {
                value = value.Insert(cursor, character == '\r' ? "\n" : character.ToString()); cursor++;
            }
            item.SetGuiProperty("Text", value); item.SetGuiProperty("CursorPosition", (double)cursor + 1);
        }
    }

    private static void Scroll(GuiInstance? item, float wheel)
    {
        while (item is not null && item.ClassName != "ScrollingFrame") item = item.Parent as GuiInstance;
        if (item is null || !Value(item, "ScrollingEnabled", true)) return;
        var canvas = Value(item, "AbsoluteCanvasSize", new Vector2()); var viewport = Value(item, "AbsoluteWindowSize", new Vector2());
        var offset = Value(item, "CanvasPosition", new Vector2());
        var horizontal = EnumName(item, "ScrollingDirection", "XY") == "X";
        item.SetGuiProperty("CanvasPosition", horizontal
            ? new Vector2(Math.Clamp(offset.X - wheel * 40, 0, Math.Max(0, canvas.X - viewport.X)), offset.Y)
            : new Vector2(offset.X, Math.Clamp(offset.Y - wheel * 40, 0, Math.Max(0, canvas.Y - viewport.Y))));
    }
}
