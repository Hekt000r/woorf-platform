using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

public sealed partial class LocalNetworkHost
{
    public int Start()
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        if (isStarted) throw new InvalidOperationException("Host already started.");
        isStarted = true;
        server = new RuntimeEndpoint(project.Game, this, clock);
        endpoints.Add(server);
        foreach (var script in project.Scripts.Where(script => script.Instance.ClassName == "Script"))
        {
            if (script.Instance.IsDestroyed || !script.Instance.GetAncestors()
                .Any(ancestor => ancestor.ClassName is "Workspace" or "ServerScriptService")) continue;
            if (server.Session.Execute(script.Source, script.FilePath, script.Instance) != 0) return 1;
        }
        return 0;
    }

    public Player ConnectClient()
    {
        EnsureStarted();
        var player = CreatePlayer();
        player.Parent = project.Game.GetService("Players");
        foreach (var endpoint in endpoints.Where(endpoint => endpoint.IsClient).ToArray())
            endpoint.Replicas!.Synchronize();
        if (StartClient(player) != 0) throw new InvalidOperationException("Client startup failed.");
        return player;
    }

    public int RunUntilIdle()
    {
        EnsureStarted();
        return Drain();
    }

    public void DisconnectClient(Player player)
    {
        EnsureStarted();
        var endpoint = endpoints.FirstOrDefault(endpoint => endpoint.ServerPlayer == player);
        if (endpoint is null || !endpoint.IsConnected) return;
        endpoint.IsConnected = false;
        ((Players)project.Game.GetService("Players")).PlayerRemoving.Fire(player);
        foreach (var observer in endpoints.Where(item => item.IsClient && item.IsConnected).ToArray())
        {
            if (observer.Replicas!.ToClient(player) is Player replica)
                ((Players)observer.Game.GetService("Players")).PlayerRemoving.Fire(replica);
        }
        player.Destroy();
        endpoints.Remove(endpoint);
        endpoint.Dispose();
        foreach (var observer in endpoints.Where(item => item.IsClient).ToArray())
            observer.Replicas!.Synchronize();
    }

    void IRuntimeHost.RequestDisconnect(Player player) => disconnects.Enqueue(player);

    private void ProcessDisconnects()
    {
        while (disconnects.TryDequeue(out var player)) DisconnectClient(player);
    }

    private Player CreatePlayer()
    {
        var id = ++nextPlayerId;
        return new Player(id, $"Player{id}");
    }

    private int StartClient(Player player)
    {
        var game = new DataModel.DataModel { Name = project.Game.Name };
        var endpoint = new RuntimeEndpoint(game, this, clock, player);
        endpoints.Add(endpoint);
        endpoint.Replicas = new ReplicaMap(project.Game, game);
        endpoint.Replicas.Synchronize();
        ((Players)game.GetService("Players")).LocalPlayer = (Player)endpoint.Replicas.ToClient(player)!;
        return ClientStartup.Run(endpoint, project.Game);
    }

    private void EnsureStarted()
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        if (!isStarted) throw new InvalidOperationException("Host has not started.");
    }
}
