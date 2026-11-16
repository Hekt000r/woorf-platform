using System.Diagnostics;

static class InboundLimitTests
{
    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-inbound-" + Guid.NewGuid().ToString("N"));
        Process? server = null;
        try
        {
            NetworkProjectTests.Write(root, "woorf.project.json", "{}");
            NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
                local remote = Instance.new("RemoteEvent", game:GetService("ReplicatedStorage")); remote.Name = "Traffic"
                local counts = {}
                remote.OnServerEvent:Connect(function(player, kind)
                    counts[player] = (counts[player] or 0) + 1
                    if kind == "ping" then remote:FireClient(player, "pong") end
                end)
                game:GetService("Players").PlayerRemoving:Connect(function(player)
                    print("REMOVED " .. player.UserId .. " " .. (counts[player] or 0))
                end)
                """);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { cliPath, root, "--listen", "127.0.0.1:0" }) start.ArgumentList.Add(argument);
            server = Process.Start(start)!;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var address = (await server.StandardOutput.ReadLineAsync(timeout.Token))![10..];
            using var flood = new SocketTestClient();
            await flood.Connect(address, timeout.Token); await flood.Send("{\"Kind\":\"hello\"}", timeout.Token);
            using var welcome = await flood.Read(timeout.Token) ?? throw new Exception("Missing welcome.");
            var remote = welcome.RootElement.GetProperty("Nodes").EnumerateArray()
                .Single(node => node.GetProperty("Name").GetString() == "Traffic").GetProperty("Id").GetInt32();
            for (var index = 0; index < 256; index++)
                await flood.Send(Request(remote, "spam"), timeout.Token);
            var removed = await server.StandardOutput.ReadLineAsync(timeout.Token);
            if (removed is null || !removed.StartsWith("REMOVED 1 ") || int.Parse(removed[10..]) >= 200)
                throw new Exception("Flood reached excessive callbacks: " + removed);
            using var healthy = new SocketTestClient();
            await healthy.Connect(address, timeout.Token); await healthy.Send("{\"Kind\":\"hello\"}", timeout.Token);
            using var secondWelcome = await healthy.Read(timeout.Token);
            await healthy.Send(Request(remote, "ping"), timeout.Token);
            while (true)
            {
                using var reply = await healthy.Read(timeout.Token) ?? throw new Exception("Healthy peer disconnected.");
                if (reply.RootElement.GetProperty("Kind").GetString() == "remote") break;
            }
            await CheckByteFlood(address, remote, server, timeout.Token);
            await CheckLocalFlood(cliPath, root);
            Console.WriteLine("PASS disconnects inbound request floods before dispatch while preserving healthy peers");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL inbound rate limits: " + error.Message); return 1; }
        finally
        {
            if (server is not null) { if (!server.HasExited) { server.Kill(true); await server.WaitForExitAsync(); } server.Dispose(); }
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
    private static async Task CheckByteFlood(string address, int remote, Process server, CancellationToken cancellation)
    {
        using var flood = new SocketTestClient();
        await flood.Connect(address, cancellation); await flood.Send("{\"Kind\":\"hello\"}", cancellation);
        using var welcome = await flood.Read(cancellation);
        try
        {
            for (var index = 0; index < 3; index++) await flood.Send(Request(remote, new string('x', 2 * 1024 * 1024)), cancellation);
        }
        catch (Exception error) when (error is IOException or System.Net.Sockets.SocketException) { }
        var removed = await server.StandardOutput.ReadLineAsync(cancellation);
        if (removed is null || !removed.StartsWith("REMOVED 3 ") || int.Parse(removed[10..]) > 2)
            throw new Exception("Byte flood was not bounded: " + removed);
    }

    private static async Task CheckLocalFlood(string cliPath, string root)
    {
        NetworkProjectTests.WriteClient(root, """
            local remote = game:GetService("ReplicatedStorage").Traffic
            local blocked = false
            for index = 1, 256 do
                if not pcall(function() remote:FireServer("spam") end) then blocked = true; break end
            end
            assert(blocked, "local request flood was not bounded")
            print("local-limited")
            """);
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { cliPath, root, "--clients", "1" }) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            if (process.ExitCode != 0 || !(await output).Contains("local-limited") || await error != "")
                throw new Exception("Local limits failed: " + await output + await error);
        }
        finally { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } }
    }

    private static string Request(int remote, string text) =>
        $"{{\"Kind\":\"remote\",\"Remote\":{remote},\"Arguments\":[{{\"Kind\":\"string\",\"Text\":\"{text}\"}}]}}";
}
