using System.Net.Sockets;
using System.Diagnostics;
using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal sealed partial class SocketRuntime
{
    internal int Connect(string host, int port, CancellationToken cancellation)
    {
        using var client = new TcpClient { NoDelay = true };
        client.ConnectAsync(host, port, cancellation).AsTask().GetAwaiter().GetResult();
        var peer = new SocketPeer(client);
        peers.Add(peer);
        peer.Send(new("hello", Credential: ClientCredential.Load(identityFile)));
        var started = clock.ElapsedSeconds;
        while (!cancellation.IsCancellationRequested && (!shouldLeave || peer.Writes.QueuedBytes > 0))
        {
            var tickStarted = Stopwatch.GetTimestamp();
            foreach (var packet in peer.Read()) Receive(packet);
            if (peer.IsClosed) return endpoint is null ? 1 : 0;
            if (endpoint is null && clock.ElapsedSeconds - started > 5)
                throw new InvalidDataException("Server handshake timed out.");
            if (!shouldLeave && endpoint is not null && !Step()) return 1;
            peer.Writes.Pump();
            ReportMetrics(tickStarted);
            Thread.Sleep(5);
        }
        return 0;
    }

    private void Receive(WirePacket packet)
    {
        if (endpoint is null)
        {
            if (packet.Kind != "welcome" || packet.Nodes is null) throw new InvalidDataException("Expected server welcome.");
            ClientCredential.Save(identityFile, packet.Credential);
            replicaUpdates.Initialize(packet.Nodes);
            var player = graph.Find(packet.Player) as Player ?? throw new InvalidDataException("Invalid local player.");
            ((Players)game.GetService("Players")).LocalPlayer = player;
            peers[0].Player = player;
            endpoint = new RuntimeEndpoint(game, this, clock, player);
            if (ClientStartup.Run(endpoint, game) != 0) throw new InvalidDataException("Client startup failed.");
            return;
        }
        if (packet.Kind == "delta") replicaUpdates.Apply(packet);
        else Deliver(packet, null);
    }
}
