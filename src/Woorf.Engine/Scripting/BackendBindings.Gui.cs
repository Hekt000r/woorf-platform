using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Gui;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private bool TryReadGui(nint state, Instance instance, string property)
    {
        if (instance is not GuiInstance gui) return false;
        if (property == "AbsoluteSize" && gui.HasGuiProperty(property) && gui.Get<Vector2>(property) == default
            && gui.GetAncestors().Any(ancestor => ancestor.ClassName == "ScreenGui"))
        {
            var viewport = (game.Workspace as Workspace)?.CurrentCamera?.ViewportSize ?? new Vector2(1, 1);
            Push(state, GuiScene.MeasureAbsoluteSize(gui, viewport));
            return true;
        }
        if (property == "SelectionImageObject" && gui.IsA("GuiObject")) { Push(state, gui.SelectionImage); return true; }
        if (gui.HasGuiProperty(property)) { Push(state, gui.GetGuiProperty(property)); return true; }
        if (gui.HasGuiSignal(property)) { signals.PushSignal(state, gui.GetGuiSignal(property)); return true; }
        if (gui.ClassName == "TextBox" && property is "CaptureFocus" or "ReleaseFocus" or "IsFocused")
        {
            woorf_push_method(state, property); return true;
        }
        return false;
    }

    private bool TryWriteGui(nint state, Instance instance, string property)
    {
        if (instance is not GuiInstance gui) return false;
        if (property == "SelectionImageObject" && gui.IsA("GuiObject"))
        {
            gui.SelectionImage = Read(state, 3) switch
            {
                null => null,
                GuiInstance image when image.IsA("GuiObject") => image,
                _ => throw new ArgumentException("Expected GuiObject or nil."),
            };
            return true;
        }
        if (!gui.HasGuiProperty(property)) return false;
        gui.SetGuiProperty(property, Read(state, 3)!);
        return true;
    }

    private bool TryCallGui(nint state, Instance instance, string method, out int result)
    {
        result = 0;
        if (instance is not GuiInstance { ClassName: "TextBox" } gui) return false;
        switch (method)
        {
            case "CaptureFocus": gui.SetFocused(true); return true;
            case "ReleaseFocus": gui.SetFocused(false, Read(state, 2) is true); return true;
            case "IsFocused": result = Push(state, gui.IsFocused); return true;
            default: return false;
        }
    }
}
