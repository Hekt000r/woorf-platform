namespace Woorf.Engine.DataModel;

public sealed class RunService : Instance
{
    public Woorf.Engine.Events.ScriptSignal Heartbeat { get; } = new();
    internal RunService() : base("RunService") { }

    internal bool IsClientRuntime { get; set; }
    public bool IsClient() => IsClientRuntime;
    public bool IsServer() => !IsClientRuntime;
}
