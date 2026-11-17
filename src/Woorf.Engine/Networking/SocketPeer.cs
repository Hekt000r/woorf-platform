using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal sealed record WirePacket(string Kind, int Version = 8, int Player = 0, int Remote = 0,
    WireValue[]? Arguments = null, WireNode[]? Nodes = null,
    WirePatch[]? Updates = null, int[]? Removed = null, string? Credential = null);

internal sealed class SocketPeer : IDisposable
{
    private readonly TcpClient client;
    internal SocketWrites Writes { get; }

    internal SocketPeer(TcpClient client, bool shouldLimitInput = false)
    {
        this.client = client;
        inbound = shouldLimitInput ? new InboundBudget() : null;
        client.SendBufferSize = 64 * 1024;
        client.Client.Blocking = false;
        Writes = new SocketWrites(client.Client);
    }

    private readonly InboundBudget? inbound;
    private const int MaxFrame = 4 * 1024 * 1024;
    internal static readonly JsonSerializerOptions Options = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        MaxDepth = 192,
    };
    private readonly byte[] buffer = new byte[MaxFrame + 4];
    private int count;
    internal long ReplicationRevision { get; set; } = -1;
    internal ReplicationBaseline Baseline { get; } = new();
    internal Player? Player { get; set; }
    internal DateTime ConnectedAt { get; } = DateTime.UtcNow;
    internal bool IsClosed { get; private set; }

    internal void Send(WirePacket packet) => Writes.Enqueue(packet, false);
    internal void SendRemote(WirePacket packet) => Writes.Enqueue(packet, true);

    internal IReadOnlyList<WirePacket> Read()
    {
        var packets = new List<WirePacket>();
        var received = 0;
        for (var iteration = 0; iteration < 64; iteration++)
        {
            if (count >= 4)
            {
                var length = BinaryPrimitives.ReadInt32BigEndian(buffer);
                if (length is < 1 or > MaxFrame) throw new InvalidDataException("Invalid socket frame length.");
                if (count >= length + 4)
                {
                    inbound?.Dispatch();
                    var packet = JsonSerializer.Deserialize<WirePacket>(buffer.AsSpan(4, length), Options)
                        ?? throw new InvalidDataException("Empty socket packet.");
                    if (packet.Version != 8) throw new InvalidDataException("Unsupported socket protocol version.");
                    packets.Add(packet);
                    count -= length + 4;
                    Buffer.BlockCopy(buffer, length + 4, buffer, 0, count);
                    if (packets.Count == 32) break;
                    continue;
                }
            }
            if (client.Available == 0)
            {
                if (client.Client.Poll(0, SelectMode.SelectRead) && client.Available == 0) IsClosed = true;
                break;
            }
            if (received >= 256 * 1024) break;
            var read = client.Client.Receive(buffer, count, Math.Min(256 * 1024 - received, Math.Min(buffer.Length - count, client.Available)), SocketFlags.None);
            inbound?.Receive(read);
            received += read;
            if (read == 0) { IsClosed = true; break; }
            count += read;
        }
        return packets;
    }

    public void Dispose()
    {
        IsClosed = true;
        Writes.Clear();
        client.Dispose();
    }
}
