static class SchedulerProjectTests
{
    public static IEnumerable<(string Name, Action<string> Arrange, int Exit, string Output, string Error)> GetCases()
    {
        yield return ("shares one yielding module execution among concurrent requires", ArrangeSharedModule, 0, "module\nshared\n", "");
        yield return ("resumes all requiring tasks when a yielding module fails", ArrangeFailingModule, 0, "failure\nfailure\n", "");
        yield return ("starts later scripts while earlier scripts await children", ArrangeWaitingScripts, 0, "arrived\n", "");
        yield return ("defers project work until startup scripts have run", ArrangeDeferredScripts, 0, "startup\ndeferred\n", "");
        yield return ("reports startup script errors after a wait", root =>
            Write(root, "src/ServerScriptService/Main.server.luau", "task.wait(0.001); error('yielded startup failure')"),
            1, "", "yielded startup failure");
        yield return ("cancels a script's scheduled work when the script is destroyed", ArrangeDestroyedScript,
            0, "destroyed\n", "");
        yield return ("cancels a yielding callback when its script is destroyed", ArrangeDestroyedCallback,
            0, "destroyed\n", "");
        yield return ("releases module waiters when the loading script is destroyed", ArrangeDestroyedModuleLoader,
            0, "continued\n", "");
    }

    private static void ArrangeSharedModule(string root)
    {
        Write(root, "src/ReplicatedStorage/Shared.luau", """
            print("module")
            task.wait(0.001)
            return { Ready = true }
            """);
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local module = game:GetService("ReplicatedStorage").Shared
            local first, second
            task.spawn(function() first = require(module) end)
            task.spawn(function() second = require(module) end)
            assert(first == nil and second == nil)
            task.wait(0.003)
            assert(first.Ready and first == second and require(module) == first)
            print("shared")
            """);
    }

    private static void ArrangeFailingModule(string root)
    {
        Write(root, "src/ReplicatedStorage/Broken.luau", "task.wait(0.001); error('yielded module failure')");
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local module = game:GetService("ReplicatedStorage").Broken
            for i = 1, 2 do
                task.spawn(function()
                    local success, message = pcall(require, module)
                    assert(not success and string.find(message, "yielded module failure", 1, true))
                    print("failure")
                end)
            end
            """);
    }

    private static void ArrangeWaitingScripts(string root)
    {
        Write(root, "src/ServerScriptService/A.server.luau", """
            local child = workspace:WaitForChild("FromB", 1)
            assert(child and child.Parent == workspace)
            print("arrived")
            """);
        Write(root, "src/ServerScriptService/B.server.luau", """
            local child = Instance.new("Folder")
            child.Name = "FromB"
            child.Parent = workspace
            """);
    }

    private static void ArrangeDeferredScripts(string root)
    {
        Write(root, "src/ServerScriptService/A.server.luau", "task.defer(function() print('deferred') end)");
        Write(root, "src/ServerScriptService/B.server.luau", "print('startup')");
    }

    private static void ArrangeDestroyedScript(string root)
    {
        Write(root, "src/ServerScriptService/A.server.luau", "task.delay(0.01, function() print('must not run') end)");
        Write(root, "src/ServerScriptService/B.server.luau", """
            task.delay(0.001, function()
                game:GetService("ServerScriptService").A:Destroy()
                print("destroyed")
            end)
            """);
    }

    private static void ArrangeDestroyedCallback(string root)
    {
        Write(root, "src/ServerScriptService/A.server.luau", """
            workspace.ChildAdded:Connect(function()
                task.wait(0.01)
                print("callback survived destruction")
            end)
            """);
        Write(root, "src/ServerScriptService/B.server.luau", """
            task.delay(0.001, function() Instance.new("Folder", workspace) end)
            task.delay(0.002, function()
                game:GetService("ServerScriptService").A:Destroy()
                print("destroyed")
            end)
            """);
    }

    private static void ArrangeDestroyedModuleLoader(string root)
    {
        Write(root, "src/ReplicatedStorage/Slow.luau", "task.wait(0.02); return true");
        Write(root, "src/ServerScriptService/A.server.luau", """
            local module = game:GetService("ReplicatedStorage").Slow
            task.spawn(function() require(module) end)
            """);
        Write(root, "src/ServerScriptService/B.server.luau", """
            local module = game:GetService("ReplicatedStorage").Slow
            task.delay(0.001, function() game:GetService("ServerScriptService").A:Destroy() end)
            task.delay(0.002, function()
                pcall(require, module)
                print("continued")
            end)
            """);
    }

    private static void Write(string root, string relative, string source)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, source);
    }
}
