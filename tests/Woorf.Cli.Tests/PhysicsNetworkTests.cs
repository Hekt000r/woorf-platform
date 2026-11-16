using System.Diagnostics;

static class PhysicsNetworkTests
{
    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-physics-" + Guid.NewGuid().ToString("N"));
        var processes = new List<Process>();
        try
        {
            var repository = new DirectoryInfo(AppContext.BaseDirectory);
            while (repository is not null && !Directory.Exists(Path.Combine(repository.FullName, "examples"))) repository = repository.Parent;
            var source = Path.Combine(repository!.FullName, "examples", "physics-boxes");
            if (!Directory.Exists(source)) throw new Exception("Physics scene is missing.");
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (Path.GetRelativePath(source, file).Split(Path.DirectorySeparatorChar).Contains(".woorf")) continue;
                NetworkProjectTests.Write(root, Path.GetRelativePath(source, file), File.ReadAllText(file));
            }
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            var server = Start(typeof(PhysicsNetworkTests).Assembly.Location, "--reference-host", root);
            processes.Add(server);
            var serverError = server.StandardError.ReadToEndAsync();
            var line = await server.StandardOutput.ReadLineAsync(timeout.Token);
            if (line is null || !line.StartsWith("Listening ")) throw new Exception("Physics server did not listen: " + line);
            var clients = new[] { Start(cliPath, "--connect", line[10..]), Start(cliPath, "--connect", line[10..]) };
            processes.AddRange(clients);
            foreach (var client in clients)
            {
                var output = client.StandardOutput.ReadToEndAsync(); var error = client.StandardError.ReadToEndAsync();
                await client.WaitForExitAsync(timeout.Token);
                if (client.ExitCode != 0 || (await output).Trim() != "physics-client" || await error != "")
                    throw new Exception("Physics client failed: " + await output + await error);
            }
            if (await server.StandardOutput.ReadLineAsync(timeout.Token) != "physics-server") throw new Exception("Physics scene did not complete.");
            await server.StandardInput.WriteLineAsync("stop"); await server.WaitForExitAsync(timeout.Token);
            if (server.ExitCode != 0 || await serverError != "") throw new Exception("Physics server failed: " + await serverError);
            Console.WriteLine("PASS physics replicates falling and settled boxes with velocities and materials to two TCP clients");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL physics TCP scene: " + error.Message); return 1; }
        finally
        {
            foreach (var process in processes)
            {
                if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); }
                process.Dispose();
            }
            if (Directory.Exists(root)) Directory.Delete(root, true);
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
