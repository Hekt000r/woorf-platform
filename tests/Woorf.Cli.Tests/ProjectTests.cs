using System.Diagnostics;

static class ProjectTests
{
    public static async Task<int> RunAsync(string cliPath)
    {
        var cases = new (string Name, Action<string> Arrange, int Exit, string Output, string Error)[]
        {
            ("loads project before running isolated server scripts", ArrangeProject, 0, "first\nsecond\n", ""),
            ("retains signal closures after the listener script returns", ArrangeSignalProject, 0,
                "once:A:One\nconnected:A:One\nconnected:A:Two\n", ""),
            ("rejects invalid project JSON", root => Write(root, "woorf.project.json", "{"), 1, "", "Invalid project manifest"),
            ("rejects unsupported service directories", root => Write(root, "src/Unknown/file.luau", ""), 1, "", "Unknown"),
            ("rejects invalid metadata classes", root => Write(root, "src/Workspace/Bad/init.meta.json", "{\"className\":\"Unknown\"}"), 1, "", "Unknown"),
            ("rejects source paths outside project", root => Write(root, "woorf.project.json", "{\"root\":\"../\"}"), 1, "", "root"),
            ("stops on script failure", root => Write(root, "src/ServerScriptService/main.server.luau", "error('project failure')"), 1, "", "project failure"),
        }.Concat(ModuleCases.GetCases()).Concat(SchedulerProjectTests.GetCases()).ToArray();
        var failedCount = 0;
        foreach (var test in cases)
        {
            var root = Path.Combine(Path.GetTempPath(), "woorf-project-" + Guid.NewGuid().ToString("N"));
            try
            {
                Write(root, "woorf.project.json", "{\"name\":\"TestGame\",\"root\":\"src\"}");
                Directory.CreateDirectory(Path.Combine(root, "src"));
                test.Arrange(root);
                var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add(cliPath); start.ArgumentList.Add(root);
                using var process = Process.Start(start)!;
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try { await process.WaitForExitAsync(timeout.Token); }
                catch (OperationCanceledException) { process.Kill(true); throw; }
                var output = (await outputTask).Replace("\r\n", "\n");
                var error = await errorTask;
                if (process.ExitCode != test.Exit || output != test.Output ||
                    (test.Error == "" ? error != "" : !error.Contains(test.Error)))
                    throw new Exception($"exit={process.ExitCode}, stdout={output}, stderr={error}");
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception error) { failedCount++; Console.Error.WriteLine($"FAIL {test.Name}: {error.Message}"); }
            finally { Directory.Delete(root, true); }
        }
        return failedCount;
    }

    private static void ArrangeProject(string root)
    {
        Write(root, "src/Workspace/Scene/init.meta.json", "{\"className\":\"Model\"}");
        Write(root, "src/ReplicatedStorage/Shared.luau", "error('module must not auto-run')");
        Write(root, "src/StarterGui/Menu.client.luau", "error('client must not auto-run')");
        Write(root, "src/ServerStorage/Stored.server.luau", "error('storage must not auto-run')");
        Write(root, "src/ServerScriptService/A.server.luau", """
            assert(game.Name == "TestGame" and game:IsLoaded())
            assert(script.Name == "A" and script:IsA("BaseScript") and script:IsA("LuaSourceContainer"))
            assert(script.Parent == game:GetService("ServerScriptService"))
            assert(workspace.Scene.ClassName == "Model")
            assert(game:GetService("ReplicatedStorage").Shared.ClassName == "ModuleScript")
            assert(game:GetService("StarterGui").Menu.ClassName == "LocalScript")
            assert(script.Parent.B.ClassName == "Script")
            Instance.new("Folder", workspace).Name = "CreatedByA"
            leaked = true
            print("first")
            """);
        Write(root, "src/ServerScriptService/B.server.luau", """
            assert(script.Name == "B" and leaked == nil)
            assert(workspace.CreatedByA.Parent == workspace)
            print("second")
            """);
    }

    private static void Write(string root, string relative, string source)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, source);
    }

    private static void ArrangeSignalProject(string root)
    {
        Write(root, "src/ServerScriptService/A.server.luau", """
            local listenerName = script.Name
            workspace.ChildAdded:Once(function(child)
                assert(script.Name == listenerName)
                print("once:" .. script.Name .. ":" .. child.Name)
            end)
            workspace.ChildAdded:Connect(function(child)
                assert(script.Name == listenerName)
                print("connected:" .. script.Name .. ":" .. child.Name)
            end)
            """);
        Write(root, "src/ServerScriptService/B.server.luau", """
            for _, name in {"One", "Two"} do
                local folder = Instance.new("Folder")
                folder.Name = name
                folder.Parent = workspace
            end
            """);
    }
}
