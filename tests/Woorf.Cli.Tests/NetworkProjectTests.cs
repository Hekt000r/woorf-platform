using System.Diagnostics;

static class NetworkProjectTests
{
    public static async Task<int> RunAsync(string cliPath)
    {
        var failures = 0;
        foreach (var test in GetCases())
        {
            var root = Path.Combine(Path.GetTempPath(), "woorf-network-" + Guid.NewGuid().ToString("N"));
            try
            {
                Write(root, "woorf.project.json", "{\"name\":\"NetworkGame\",\"root\":\"src\"}");
                Directory.CreateDirectory(Path.Combine(root, "src"));
                test.Arrange(root);
                var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var argument in new[] { cliPath, root, "--clients", "2" }) start.ArgumentList.Add(argument);
                using var process = Process.Start(start)!;
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try { await process.WaitForExitAsync(timeout.Token); }
                catch (OperationCanceledException) { process.Kill(true); throw; }
                var output = (await outputTask).Replace("\r\n", "\n");
                var error = await errorTask;
                if (process.ExitCode != 0 || output != test.Output || error != "")
                    throw new Exception($"exit={process.ExitCode}, stdout={output}, stderr={error}");
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception error) { failures++; Console.Error.WriteLine($"FAIL {test.Name}: {error.Message}"); }
            finally { Directory.Delete(root, true); }
        }
        return failures;
    }

    private static IEnumerable<(string Name, Action<string> Arrange, string Output)> GetCases()
    {
        yield return ("routes queued RemoteEvents between two isolated clients and authentic Players", ArrangeRoundtrip, "roundtrip\n");
        foreach (var test in NetworkValueCases.GetCases()) yield return test;
        foreach (var test in NetworkIsolationCases.GetCases()) yield return test;
    }

    private static void ArrangeRoundtrip(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local players = game:GetService("Players")
            assert(game:GetService("RunService"):IsServer() and not game:GetService("RunService"):IsClient())
            assert(players.LocalPlayer == nil)
            local joins, replies, broadcasts = 0, 0, 0
            players.PlayerAdded:Connect(function(player) joins += 1; assert(player.Parent == players) end)
            local remote = Instance.new("RemoteEvent", game:GetService("ReplicatedStorage")); remote.Name = "Message"
            remote.OnServerEvent:Connect(function(player, kind, id)
                assert(player.UserId == id and player.Name == "Player" .. id)
                assert(player == players:GetPlayers()[id])
                if kind == "hello" then remote:FireClient(player, "reply", id)
                elseif kind == "reply" then
                    replies += 1
                    if replies == 2 then remote:FireAllClients("all") end
                elseif kind == "all" then
                    broadcasts += 1
                    if broadcasts == 2 then assert(joins == 2); print("roundtrip") end
                else error("unexpected remote payload") end
            end)
            """);
        WriteClient(root, """
            local players = game:GetService("Players")
            local player = players.LocalPlayer
            assert(game:GetService("RunService"):IsClient() and not game:GetService("RunService"):IsServer())
            assert(#players:GetPlayers() == 2 and player.Name == "Player" .. player.UserId)
            assert(script.Parent == player.PlayerScripts)
            local remote = game:GetService("ReplicatedStorage").Message
            remote.OnClientEvent:Connect(function(kind, id)
                if kind == "reply" then assert(id == player.UserId)
                else assert(kind == "all" and id == nil) end
                remote:FireServer(kind, player.UserId)
            end)
            remote:FireServer("hello", player.UserId)
            """);
    }

    internal static void WriteClient(string root, string source) =>
        Write(root, "src/StarterPlayer/StarterPlayerScripts/Main.client.luau", source);

    internal static void Write(string root, string relative, string source)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, source);
    }
}
