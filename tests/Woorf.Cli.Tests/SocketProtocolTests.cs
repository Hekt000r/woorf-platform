using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

static class SocketProtocolTests
{
    internal static async Task CheckAsync(string address, CancellationToken cancellation)
    {
        await RejectAsync(address, "{\"Kind\":\"hello\",\"Version\":1}", false, cancellation);
        await RejectAsync(address, "{\"Kind\":\"hello\",\"Version\":2}", false, cancellation);
        await RejectAsync(address, "{\"Kind\":\"hello\",\"Version\":3}", false, cancellation);
        await RejectAsync(address, "{\"Kind\":\"hello\",\"Version\":4}", false, cancellation);
        await RejectAsync(address, "{\"Kind\":\"hello\",\"Version\":5}", false, cancellation);
        await RejectAsync(address, "{\"Kind\":\"hello\",\"Version\":999}", false, cancellation);
        await RejectAsync(address, "", true, cancellation);
        await RejectRemoteAsync(address, false, cancellation);
        await RejectRemoteAsync(address, true, cancellation);
        await CheckPartialDisconnectAsync(address, cancellation);
        await CheckDeltasAsync(address, cancellation);
        await CheckAbruptDisconnectAsync(address, cancellation);
        Console.WriteLine("PASS rejects invalid socket packets and detects abrupt disconnects");
    }

    private static async Task CheckPartialDisconnectAsync(string address, CancellationToken cancellation)
    {
        var endpoint = IPEndPoint.Parse(address);
        using var client = new TcpClient();
        await client.ConnectAsync(endpoint.Address, endpoint.Port, cancellation);
        await client.GetStream().WriteAsync(new byte[] { 0, 0 }, cancellation);
    }

    private static async Task CheckDeltasAsync(string address, CancellationToken cancellation)
    {
        var endpoint = IPEndPoint.Parse(address);
        using var client = new TcpClient();
        await client.ConnectAsync(endpoint.Address, endpoint.Port, cancellation);
        await WriteFragmentedAsync(client, "{\"Kind\":\"hello\"}", cancellation);
        using var welcome = await ReadFrameAsync(client, cancellation) ?? throw new Exception("Missing welcome.");
        var nodes = welcome.RootElement.GetProperty("Nodes").EnumerateArray().ToArray();
        int Find(string name) => nodes.Single(node => node.GetProperty("Name").GetString() == name).GetProperty("Id").GetInt32();
        var remote = Find("Message");
        var part = Find("DeltaPart");
        var removed = Find("DeltaRemoved");
        await WriteAsync(client, $"{{\"Kind\":\"remote\",\"Remote\":{remote},\"Arguments\":[{{\"Kind\":\"string\",\"Text\":\"delta\"}}]}}", cancellation);
        using var update = await ReadFrameAsync(client, cancellation) ?? throw new Exception("Missing delta.");
        var root = update.RootElement;
        if (root.GetProperty("Kind").GetString() != "delta")
            throw new Exception("Shared changes still send full snapshots.");
        var additions = root.GetProperty("Nodes").EnumerateArray().ToArray();
        if (additions.Length != 1 || additions[0].GetProperty("Name").GetString() != "DeltaAdded")
            throw new Exception("Delta resent unchanged instances.");
        var patches = root.GetProperty("Updates").EnumerateArray().ToArray();
        var patch = patches.Single(item => item.GetProperty("Id").GetInt32() == part).GetProperty("Properties");
        if (patch.EnumerateObject().Count() != 2 || patch.GetProperty("Name").GetString() != "DeltaRenamed"
            || patch.GetProperty("Parent").GetInt32() != additions[0].GetProperty("Id").GetInt32())
            throw new Exception("Delta did not restrict the update to changed properties.");
        if (!root.GetProperty("Removed").EnumerateArray().Any(item => item.GetInt32() == removed))
            throw new Exception("Delta omitted destroyed instances.");
        using var reply = await ReadFrameAsync(client, cancellation) ?? throw new Exception("Missing remote after delta.");
        if (reply.RootElement.GetProperty("Kind").GetString() != "remote")
            throw new Exception("Delta ordering failed.");
        for (var index = 0; index < 20; index++)
            await WriteAsync(client, $"{{\"Kind\":\"remote\",\"Remote\":{remote},\"Arguments\":[{{\"Kind\":\"string\",\"Text\":\"noop\"}}]}}", cancellation);
        for (var index = 0; index < 20; index++)
        {
            using var unchanged = await ReadFrameAsync(client, cancellation) ?? throw new Exception("Missing no-op reply.");
            if (unchanged.RootElement.GetProperty("Kind").GetString() != "remote")
                throw new Exception("Unchanged state generated replication traffic.");
        }
        Console.WriteLine("PASS sends property deltas additions and removals before remote messages");
    }

