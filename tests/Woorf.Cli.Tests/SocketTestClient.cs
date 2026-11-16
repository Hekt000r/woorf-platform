using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

sealed class SocketTestClient : IDisposable
{
    private readonly TcpClient client = new();
    internal async Task Connect(string address, CancellationToken cancellation) =>
        await client.ConnectAsync(IPEndPoint.Parse(address), cancellation);
    internal async Task Send(string text, CancellationToken cancellation)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var frame = new byte[bytes.Length + 4];
        BinaryPrimitives.WriteInt32BigEndian(frame, bytes.Length);
        bytes.CopyTo(frame, 4);
        await client.GetStream().WriteAsync(frame, cancellation);
    }
    internal async Task<JsonDocument?> Read(CancellationToken cancellation)
    {
        var header = new byte[4];
        if (await client.GetStream().ReadAsync(header.AsMemory(0, 1), cancellation) == 0) return null;
        await client.GetStream().ReadExactlyAsync(header.AsMemory(1), cancellation);
        var length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length is < 1 or > 4 * 1024 * 1024) throw new Exception("Invalid frame length.");
        var bytes = new byte[length];
        await client.GetStream().ReadExactlyAsync(bytes, cancellation);
        return JsonDocument.Parse(bytes);
    }
    public void Dispose() => client.Dispose();
}
