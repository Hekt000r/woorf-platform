using System.Diagnostics;

static class StableIdentityTests
{
    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-identities-" + Guid.NewGuid().ToString("N"));
        try
        {
            Arrange(root);
            var first = Path.Combine(root, "first.json"); var second = Path.Combine(root, "second.json");
            await RunServer(cliPath, root, async address =>
            {
                await RunClient(cliPath, address, first, "PROFILE 1 1");
                await RunClient(cliPath, address, second, "PROFILE 2 1");
                await RunClient(cliPath, address, first, "PROFILE 1 2");
            });
            await RunServer(cliPath, root, async address =>
            {
                await RunClient(cliPath, address, second, "PROFILE 2 2");
                await RunClient(cliPath, address, first, "PROFILE 1 3");
                var invalid = Path.Combine(root, "invalid.json");
                File.WriteAllText(invalid, "{\"Token\":\"" + new string('0', 64) + "\"}");
                await RunClient(cliPath, address, invalid, "", shouldReject: true);
                await RunClient(cliPath, address, first, "PROFILE 1 4");
                await RejectDuplicate(address, first);
            });
            Console.WriteLine("PASS restores credential identities across reconnect and reversed server restart order while rejecting forged credentials");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL stable identities: " + error.Message); return 1; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static void Arrange(string root)
    {
        NetworkProjectTests.Write(root, "woorf.project.json", "{}");
        NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
            local query = Instance.new("RemoteFunction", game:GetService("ReplicatedStorage")); query.Name = "Profile"
            local store = game:GetService("DataStoreService"):GetDataStore("Visits")
            query.OnServerInvoke = function(player)
                return store:UpdateAsync(tostring(player.UserId), function(old) return (old or 0) + 1 end)
            end
            """);
        NetworkProjectTests.WriteClient(root, """
            local player = game:GetService("Players").LocalPlayer
            local count = game:GetService("ReplicatedStorage").Profile:InvokeServer()
            print("PROFILE " .. player.UserId .. " " .. count)
            player:Kick()
            """);
    }

    private static async Task RunServer(string cliPath, string root, Func<string, Task> run)
    {
        using var host = Start(typeof(StableIdentityTests).Assembly.Location, "--reference-host", root);
        var error = host.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            var line = await host.StandardOutput.ReadLineAsync(timeout.Token);
            if (line is null || !line.StartsWith("Listening ")) throw new Exception("Identity host did not start: " + line);
            await run(line[10..]);
            await host.StandardInput.WriteLineAsync("stop");
            await host.WaitForExitAsync(timeout.Token);
            if (host.ExitCode != 0 || await error != "") throw new Exception("Identity host failed: " + await error);
        }
        finally { if (!host.HasExited) { host.Kill(true); await host.WaitForExitAsync(); } }
    }

    private static async Task RunClient(string cliPath, string address, string identity, string expected, bool shouldReject = false)
    {
        using var client = Start(cliPath, "--connect", address, "--identity-file", identity);
        var output = client.StandardOutput.ReadToEndAsync(); var error = client.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        try
        {
            await client.WaitForExitAsync(timeout.Token);
            if ((await output).Trim() != expected || client.ExitCode != (shouldReject ? 1 : 0) || (!shouldReject && await error != ""))
                throw new Exception("Credential client failed: " + await output + await error);
            if (!shouldReject && !File.Exists(identity)) throw new Exception("Client did not persist its credential.");
        }
        finally { if (!client.HasExited) { client.Kill(true); await client.WaitForExitAsync(); } }
    }

    private static async Task RejectDuplicate(string address, string path)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var credential = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var token = credential.RootElement.GetProperty("Token").GetString();
        var hello = System.Text.Json.JsonSerializer.Serialize(new { Kind = "hello", Credential = token });
        using var original = new SocketTestClient();
        await original.Connect(address, timeout.Token); await original.Send(hello, timeout.Token);
        using var welcome = await original.Read(timeout.Token) ?? throw new Exception("Original identity rejected.");
        using var duplicate = new SocketTestClient();
        await duplicate.Connect(address, timeout.Token); await duplicate.Send(hello, timeout.Token);
        using var rejected = await duplicate.Read(timeout.Token);
        if (rejected is not null) throw new Exception("Duplicate active identity accepted.");
        var query = welcome.RootElement.GetProperty("Nodes").EnumerateArray()
            .Single(node => node.GetProperty("Name").GetString() == "Profile").GetProperty("Id").GetInt32();
        await original.Send($"{{\"Kind\":\"remote\",\"Remote\":{query},\"Arguments\":[{{\"Kind\":\"string\",\"Text\":\"request\"}},{{\"Kind\":\"number\",\"Number\":1}}]}}", timeout.Token);
        while (true)
        {
            using var reply = await original.Read(timeout.Token) ?? throw new Exception("Original identity was disconnected.");
            if (reply.RootElement.GetProperty("Kind").GetString() == "remote") break;
        }
    }

    private static Process Start(string assembly, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(assembly);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start)!;
    }
}