    private static async Task CheckAbruptDisconnectAsync(string address, CancellationToken cancellation)
    {
        var endpoint = IPEndPoint.Parse(address);
        using var client = new TcpClient();
        await client.ConnectAsync(endpoint.Address, endpoint.Port, cancellation);
        await WriteAsync(client, "{\"Kind\":\"hello\"}", cancellation);
        using var welcome = await ReadFrameAsync(client, cancellation) ?? throw new Exception("Missing welcome.");
        var remote = welcome.RootElement.GetProperty("Nodes").EnumerateArray()
            .Single(node => node.GetProperty("Name").GetString() == "Message").GetProperty("Id").GetInt32();
        await WriteAsync(client, $"{{\"Kind\":\"remote\",\"Remote\":{remote},\"Arguments\":[{{\"Kind\":\"string\",\"Text\":\"abrupt\"}}]}}", cancellation);
        while (true)
        {
            using var packet = await ReadFrameAsync(client, cancellation) ?? throw new Exception("Missing InvokeClient request.");
            if (packet.RootElement.GetProperty("Kind").GetString() == "remote") break;
        }
    }

    private static async Task RejectRemoteAsync(string address, bool nullArgument, CancellationToken cancellation)
    {
        var endpoint = IPEndPoint.Parse(address);
        using var client = new TcpClient();
        await client.ConnectAsync(endpoint.Address, endpoint.Port, cancellation);
        await WriteAsync(client, "{\"Kind\":\"hello\",\"Version\":6}", cancellation);
        using var welcome = await ReadFrameAsync(client, cancellation) ?? throw new Exception("Missing welcome.");
        var remote = nullArgument
            ? welcome.RootElement.GetProperty("Nodes").EnumerateArray().Single(node => node.GetProperty("Name").GetString() == "Message").GetProperty("Id").GetInt32()
            : int.MaxValue;
        var arguments = nullArgument ? "[null]" : "[]";
        await WriteAsync(client, $"{{\"Kind\":\"remote\",\"Remote\":{remote},\"Arguments\":{arguments}}}", cancellation);
        while (true)
        {
            using var packet = await ReadFrameAsync(client, cancellation);
            if (packet is null) break;
            if (packet.RootElement.GetProperty("Kind").GetString() != "delta")
                throw new Exception("Server accepted an invalid remote packet.");
        }
    }

    private static async Task<JsonDocument?> ReadFrameAsync(TcpClient client, CancellationToken cancellation)
    {
        var header = new byte[4];
        if (await client.GetStream().ReadAsync(header.AsMemory(0, 1), cancellation) == 0) return null;
        await client.GetStream().ReadExactlyAsync(header.AsMemory(1), cancellation);
        var length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length is < 1 or > 4 * 1024 * 1024) throw new Exception("Invalid frame.");
        var bytes = new byte[length];
        await client.GetStream().ReadExactlyAsync(bytes, cancellation);
        return JsonDocument.Parse(bytes);
    }

    private static async Task RejectAsync(string address, string payload, bool oversized, CancellationToken cancellation)
    {
        var endpoint = IPEndPoint.Parse(address);
        using var client = new TcpClient();
        await client.ConnectAsync(endpoint.Address, endpoint.Port, cancellation);
        if (oversized)
        {
            var header = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(header, int.MaxValue);
            await client.GetStream().WriteAsync(header, cancellation);
        }
        else await WriteAsync(client, payload, cancellation);
        if (await client.GetStream().ReadAsync(new byte[1], cancellation) != 0)
            throw new Exception("Server accepted an invalid handshake or frame.");
    }

    private static async Task WriteFragmentedAsync(TcpClient client, string payload, CancellationToken cancellation)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, bytes.Length);
        await client.GetStream().WriteAsync(header.AsMemory(0, 1), cancellation);
        await Task.Delay(20, cancellation);
        await client.GetStream().WriteAsync(header.AsMemory(1), cancellation);
        await client.GetStream().WriteAsync(bytes.AsMemory(0, 2), cancellation);
        await Task.Delay(20, cancellation);
        await client.GetStream().WriteAsync(bytes.AsMemory(2), cancellation);
    }

    private static async Task WriteAsync(TcpClient client, string payload, CancellationToken cancellation)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, bytes.Length);
        await client.GetStream().WriteAsync(header, cancellation);
        await client.GetStream().WriteAsync(bytes, cancellation);
    }
}
