namespace Woorf.Engine.DataModel;

public sealed class RunService : Instance
{
    internal RunService() : base("RunService") { }

    internal bool IsClientRuntime { get; set; }
    public bool IsClient() => IsClientRuntime;
    public bool IsServer() => !IsClientRuntime;
}
