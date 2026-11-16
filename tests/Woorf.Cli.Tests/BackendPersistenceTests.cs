using System.Diagnostics;

static class BackendPersistenceTests
{
    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-backend-store-" + Guid.NewGuid().ToString("N"));
        try
        {
            NetworkProjectTests.Write(root, "woorf.project.json", "{\"name\":\"Persistence\",\"root\":\"src\"}");
            NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
                local store = game:GetService("DataStoreService"):GetDataStore("Profiles")
                store:SetAsync("player", {Coins = 10})
                game:BindToClose(function()
                    task.wait(0.01)
                    store:SetAsync("closed", true)
                    print("saved")
                end)
                """);
            await RunProcess(cliPath, root, "saved\n");
            NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
                local service = game:GetService("DataStoreService")
                local store = service:GetDataStore("Profiles")
                assert(store:GetAsync("player").Coins == 10 and store:GetAsync("closed") == true)
                assert(service:GetDataStore("Profiles", "other"):GetAsync("player") == nil)
                store:RemoveAsync("count")
                assert(store:IncrementAsync("count", 2) == 2 and store:IncrementAsync("count", 3) == 5)
                assert(not pcall(function() store:SetAsync("bad", workspace) end))
                print("restored")
                """);
            await RunProcess(cliPath, root, "restored\n");
            NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
                local store = game:GetService("DataStoreService"):GetDataStore("Profiles")
                for index = 1, 25 do store:IncrementAsync("race", 1) end
                print("incremented")
                """);
            await Task.WhenAll(RunProcess(cliPath, root, "incremented\n"), RunProcess(cliPath, root, "incremented\n"));
            NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
                assert(game:GetService("DataStoreService"):GetDataStore("Profiles"):GetAsync("race") == 50)
                print("counted")
                """);
            await RunProcess(cliPath, root, "counted\n");
            NetworkProjectTests.Write(root, "src/ServerScriptService/A.server.luau", """
                local part = Instance.new("Part", workspace); part.Name = "Expired"
                game:GetService("Debris"):AddItem(part, 0.01)
                script:Destroy()
                """);
            NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
                task.wait(0.03)
                assert(workspace:FindFirstChild("Expired") == nil)
                print("cleaned")
                """);
            await RunProcess(cliPath, root, "cleaned\n");
            await CheckAtomicUpdates(cliPath, root);
            Console.WriteLine("PASS persists backend data through process shutdown and preserves Debris cleanup after script destruction");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL persists backend data through process shutdown and preserves Debris cleanup after script destruction: " + error.Message);
            return 1;
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static async Task CheckAtomicUpdates(string cliPath, string root)
    {
        NetworkProjectTests.Write(root, "src/ServerScriptService/A.server.luau", "");
        NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
            local store = game:GetService("DataStoreService"):GetDataStore("AtomicProfiles")
            for index = 1, 25 do
                store:UpdateAsync("profile", function(old)
                    old = old or {Coins = 0, Items = 0}
                    return {Coins = old.Coins + 7, Items = old.Items + 1}
                end)
            end
            print("updated")
            """);
        await Task.WhenAll(RunProcess(cliPath, root, "updated\n"), RunProcess(cliPath, root, "updated\n"));
        NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
            local value = game:GetService("DataStoreService"):GetDataStore("AtomicProfiles"):GetAsync("profile")
            assert(value.Coins == 350 and value.Items == 50)
            print("atomic")
            """);
        await RunProcess(cliPath, root, "atomic\n");
    }

    private static async Task RunProcess(string cliPath, string root, string expected)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(cliPath); start.ArgumentList.Add(root);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw; }
        if (process.ExitCode != 0 || (await output).Replace("\r\n", "\n") != expected || await error != "")
            throw new Exception($"Backend process expected {expected.Trim()}: " + await output + await error);
    }
}
