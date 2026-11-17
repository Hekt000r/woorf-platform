using Woorf.Engine.DataModel;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private bool TryReadTouch(nint state, Instance instance, string property)
    {
        if (instance is not BasePart part) return false;
        if (property == "CanTouch") { Push(state, part.CanTouch); return true; }
        if (property is not ("Touched" or "TouchEnded")) return false;
        signals.PushSignal(state, property == "Touched" ? part.Touched : part.TouchEnded);
        return true;
    }

    private bool TryWriteTouch(nint state, Instance instance, string property)
    {
        if (instance is not BasePart part || property != "CanTouch") return false;
        part.CanTouch = Read(state, 3) is bool value ? value : throw new ArgumentException("Expected boolean.");
        return true;
    }
}
