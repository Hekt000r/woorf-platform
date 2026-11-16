using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal interface IRuntimeHost
{
    void Send(RuntimeEndpoint source, RemoteEvent remote, string method, Player? player, object?[] arguments);
    void RequestDisconnect(Player player);
}
