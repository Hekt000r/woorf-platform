using Woorf.Engine.DataModel;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;

namespace Woorf.Engine.Networking;

public sealed partial class LocalNetworkHost : IDisposable, IRuntimeHost
{
    private readonly LoadedProject project;
    private readonly int clientCount;
    private readonly IRuntimeClock clock;
    private bool isStarted;
    private bool isDisposed;
    internal bool HasFailed { get; private set; }
    internal bool IsInteractivePreview { get; init; }
    private int nextPlayerId;
    private readonly Queue<Player> disconnects = [];

    public LocalNetworkHost(LoadedProject project, IRuntimeClock clock)
        : this(project, 0, clock) { }

    internal LocalNetworkHost(LoadedProject project, int clientCount, IRuntimeClock clock)
    {
        this.project = project;
        this.clientCount = clientCount;
        this.clock = clock;
    }

    private readonly List<RuntimeEndpoint> endpoints = [];
    private readonly Queue<RemoteMessage> messages = [];
    private RuntimeEndpoint server = null!;

    internal int Run()
    {
        var status = Start();
        if (status != 0) return status;
        var players = (Players)project.Game.GetService("Players");
        var connected = Enumerable.Range(1, clientCount).Select(_ => CreatePlayer()).ToArray();
        foreach (var player in connected) player.Parent = players;
        foreach (var player in connected)
            if (StartClient(player) != 0) return 1;
        return Drain();
    }

    void IRuntimeHost.Send(RuntimeEndpoint source, RemoteEvent remote, string method, Player? player, object?[] arguments)
    {
        if (!source.IsConnected) throw new InvalidOperationException("Client disconnected.");
        if (source.IsClient)
        {
            if (method != "FireServer") throw new InvalidOperationException($"{method} can only be called from the server.");
            try { source.Inbound.Dispatch(); }
            catch (InvalidDataException error)
            {
                disconnects.Enqueue(source.ServerPlayer!);
                throw new InvalidOperationException(error.Message);
            }
            var original = source.Replicas!.ToServer(remote) as RemoteEvent
                ?? throw new InvalidOperationException("RemoteEvent is not replicated from the server.");
            messages.Enqueue(new(original, server, source.ServerPlayer,
                RemoteValues.Translate(arguments, source.Replicas.ToServer)));
            return;
        }
        if (method == "FireServer") throw new InvalidOperationException("FireServer can only be called from a client.");
        var clients = endpoints.Where(endpoint => endpoint.IsClient && endpoint.IsConnected).ToArray();
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
            ProcessDisconnects();
            foreach (var endpoint in endpoints.Where(endpoint => endpoint.IsClient)) endpoint.Replicas!.Synchronize();
            var count = messages.Count;
            for (var index = 0; index < count; index++)
            {
                Deliver(messages.Dequeue());
                ProcessDisconnects();
            }
            var deadline = double.PositiveInfinity;
            foreach (var endpoint in endpoints.ToArray())
            {
                var next = endpoint.Session.Step(clock.ElapsedSeconds);
                if (endpoint.Session.HasFailed) return 1;
                ProcessDisconnects();
                if (next >= 0 && endpoint.IsConnected) deadline = Math.Min(deadline, next);
            }
            if (messages.Count > 0) continue;
            if (double.IsPositiveInfinity(deadline)) return 0;
            var delay = deadline - clock.ElapsedSeconds;
            if (delay > 0) clock.Sleep(TimeSpan.FromSeconds(Math.Max(delay, 0.0000001)));
        }
    }

    private void Deliver(RemoteMessage message)
    {
        var target = message.Target ?? endpoints.FirstOrDefault(endpoint => endpoint.ServerPlayer == message.Sender);
        if (target is null || !target.IsConnected ||
            message.Target == server && !endpoints.Any(endpoint => endpoint.ServerPlayer == message.Sender)) return;
        if (target.IsClient)
        {
            var remote = target.Replicas!.ToClient(message.Remote) as RemoteEvent;
            if (remote is null || remote.IsDestroyed) return;
            if (remote is RemoteFunction function) target.Session.InitializeRemote(function);
            remote.OnClientEvent.Fire(RemoteValues.Translate(message.Arguments, target.Replicas.ToClient));
        }
        else if (!message.Remote.IsDestroyed)
        {
            if (message.Remote is RemoteFunction function) target.Session.InitializeRemote(function);
            message.Remote.OnServerEvent.Fire(message.Arguments.Prepend(message.Sender).ToArray());
        }
    }

    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        foreach (var endpoint in endpoints.AsEnumerable().Reverse())
        {
            endpoint.Dispose();
            HasFailed |= endpoint.Session.HasFailed;
        }
        endpoints.Clear();
        messages.Clear();
        disconnects.Clear();
    }
}
