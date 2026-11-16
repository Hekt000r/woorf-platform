using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal sealed record WirePacket(string Kind, int Version = 3, int Player = 0, int Remote = 0,
    WireValue[]? Arguments = null, WireNode[]? Nodes = null,
    WirePatch[]? Updates = null, int[]? Removed = null);

internal sealed class SocketPeer : IDisposable
{
    private readonly TcpClient client;
    internal SocketWrites Writes { get; }

    internal SocketPeer(TcpClient client)
    {
        this.client = client;
        client.SendBufferSize = 64 * 1024;
        client.Client.Blocking = false;
        Writes = new SocketWrites(client.Client);
    }

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
        for (var iteration = 0; iteration < 64; iteration++)
        {
            if (count >= 4)
            {
                var length = BinaryPrimitives.ReadInt32BigEndian(buffer);
                if (length is < 1 or > MaxFrame) throw new InvalidDataException("Invalid socket frame length.");
                if (count >= length + 4)
                {
                    var packet = JsonSerializer.Deserialize<WirePacket>(buffer.AsSpan(4, length), Options)
                        ?? throw new InvalidDataException("Empty socket packet.");
                    if (packet.Version != 3) throw new InvalidDataException("Unsupported socket protocol version.");
                    packets.Add(packet);
                    count -= length + 4;
                    Buffer.BlockCopy(buffer, length + 4, buffer, 0, count);
                    continue;
                }
            }
            if (client.Available == 0)
            {
                if (client.Client.Poll(0, SelectMode.SelectRead) && client.Available == 0) IsClosed = true;
                break;
            }
            var read = client.Client.Receive(buffer, count, Math.Min(buffer.Length - count, client.Available), SocketFlags.None);
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
