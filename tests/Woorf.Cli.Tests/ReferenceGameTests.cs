using System.Diagnostics;
using Woorf.Engine.Networking;
using Woorf.Engine.Projects;

static class ReferenceGameTests
{
    internal static int RunHost(string root)
    {
        using var cancellation = new CancellationTokenSource();
        _ = Task.Run(async () => { await Console.In.ReadLineAsync(); cancellation.Cancel(); });
        return SocketTransport.Listen(ProjectLoader.Load(root), "127.0.0.1:0", cancellation.Token);
    }

    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-reference-" + Guid.NewGuid().ToString("N"));
        try
        {
            var source = Path.Combine(FindRepository(), "examples", "round-game");
            if (!Directory.Exists(source)) throw new Exception("Reference game project is missing.");
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(source, file);
                if (relative.Split(Path.DirectorySeparatorChar).Contains(".woorf")) continue;
                NetworkProjectTests.Write(root, relative, File.ReadAllText(file));
            }
            await RunSession(cliPath, root, 7, 1, false);
            await RunSession(cliPath, root, 14, 2, true);
            Console.WriteLine("PASS reference game validates requests and restores round rewards and inventory after TCP server restart");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL reference game restart: " + error.Message);
            return 1;
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static async Task RunSession(string cliPath, string root, int coins, int rounds, bool isReversed)
    {
        var processes = new List<Process>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        try
        {
            var host = Start(typeof(ReferenceGameTests).Assembly.Location, "--reference-host", root);
            processes.Add(host);
            var hostError = host.StandardError.ReadToEndAsync();
            var listening = await host.StandardOutput.ReadLineAsync(timeout.Token);
            if (listening is null || !listening.StartsWith("Listening "))
                throw new Exception("Reference host did not listen: " + listening);
            var firstId = isReversed ? 2 : 1;
            var secondId = isReversed ? 1 : 2;
            var first = Start(cliPath, "--connect", listening[10..], "--identity-file", Path.Combine(root, $"client{firstId}.json"));
            processes.Add(first);
            var firstError = first.StandardError.ReadToEndAsync();
            await ExpectLine(first, $"JOINED profile-{firstId}", timeout.Token);
            var second = Start(cliPath, "--connect", listening[10..], "--identity-file", Path.Combine(root, $"client{secondId}.json"));
            processes.Add(second);
            var secondError = second.StandardError.ReadToEndAsync();
            await ExpectLine(second, $"JOINED profile-{secondId}", timeout.Token);
            await ExpectLine(second, $"COMPLETE profile-{secondId} {coins} {rounds} {rounds}", timeout.Token);
            await ExpectLine(first, $"COMPLETE profile-{firstId} {coins} {rounds} {rounds}", timeout.Token);
            var departing = isReversed ? second : first;
            await departing.WaitForExitAsync(timeout.Token);
            if (departing.ExitCode != 0) throw new Exception("Departing client failed.");
            await ExpectLine(host, "SAVED leave profile-1", timeout.Token);
            var hostOutput = host.StandardOutput.ReadToEndAsync();
            await host.StandardInput.WriteLineAsync("stop");
            await host.WaitForExitAsync(timeout.Token);
            await first.WaitForExitAsync(timeout.Token);
            await second.WaitForExitAsync(timeout.Token);
            var output = await hostOutput;
            if (host.ExitCode != 0 || await hostError != "" || first.ExitCode != 0 || await firstError != "" || second.ExitCode != 0 || await secondError != "")
                throw new Exception("Reference session failed: " + await hostError + await secondError);
            if (!output.Contains("SAVED shutdown profile-2"))
                throw new Exception("Missing leave/shutdown saves: " + output);
        }
        finally
        {
            foreach (var process in processes)
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    await process.WaitForExitAsync();
                }
                process.Dispose();
            }
        }
    }

    private static async Task ExpectLine(Process process, string expected, CancellationToken cancellation)
    {
        var actual = await process.StandardOutput.ReadLineAsync(cancellation);
        if (actual != expected) throw new Exception($"Expected {expected}, got {actual}");
    }

    private static Process Start(string assembly, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        start.ArgumentList.Add(assembly);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start)!;
    }

    private static string FindRepository()
    {
        for (var path = new DirectoryInfo(AppContext.BaseDirectory); path is not null; path = path.Parent)
            if (File.Exists(Path.Combine(path.FullName, "src", "Woorf.Engine", "Woorf.Engine.csproj"))) return path.FullName;
        throw new Exception("Cannot find reference game repository.");
    }
}
