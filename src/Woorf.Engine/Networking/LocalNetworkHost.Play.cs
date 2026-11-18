using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

public sealed partial class LocalNetworkHost
{
    internal DataModel.DataModel GetClientGame(Player player) =>
        endpoints.First(endpoint => endpoint.ServerPlayer == player && endpoint.IsConnected).Game;

    internal Tool? GetAuthorityTool(Player player, Tool tool) =>
        endpoints.FirstOrDefault(endpoint => endpoint.ServerPlayer == player && endpoint.IsConnected)?.Replicas?.ToServer(tool) as Tool;

    public int Pump()
    {
        EnsureStarted();
        ProcessDisconnects();
        foreach (var endpoint in endpoints.Where(endpoint => endpoint.IsClient).ToArray()) endpoint.Replicas!.Synchronize();
        var count = messages.Count;
        for (var index = 0; index < count; index++) { Deliver(messages.Dequeue()); ProcessDisconnects(); }
        foreach (var endpoint in endpoints.ToArray())
        {
            endpoint.Session.Step(clock.ElapsedSeconds);
            if (endpoint.Session.HasFailed) { HasFailed = true; return 1; }
            ProcessDisconnects();
        }
        foreach (var endpoint in endpoints.Where(endpoint => endpoint.IsClient).ToArray()) endpoint.Replicas!.Synchronize();
        return 0;
    }
}
