using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private bool TryReadContextAction(nint state, ContextActionService actions, string property)
    {
        if (property is not ("BindAction" or "BindActionAtPriority" or "BindActionToInputTypes" or "UnbindAction" or
            "UnbindAllActions" or "_ActionInput" or "_SinkInput" or "_CancelInput")) return false;
        if (!((RunService)game.GetService("RunService")).IsClient())
            throw new InvalidOperationException("ContextActionService action bindings are client-only.");
        if (property == "_ActionInput") signals.PushSignal(state, actions.Input);
        else woorf_push_method(state, property);
        return true;
    }

    private bool TryCallContextAction(nint state, ContextActionService actions, string method, out int result)
    {
        result = 0;
        switch (method)
        {
            case "_SinkInput": actions.SinkInput(); return true;
            case "_CancelInput":
                result = Push(state, new GuiInputObject(default, default, "None", "Cancel", "Unknown"));
                return true;
            default: return false;
        }
    }
}
