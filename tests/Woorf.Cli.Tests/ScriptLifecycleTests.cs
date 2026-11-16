using System.Diagnostics;

static class ScriptLifecycleTests
{
    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-lifecycle-" + Guid.NewGuid().ToString("N"));
        try
        {
            NetworkProjectTests.Write(root, "woorf.project.json", "{\"root\":\"src\"}");
            NetworkProjectTests.Write(root, "src/ServerStorage/Worker.server.luau", """
                workspace:SetAttribute("Runs", (workspace:GetAttribute("Runs") or 0) + 1)
                workspace.ChildAdded:Connect(function() workspace:SetAttribute("Listeners", (workspace:GetAttribute("Listeners") or 0) + 1) end)
                task.delay(0.05, function() workspace:SetAttribute("Stale", true) end)
                """);
            NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
                local worker = game:GetService("ServerStorage").Worker:Clone()
                worker.Parent = workspace
                task.wait(0.02)
                assert(workspace:GetAttribute("Runs") == 1, "dynamic Script did not start")
                worker.Enabled = false
                task.wait(0.07)
                assert(workspace:GetAttribute("Stale") == nil, "disabled timer ran")
                worker.Disabled = false
                task.wait(0.02)
                assert(workspace:GetAttribute("Runs") == 2, "reenabled Script did not restart")
                local marker = Instance.new("Folder", workspace)
                assert(workspace:GetAttribute("Listeners") == 1, "old listener survived restart")
                worker.Parent = game:GetService("ServerStorage")
                task.wait(0.07)
                assert(workspace:GetAttribute("Stale") == nil, "inactive timer ran")
                print("server-lifecycle")
                """);
            await RunProcess(cliPath, root, "server-lifecycle\n");
            NetworkProjectTests.Write(root, "src/ServerStorage/Worker.server.luau", "error('dynamic-failure')");
            NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
                game:GetService("ServerStorage").Worker:Clone().Parent = workspace
                task.wait(0.01)
                """);
            await RunProcess(cliPath, root, "", expectedExit: 1, expectedError: "dynamic-failure");
            NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", "");
            NetworkProjectTests.Write(root, "src/ReplicatedStorage/Worker.client.luau", """
                local player = game:GetService("Players").LocalPlayer
                player:SetAttribute("Runs", (player:GetAttribute("Runs") or 0) + 1)
                """);
            NetworkProjectTests.WriteClient(root, """
                local player = game:GetService("Players").LocalPlayer
                local worker = game:GetService("ReplicatedStorage").Worker:Clone()
                worker.Enabled = false
                worker.Parent = player.PlayerGui
                task.wait(0.02)
                assert(player:GetAttribute("Runs") == nil)
                worker.Enabled = true
                task.wait(0.02)
                assert(player:GetAttribute("Runs") == 1, "dynamic LocalScript did not start")
                worker.Disabled = true
                task.wait(0.02)
                worker.Enabled = true
                task.wait(0.02)
                assert(player:GetAttribute("Runs") == 2)
                print("client-lifecycle")
                """);
            await RunProcess(cliPath, root, "client-lifecycle\n", ["--clients", "1"]);
            await CheckChain(cliPath, root);
            await CheckContexts(cliPath, root);
            Console.WriteLine("PASS starts stops and restarts dynamic server and client scripts without stale work");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL dynamic script lifecycle: " + error.Message); return 1; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static async Task CheckChain(string cliPath, string root)
    {
        NetworkProjectTests.Write(root, "src/ServerStorage/Chain.server.luau", "game:GetService('ServerStorage').Leaf:Clone().Parent = workspace");
        NetworkProjectTests.Write(root, "src/ServerStorage/Leaf.server.luau", "print('chained')");
        NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", "game:GetService('ServerStorage').Chain:Clone().Parent = workspace");
        await RunProcess(cliPath, root, "chained\n");
    }

    private static async Task CheckContexts(string cliPath, string root)
    {
        NetworkProjectTests.Write(root, "src/ReplicatedStorage/Worker.client.luau", "");
        NetworkProjectTests.Write(root, "src/ReplicatedStorage/Context.server.luau", """
            if game:GetService("RunService"):IsServer() then
                workspace:SetAttribute("ServerContext", true)
            else
                game:GetService("Players").LocalPlayer:SetAttribute("ClientContext", true)
            end
            """);
        NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
            local worker = game:GetService("ReplicatedStorage").Context
            assert(typeof(Enum.RunContext.Server) == "EnumItem")
            assert(worker.RunContext == Enum.RunContext.Legacy and worker.Enabled and not worker.Disabled)
            assert(not pcall(function() worker.RunContext = "Server" end))
            assert(not pcall(function() worker.RunContext = Enum.RunContext.Plugin end))
            worker.RunContext = Enum.RunContext.Server
            task.wait(0.03)
            assert(workspace:GetAttribute("ServerContext") == true)
            assert(worker:Clone().RunContext == Enum.RunContext.Server)
            worker.RunContext = Enum.RunContext.Client
            print("context-server")
            """);
        NetworkProjectTests.WriteClient(root, """
            task.wait(0.1)
            assert(game:GetService("Players").LocalPlayer:GetAttribute("ClientContext") == true)
            print("context-client")
            """);
        await RunProcess(cliPath, root, "context-server\ncontext-client\n", ["--clients", "1"]);
    }

    private static async Task RunProcess(string cliPath, string root, string expected, string[]? arguments = null, int expectedExit = 0, string expectedError = "")
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(cliPath); start.ArgumentList.Add(root);
        foreach (var argument in arguments ?? []) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); await process.WaitForExitAsync(); throw; }
        if (process.ExitCode != expectedExit || (await output).Replace("\r\n", "\n") != expected
            || (expectedError == "" ? await error != "" : !(await error).Contains(expectedError)))
            throw new Exception("Script process failed: " + await output + await error);
    }
}
