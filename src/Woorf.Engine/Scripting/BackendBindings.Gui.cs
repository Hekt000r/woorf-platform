using Woorf.Engine.DataModel;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private bool TryReadGui(nint state, Instance instance, string property)
    {
        if (instance is not GuiInstance gui) return false;
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
        if (instance is not GuiInstance gui || !gui.HasGuiProperty(property)) return false;
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
