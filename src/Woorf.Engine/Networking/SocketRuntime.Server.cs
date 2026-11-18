using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal sealed partial class SocketRuntime
{
    internal int Listen(IPEndPoint address, CancellationToken cancellation)
    {
        using var listener = new TcpListener(address);
        endpoint = new RuntimeEndpoint(game, this, clock);
        if (endpoint.Session.StartScripts(project!.Scripts) != 0) return 1;
        listener.Start(32);
        Console.WriteLine("Listening " + listener.LocalEndpoint);
        while (!cancellation.IsCancellationRequested)
        {
            var started = Stopwatch.GetTimestamp();
            if (listener.Pending())
            {
                var client = listener.AcceptTcpClient();
                client.NoDelay = true;
                if (peers.Count >= 32) client.Dispose();
                else peers.Add(new SocketPeer(client, shouldLimitInput: true));
            }
            foreach (var peer in peers.ToArray()) ReadPeer(peer);
            ProcessLeaves();
            if (!Step()) return 1;
            ProcessLeaves();
            Flush();
            ReportMetrics(started);
            Thread.Sleep(5);
        }
        return 0;
    }

    private void ReadPeer(SocketPeer peer)
    {
        try
        {
            foreach (var packet in peer.Read())
            {
                if (peer.Player is null)
                {
                    if (packet.Kind != "hello" || packet.Player != 0) throw new InvalidDataException("Expected protocol handshake.");
                    var identity = identities.Resolve(packet.Credential);
                    if (peers.Any(other => other.Player?.UserId == identity.UserId))
                        throw new InvalidDataException("Identity already connected.");
                    var player = new Player(identity.UserId, $"Player{identity.UserId}");
                    peer.Player = player;
                    player.Parent = game.GetService("Players");
                    var nodes = graph.Capture();
                    peer.Send(new("welcome", Player: graph.Identify(player), Nodes: nodes, Credential: identity.Token));
                    peer.Baseline.Initialize(nodes);
                    peer.ReplicationRevision = graph.Revision;
                }
                else if (packet.Kind == "leave") { RemovePeer(peer); return; }
                else if (packet.Kind == "input") ReceivePlayInput(packet, peer.Player);
                else if (packet.Kind == "tool") ReceiveToolInput(packet, peer.Player);
                else Deliver(packet, peer.Player);
            }
            if (peer.IsClosed || peer.Player is null && DateTime.UtcNow - peer.ConnectedAt > TimeSpan.FromSeconds(5))
                RemovePeer(peer);
        }
        catch (Exception error) when (IsPeerFailure(error)) { RemovePeer(peer); }
    }

    private void Flush()
    {
        graph.UpdateDirty();
        var changes = graph.GetChanges();
        foreach (var peer in peers.Where(peer => peer.Player is not null).ToArray())
        {
            try
            {
                if (peer.ReplicationRevision == graph.Revision) continue;
                var delta = peer.Baseline.BuildDelta(changes.Nodes, changes.Removed);
                if (delta is not null) peer.Send(delta);
                peer.ReplicationRevision = graph.Revision;
            }
            catch (Exception error) when (IsPeerFailure(error)) { RemovePeer(peer); }
        }
        graph.CompleteChanges();
        foreach (var peer in peers.ToArray())
        {
            try
            {
                peer.Writes.ReleaseDeferred();
                peer.Writes.Pump();
            }
            catch (Exception error) when (IsPeerFailure(error)) { RemovePeer(peer); }
        }
    }

    private void ProcessLeaves()
    {
        while (disconnects.TryDequeue(out var player))
        {
            var peer = peers.FirstOrDefault(peer => peer.Player == player);
            if (peer is not null) RemovePeer(peer);
        }
    }

    private void RemovePeer(SocketPeer peer)
    {
        if (!peers.Remove(peer)) return;
        RecordDisconnectedPeer(peer);
        if (peer.Player is Player player)
        {
            ((Players)game.GetService("Players")).PlayerRemoving.Fire(player);
            player.Destroy();
        }
        peer.Dispose();
    }
}
