using Woorf.Engine.DataModel;
using Woorf.Engine.Networking;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed class NetworkBindings(RuntimeEndpoint? endpoint, LuaValueCodec codec, SignalBindings signals)
{
    internal bool TryRead(nint state, Instance instance, string property, out int result)
    {
        result = 1;
        if (instance is RemoteFunction && property is "FireServer" or "FireClient" or "FireAllClients" or "OnServerEvent" or "OnClientEvent")
            throw new InvalidOperationException($"{property} is not a valid member of RemoteFunction.");
        if (instance is RemoteFunction)
        {
            var internalName = property switch
            {
                "_SendServer" => "FireServer", "_SendClient" => "FireClient",
                "_ServerEvent" => "OnServerEvent", "_ClientEvent" => "OnClientEvent", _ => null,
            };
            if (internalName == "OnServerEvent") { signals.PushSignal(state, ((RemoteFunction)instance).OnServerEvent); return true; }
            if (internalName == "OnClientEvent") { signals.PushSignal(state, ((RemoteFunction)instance).OnClientEvent); return true; }
            if (internalName is not null) { woorf_push_method(state, internalName); return true; }
        }
        switch (instance, property)
        {
            case (RemoteEvent remote, "OnServerEvent"):
                signals.PushSignal(state, remote.OnServerEvent); return true;
            case (RemoteEvent remote, "OnClientEvent"):
                signals.PushSignal(state, remote.OnClientEvent); return true;
            case (Players players, "PlayerRemoving"):
                signals.PushSignal(state, players.PlayerRemoving); return true;
            case (Players players, "PlayerAdded"):
                signals.PushSignal(state, players.PlayerAdded); return true;
            case (Players players, "LocalPlayer"):
                codec.Push(state, players.LocalPlayer); return true;
            case (Player player, "UserId"):
                codec.Push(state, player.UserId); return true;
            case (RemoteFunction, "InvokeServer" or "InvokeClient"):
                woorf_push_method(state, property); return true;
            case (RemoteEvent, "FireServer" or "FireClient" or "FireAllClients"):
            case (Player, "Kick"):
            case (Players, "GetPlayers"):
            case (RunService, "IsServer" or "IsClient"):
                woorf_push_method(state, property); return true;
            default: return false;
        }
    }

    internal bool TryCall(nint state, Instance instance, string method, out int result)
    {
        result = 0;
        switch (instance, method)
        {
            case (Player kickedPlayer, "Kick"):
                var runtimeEndpoint = endpoint ?? throw new InvalidOperationException("Kick requires a client/server session.");
                var target = runtimeEndpoint.IsClient ? runtimeEndpoint.Replicas?.ToServer(kickedPlayer) as Player ?? kickedPlayer : kickedPlayer;
                if (runtimeEndpoint.IsClient && target != runtimeEndpoint.ServerPlayer)
                    throw new InvalidOperationException("Clients can only kick their local Player.");
                if (target is not null) runtimeEndpoint.Host.RequestDisconnect(target);
                return true;
            case (RunService service, "IsServer" or "IsClient"):
                codec.Push(state, method == "IsClient" ? service.IsClientRuntime : !service.IsClientRuntime);
                result = 1; return true;
            case (Players players, "GetPlayers"):
                codec.Push(state, new LuaTableValue(players.GetPlayers().Select((player, index) =>
                    new KeyValuePair<object, object?>((double)index + 1, player)).ToArray()));
                result = 1; return true;
            case (RemoteEvent remote, "FireServer" or "FireClient" or "FireAllClients"):
                var runtime = endpoint ?? throw new InvalidOperationException("RemoteEvents require a client/server session.");
                var arguments = codec.ReadArguments(state, 2);
                var player = method == "FireClient" && arguments.Length > 0 ? arguments[0] as Player : null;
                runtime.Host.Send(runtime, remote, method, player,
                    method == "FireClient" ? arguments.Skip(1).ToArray() : arguments);
                return true;
            default: return false;
        }
    }
}
