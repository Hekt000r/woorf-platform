using Woorf.Engine.DataModel;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Scripting;

namespace Woorf.Engine.Networking;

internal sealed class RuntimeEndpoint : IDisposable
{
    internal RuntimeEndpoint(DataModel.DataModel game, IRuntimeHost host, IRuntimeClock clock, Player? player = null)
    {
        Game = game;
        Host = host;
        ServerPlayer = player;
        ((RunService)game.GetService("RunService")).IsClientRuntime = player is not null;
        Session = new LuauSession(game, clock, this);
    }

    internal DataModel.DataModel Game { get; }
    internal IRuntimeHost Host { get; }
    internal Player? ServerPlayer { get; }
    internal bool IsConnected { get; set; } = true;
    internal bool IsClient => ServerPlayer is not null;
    internal LuauSession Session { get; }
    internal ReplicaMap? Replicas { get; set; }
    public void Dispose() => Session.Dispose();
}
