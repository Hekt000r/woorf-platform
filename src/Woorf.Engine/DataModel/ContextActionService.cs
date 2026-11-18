using Woorf.Engine.Datatypes;
using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

internal sealed class ContextActionService : Instance
{
    private readonly Stack<bool> dispatches = new();
    internal ScriptSignal Input { get; } = new();
    internal ContextActionService() : base("ContextActionService") =>
        GetSignal("Destroying").Connect(_ => Input.Close());

    internal bool Dispatch(GuiInputObject input)
    {
        dispatches.Push(false);
        try { Input.Fire(input); return dispatches.Peek(); }
        finally { dispatches.Pop(); }
    }

    internal void SinkInput()
    {
        if (dispatches.Count == 0) return;
        dispatches.Pop();
        dispatches.Push(true);
    }
}
