using System.Diagnostics;
using System.Text.Json;

static class BrowserExportTests
{
    internal static int Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-browser-export-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            CheckCustomRoot(root);
            CheckRejectedRoot(root);
            Console.WriteLine("PASS browser exporter packages project sources and rejects escaping roots");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL browser export: " + error.Message); return 1; }
        finally { Directory.Delete(root, true); }
    }

    private static void CheckCustomRoot(string root)
    {
        var project = Path.Combine(root, "game");
        Write(project, "woorf.project.json", "{\"name\":\"Browser fixture\",\"root\":\"game-source\"}");
        Write(project, "game-source/ServerScriptService/Main.server.luau", "print('packaged script')");
        Write(project, "game-source/Workspace/Scene/init.meta.json", "{\"className\":\"Model\"}");
        Write(project, ".woorf/auth/private.json", "secret");
        Write(project, "notes.txt", "not a runtime file");
        var output = Path.Combine(root, "bundle", "game.json");
        var result = Export(project, output);
        if (result.Exit != 0) throw new Exception("Project export failed: " + result.Error);
        using var document = JsonDocument.Parse(File.ReadAllText(output));
        var files = document.RootElement.GetProperty("files");
        if (files.GetProperty("game-source/ServerScriptService/Main.server.luau").GetString() != "print('packaged script')")
            throw new Exception("Export lost custom-root script content.");
        if (files.GetProperty("game-source/Workspace/Scene/init.meta.json").GetString() != "{\"className\":\"Model\"}")
            throw new Exception("Export lost instance metadata.");
        if (files.EnumerateObject().Count() != 3 || files.GetProperty("woorf.project.json").GetString() is null)
            throw new Exception("Export must contain only manifest and runtime source files.");
    }

    private static void CheckRejectedRoot(string root)
    {
        var project = Path.Combine(root, "invalid");
        Write(project, "woorf.project.json", "{\"root\":\"../outside\"}");
        Write(root, "outside/ServerScriptService/Secret.server.luau", "print('private')");
        var output = Path.Combine(root, "rejected.json");
        var result = Export(project, output);
        if (result.Exit == 0 || File.Exists(output)) throw new Exception("Exporter accepted a root outside its project.");
    }

    private static (int Exit, string Error) Export(string project, string output)
    {
        var start = new ProcessStartInfo("pwsh") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "-NoProfile", "-File", Path.GetFullPath("scripts/export-browser-game.ps1"), "-Project", project, "-Output", output })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(15000)) { process.Kill(true); throw new Exception("Browser exporter timed out."); }
        return (process.ExitCode, stderr.GetAwaiter().GetResult() + stdout.GetAwaiter().GetResult());
    }

    private static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
