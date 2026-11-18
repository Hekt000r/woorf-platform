using System.Net.Sockets;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Networking;

internal sealed partial class SocketRuntime
{
    private readonly Dictionary<Player, double> playInputTimes = [];
    internal DataModel.DataModel PlayGame => game;
    internal Player? PlayPlayer => endpoint?.ServerPlayer;

    internal void OpenPlay(string host, int port)
    {
        var client = new TcpClient { NoDelay = true };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { client.ConnectAsync(host, port, timeout.Token).AsTask().GetAwaiter().GetResult(); }
        catch { client.Dispose(); throw; }
        var peer = new SocketPeer(client); peers.Add(peer);
        peer.Send(new("hello", Credential: ClientCredential.Load(identityFile)));
        peer.Writes.Pump();
    }

    internal void PumpPlay()
    {
        var peer = peers[0];
        foreach (var packet in peer.Read()) Receive(packet);
        if (peer.IsClosed) throw new IOException("The multiplayer server disconnected.");
        if (endpoint is not null && !Step()) throw new InvalidOperationException("Multiplayer client script failed.");
        peer.Writes.Pump();
    }

    internal void SendPlayInput(Vector3 direction, bool shouldJump, bool shouldReset, Vector3 cameraHeading)
    {
        if (endpoint is null) return;
        peers[0].Send(new("input", Arguments:
            [WireValue.Encode(direction, _ => 0), WireValue.Encode(shouldJump, _ => 0), WireValue.Encode(shouldReset, _ => 0), WireValue.Encode(cameraHeading, _ => 0)]));
        peers[0].Writes.Pump();
    }

    private void ReceivePlayInput(WirePacket packet, Player player)
    {
        if (packet.Player != 0 || packet.Remote != 0 || packet.Nodes is not null || packet.Updates is not null
            || packet.Removed is not null || packet.Credential is not null || packet.Arguments is not { Length: 3 or 4 } values
            || values.Any(value => value is null) || values[0].Decode(graph.Find) is not Vector3 direction
            || values[1].Decode(graph.Find) is not bool shouldJump || values[2].Decode(graph.Find) is not bool shouldReset
            || !float.IsFinite(direction.X) || !float.IsFinite(direction.Y) || !float.IsFinite(direction.Z))
            throw new InvalidDataException("Invalid character input packet.");
        var heading = values.Length == 4 ? values[3].Decode(graph.Find) : default(Vector3);
        if (heading is not Vector3 cameraHeading || !float.IsFinite(cameraHeading.X) || !float.IsFinite(cameraHeading.Z))
            throw new InvalidDataException("Invalid camera heading.");
        if (player.Character?.FindFirstChild("Humanoid") is not Humanoid humanoid) return;
        humanoid.CameraHeading = new(cameraHeading.X, 0, cameraHeading.Z);
        humanoid.Move(direction);
        if (shouldJump) humanoid.Jump = true;
        if (shouldReset) humanoid.Health = 0;
        playInputTimes[player] = clock.ElapsedSeconds;
    }

    private void ExpirePlayInput()
    {
        foreach (var (player, lastInput) in playInputTimes.ToArray())
        {
            if (player.Parent is null || player.IsDestroyed) { playInputTimes.Remove(player); continue; }
            if (clock.ElapsedSeconds - lastInput <= 0.5) continue;
            if (player.Character?.FindFirstChild("Humanoid") is Humanoid humanoid)
            {
                humanoid.CameraHeading = default;
                humanoid.Move(default);
            }
            playInputTimes.Remove(player);
        }
    }
}
