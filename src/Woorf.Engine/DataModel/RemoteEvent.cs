using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public sealed class RemoteEvent : Instance
{
    internal RemoteEvent() : base("RemoteEvent")
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
