using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public sealed class BindableEvent : Instance
{
    internal BindableEvent() : base("BindableEvent") => GetSignal("Destroying").Connect(_ => Event.Close());
    public ScriptSignal Event { get; } = new();
    public void Fire(params object?[] arguments) => Event.Fire(arguments);
}
