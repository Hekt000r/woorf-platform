using System.Diagnostics;

static class ProjectValidationTests
{
    public static async Task<int> RunAsync(string cliPath)
    {
        // These cases catch silently dropped objects, ignored configuration, and invalid projects accepted as games.
        var cases = new (string Name, Action<string> Arrange, int Exit, string Output, string Error)[]
        {
            ("rejects duplicate sibling names", root =>
            {
                Write(root, "src/Workspace/Same.server.luau", "");
                Write(root, "src/Workspace/Same.luau", "");
            }, 1, "", "Duplicate"),
            ("rejects malformed folder metadata", root => Write(root, "src/Workspace/Bad/init.meta.json", "{"), 1, "", "Invalid instance metadata"),
            ("rejects missing manifests", root => File.Delete(Path.Combine(root, "woorf.project.json")), 1, "", "woorf.project.json"),
            ("rejects missing source directory", root => Write(root, "woorf.project.json", "{\"root\":\"absent\"}"), 1, "", "absent"),
            ("loads custom root with default folder type", root =>
            {
                Write(root, "woorf.project.json", "{\"root\":\"content\"}");
                Write(root, "content/Workspace/Folder/Hello.server.luau", "assert(script.Parent.ClassName == 'Folder'); print('custom')");
            }, 0, "custom\n", ""),
            ("rejects linked source directories", root =>
            {
                Directory.CreateDirectory(Path.Combine(root, "src", "Workspace"));
                CreateDirectoryLink(Path.Combine(root, "src", "Workspace", "Link"), Path.GetTempPath());
            }, 1, "", "Linked paths"),
        };
        var failedCount = 0;
        foreach (var test in cases)
        {
            var root = Path.Combine(Path.GetTempPath(), "woorf-validation-" + Guid.NewGuid().ToString("N"));
            try
            {
                Write(root, "woorf.project.json", "{}");
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
            finally
            {
                var link = Path.Combine(root, "src", "Workspace", "Link");
                if (Directory.Exists(link) && File.GetAttributes(link).HasFlag(FileAttributes.ReparsePoint))
                    Directory.Delete(link);
                Directory.Delete(root, true);
            }
        }
        return failedCount;
    }

    private static void Write(string root, string relative, string source)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, source);
    }

    private static void CreateDirectoryLink(string link, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(link, target);
            return;
        }
        var start = new ProcessStartInfo("cmd.exe")
        {
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
        };
        foreach (var argument in new[] { "/c", "mklink", "/J", link, target })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new Exception("Could not arrange linked-directory fixture: " + process.StandardError.ReadToEnd());
    }
}
