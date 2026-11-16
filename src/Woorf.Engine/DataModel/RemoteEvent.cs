using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public class RemoteEvent : Instance
{
    internal RemoteEvent(string className = "RemoteEvent") : base(className)
    {
        GetSignal("Destroying").Connect(_ =>
        {
            OnServerEvent.Close();
            OnClientEvent.Close();
        });
    }

    public ScriptSignal OnServerEvent { get; } = new();
    public ScriptSignal OnClientEvent { get; } = new();
}
