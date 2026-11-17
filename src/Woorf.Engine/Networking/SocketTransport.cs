using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Woorf.Engine.DataModel;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;

namespace Woorf.Engine.Networking;

public static class SocketTransport
{
    public static int Listen(LoadedProject project, string address, CancellationToken cancellation = default,
        Action<SocketTransportMetrics>? observe = null, bool shouldLoadCharacters = false)
    {
        if (shouldLoadCharacters) ((Players)project.Game.GetService("Players")).CharacterAutoLoads = true;
        var endpoint = IPEndPoint.Parse(address);
        using var runtime = new SocketRuntime(project, observe);
        return runtime.Finish(runtime.Listen(endpoint, cancellation));
    }

    public static int Connect(string address, CancellationToken cancellation = default,
        Action<SocketTransportMetrics>? observe = null, string? identityFile = null)
    {
        var separator = address.LastIndexOf(':');
        if (separator < 1 || !int.TryParse(address[(separator + 1)..], out var port) || port is < 1 or > 65535)
            throw new InvalidDataException("Expected host:port.");
        using var runtime = new SocketRuntime(observe: observe, identityFile: identityFile);
        return runtime.Finish(runtime.Connect(address[..separator].Trim('[', ']'), port, cancellation));
    }
}

internal sealed partial class SocketRuntime : IRuntimeHost, IDisposable
{
    private readonly LoadedProject? project;
    private readonly MonotonicClock clock = new();
    private readonly DataModel.DataModel game;
    private readonly WireGraph graph;
    private readonly List<SocketPeer> peers = [];
    private readonly Queue<Player> disconnects = [];
    private RuntimeEndpoint? endpoint;
    private bool shouldLeave;
    private readonly ServerIdentities identities;
    private readonly string? identityFile;
    private readonly ReplicaUpdates replicaUpdates;

    internal SocketRuntime(LoadedProject? project = null, Action<SocketTransportMetrics>? observe = null, string? identityFile = null)
    {
        this.identityFile = identityFile;
        this.project = project;
        this.observe = observe;
        game = project?.Game ?? new();
        identities = new ServerIdentities(Path.GetFullPath(Path.Combine(game.DataDirectory, "..", "auth")));
        graph = new WireGraph(game, project is not null);
        replicaUpdates = new ReplicaUpdates(graph);
    }

    void IRuntimeHost.Send(RuntimeEndpoint source, RemoteEvent remote, string method, Player? player, object?[] arguments)
    {
        var id = graph.Identify(remote);
        if (id == 0) throw new InvalidOperationException("Remote is not replicated.");
        if (source.IsClient && method != "FireServer" || !source.IsClient && method == "FireServer")
            throw new InvalidOperationException("Remote method called from the wrong execution side.");
        var values = arguments.Select(value => WireValue.Encode(value, graph.Identify)).ToArray();
        var packet = new WirePacket("remote", Remote: id, Arguments: values);
        if (source.IsClient) { peers[0].Send(packet); return; }
        if (method == "FireClient")
        {
            var peer = peers.FirstOrDefault(peer => peer.Player == player && player is not null);
            if (peer is null) throw new InvalidOperationException("FireClient expects a connected Player.");
            QueueRemote(peer, packet);
        }
        else foreach (var peer in peers.Where(peer => peer.Player is not null).ToArray()) QueueRemote(peer, packet);
    }

    private void QueueRemote(SocketPeer peer, WirePacket packet)
    {
        if (peer.Writes.IsOverloaded) return;
        try { peer.SendRemote(packet); }
        catch (Exception error) when (IsPeerFailure(error))
        {
            if (peer.Player is not null) disconnects.Enqueue(peer.Player);
        }
    }

    void IRuntimeHost.RequestDisconnect(Player player)
    {
        if (project is null)
        {
            if (player != endpoint!.ServerPlayer) throw new InvalidOperationException("Cannot disconnect another player.");
            peers[0].Send(new("leave"));
            shouldLeave = true;
        }
        else disconnects.Enqueue(player);
    }

    private void Deliver(WirePacket packet, Player? sender)
    {
        if (packet.Player != 0 || packet.Kind != "remote" || packet.Arguments is null || packet.Arguments.Any(value => value is null))
            throw new InvalidDataException("Invalid remote packet.");
        var remote = graph.Find(packet.Remote) as RemoteEvent
            ?? throw new InvalidDataException("Unknown replicated remote.");
        var arguments = packet.Arguments.Select(value => value.Decode(graph.Find)).ToArray();
        if (remote is RemoteFunction function) endpoint!.Session.InitializeRemote(function);
        if (project is null) remote.OnClientEvent.Fire(arguments);
        else remote.OnServerEvent.Fire(arguments.Prepend(sender).ToArray());
    }

    private bool Step()
    {
        ExpirePlayInput();
        endpoint!.Session.Step(clock.ElapsedSeconds);
        return !endpoint.Session.HasFailed;
    }

    private static bool IsPeerFailure(Exception error) => error is IOException or InvalidDataException or SocketException
        or JsonException or InvalidOperationException or ArgumentException or KeyNotFoundException;

    private bool isDisposed;
    internal int Finish(int status) { Dispose(); return status != 0 || endpoint?.Session.HasFailed == true ? 1 : 0; }

    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        foreach (var peer in peers) peer.Dispose();
        peers.Clear();
        endpoint?.Dispose();
        graph.Dispose();
    }
}
