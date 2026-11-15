using Woorf.Engine.DataModel;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;

namespace Woorf.Engine.Networking;

internal sealed class LocalNetworkHost(LoadedProject project, int clientCount, IRuntimeClock clock) : IDisposable
{
    private readonly List<RuntimeEndpoint> endpoints = [];
    private readonly Queue<RemoteMessage> messages = [];
    private RuntimeEndpoint server = null!;

    internal int Run()
    {
        server = new RuntimeEndpoint(project.Game, this, clock);
        endpoints.Add(server);
        foreach (var script in project.Scripts.Where(script => script.Instance.ClassName == "Script"))
        {
            if (script.Instance.IsDestroyed || !script.Instance.GetAncestors()
                .Any(ancestor => ancestor.ClassName is "Workspace" or "ServerScriptService")) continue;
            if (server.Session.Execute(script.Source, script.FilePath, script.Instance) != 0) return 1;
        }
        var players = (Players)project.Game.GetService("Players");
        var connected = Enumerable.Range(1, clientCount).Select(id => new Player(id, $"Player{id}")).ToArray();
        foreach (var player in connected) player.Parent = players;
        foreach (var player in connected)
        {
            var game = new DataModel.DataModel { Name = project.Game.Name };
            var endpoint = new RuntimeEndpoint(game, this, clock, player);
            endpoints.Add(endpoint);
            endpoint.Replicas = new ReplicaMap(project.Game, game);
            endpoint.Replicas.Synchronize();
            ((Players)game.GetService("Players")).LocalPlayer = (Player)endpoint.Replicas.ToClient(player)!;
            if (ClientStartup.Run(endpoint, project.Game) != 0) return 1;
        }
        return Drain();
    }

    internal void Send(RuntimeEndpoint source, RemoteEvent remote, string method, Player? player, object?[] arguments)
    {
        if (source.IsClient)
        {
            if (method != "FireServer") throw new InvalidOperationException($"{method} can only be called from the server.");
            var original = source.Replicas!.ToServer(remote) as RemoteEvent
                ?? throw new InvalidOperationException("RemoteEvent is not replicated from the server.");
            messages.Enqueue(new(original, server, source.ServerPlayer,
                RemoteValues.Translate(arguments, source.Replicas.ToServer)));
            return;
        }
        if (method == "FireServer") throw new InvalidOperationException("FireServer can only be called from a client.");
        var clients = endpoints.Where(endpoint => endpoint.IsClient).ToArray();
        if (method == "FireClient")
        {
            if (player is null || player.Parent != project.Game.GetService("Players"))
                throw new InvalidOperationException("FireClient expects a connected Player.");
            clients = clients.Where(endpoint => endpoint.ServerPlayer == player).ToArray();
            if (clients.Length == 0)
            {
                messages.Enqueue(new(remote, null!, player, arguments));
                return;
            }
        }
        foreach (var client in clients) messages.Enqueue(new(remote, client, null, arguments));
    }

    private int Drain()
    {
        while (true)
        {
            foreach (var endpoint in endpoints.Where(endpoint => endpoint.IsClient)) endpoint.Replicas!.Synchronize();
            var count = messages.Count;
            for (var index = 0; index < count; index++) Deliver(messages.Dequeue());
            var deadline = double.PositiveInfinity;
            foreach (var endpoint in endpoints)
            {
                var next = endpoint.Session.Step(clock.ElapsedSeconds);
                if (endpoint.Session.HasFailed) return 1;
                if (next >= 0) deadline = Math.Min(deadline, next);
            }
            if (messages.Count > 0) continue;
            if (double.IsPositiveInfinity(deadline)) return 0;
            var delay = deadline - clock.ElapsedSeconds;
            if (delay > 0) clock.Sleep(TimeSpan.FromSeconds(delay));
        }
    }

    private void Deliver(RemoteMessage message)
    {
        var target = message.Target ?? endpoints.FirstOrDefault(endpoint => endpoint.ServerPlayer == message.Sender);
        if (target is null) return;
        if (target.IsClient)
        {
            var remote = target.Replicas!.ToClient(message.Remote) as RemoteEvent;
            if (remote is null || remote.IsDestroyed) return;
            remote.OnClientEvent.Fire(RemoteValues.Translate(message.Arguments, target.Replicas.ToClient));
        }
        else if (!message.Remote.IsDestroyed)
            message.Remote.OnServerEvent.Fire(message.Arguments.Prepend(message.Sender).ToArray());
    }

    public void Dispose()
    {
        foreach (var endpoint in endpoints.AsEnumerable().Reverse()) endpoint.Dispose();
    }
}
