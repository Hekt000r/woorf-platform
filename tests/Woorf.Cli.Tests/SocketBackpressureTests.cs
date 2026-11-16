using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

static class SocketBackpressureTests
{
    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-backpressure-" + Guid.NewGuid().ToString("N"));
        Process? server = null;
        try
        {
            Arrange(root);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { cliPath, root, "--listen", "127.0.0.1:0" }) start.ArgumentList.Add(argument);
            server = Process.Start(start)!;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var line = await server.StandardOutput.ReadLineAsync(timeout.Token);
            if (line is null || !line.StartsWith("Listening ")) throw new Exception("Listener failed.");
            var address = IPEndPoint.Parse(line["Listening ".Length..]);
            using var slow = new TcpClient { ReceiveBufferSize = 1024 };
            await slow.ConnectAsync(address.Address, address.Port, timeout.Token);
            await WriteAsync(slow, "{\"Kind\":\"hello\"}", timeout.Token);
            using var welcome = await ReadAsync(slow, timeout.Token);
            var remote = welcome.RootElement.GetProperty("Nodes").EnumerateArray()
                .Single(node => node.GetProperty("Name").GetString() == "Traffic").GetProperty("Id").GetInt32();
            using var healthy = new TcpClient();
            await healthy.ConnectAsync(address.Address, address.Port, timeout.Token);
            await WriteAsync(healthy, "{\"Kind\":\"hello\"}", timeout.Token);
            using var healthyWelcome = await ReadAsync(healthy, timeout.Token);
            await WriteAsync(slow, Request(remote, "flood"), timeout.Token);
            await WriteAsync(healthy, Request(remote, "ping"), timeout.Token);
            using var responseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(1.5));
            while (true)
            {
                using var response = await ReadAsync(healthy, responseTimeout.Token);
                if (response.RootElement.GetProperty("Kind").GetString() == "remote") break;
            }
            if (await server.StandardOutput.ReadLineAsync(timeout.Token) != "slow removed")
                throw new Exception("Slow reader was not disconnected.");
            await WriteAsync(healthy, Request(remote, "ping"), timeout.Token);
            while (true)
            {
                using var response = await ReadAsync(healthy, timeout.Token);
                if (response.RootElement.GetProperty("Kind").GetString() == "remote") break;
            }
            await CheckStalledPeerAsync(address, server, timeout.Token);
            Console.WriteLine("PASS bounds slow-reader traffic without blocking healthy TCP clients");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL bounds slow-reader traffic without blocking healthy TCP clients: " + error.Message);
            return 1;
        }
        finally
        {
            if (server is not null) { if (!server.HasExited) server.Kill(true); server.Dispose(); }
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static async Task CheckStalledPeerAsync(IPEndPoint address, Process server, CancellationToken cancellation)
    {
        using var stalled = new TcpClient { ReceiveBufferSize = 1024 };
        await stalled.ConnectAsync(address.Address, address.Port, cancellation);
        await WriteAsync(stalled, "{\"Kind\":\"hello\"}", cancellation);
        using var welcome = await ReadAsync(stalled, cancellation);
        var remote = welcome.RootElement.GetProperty("Nodes").EnumerateArray()
            .Single(node => node.GetProperty("Name").GetString() == "Traffic").GetProperty("Id").GetInt32();
        await WriteAsync(stalled, Request(remote, "stall"), cancellation);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        if (await server.StandardOutput.ReadLineAsync(deadline.Token) != "stalled removed")
            throw new Exception("A peer with stalled writes remained connected.");
    }

    private static string Request(int remote, string text) =>
        $"{{\"Kind\":\"remote\",\"Remote\":{remote},\"Arguments\":[{{\"Kind\":\"string\",\"Text\":\"{text}\"}}]}}";

    private static async Task WriteAsync(TcpClient client, string text, CancellationToken cancellation)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, bytes.Length);
        await client.GetStream().WriteAsync(header, cancellation);
        await client.GetStream().WriteAsync(bytes, cancellation);
    }

    private static async Task<JsonDocument> ReadAsync(TcpClient client, CancellationToken cancellation)
    {
        var header = new byte[4];
        await client.GetStream().ReadExactlyAsync(header, cancellation);
        var bytes = new byte[BinaryPrimitives.ReadInt32BigEndian(header)];
        await client.GetStream().ReadExactlyAsync(bytes, cancellation);
        return JsonDocument.Parse(bytes);
    }

    private static void Arrange(string root)
    {
        NetworkProjectTests.Write(root, "woorf.project.json", "{\"name\":\"Backpressure\",\"root\":\"src\"}");
        NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
            local remote = Instance.new("RemoteEvent", game:GetService("ReplicatedStorage"))
            remote.Name = "Traffic"
            game:GetService("Players").PlayerRemoving:Connect(function(player)
                if player.UserId == 1 then print("slow removed")
                elseif player.UserId == 3 then print("stalled removed") end
            end)
            remote.OnServerEvent:Connect(function(player, kind)
                if kind == "flood" then
                    local payload = string.rep("x", 1024 * 1024)
                    for index = 1, 32 do remote:FireClient(player, payload) end
                elseif kind == "stall" then
                    remote:FireClient(player, string.rep("s", 3 * 1024 * 1024))
                else
                    assert(kind == "ping")
                    remote:FireClient(player, "pong")
                end
            end)
            """);
    }
}
