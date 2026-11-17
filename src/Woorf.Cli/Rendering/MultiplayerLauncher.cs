using System.Diagnostics;

internal static class MultiplayerLauncher
{
    internal static int Run(RenderOptions options)
    {
        if (!Directory.Exists(options.Source)) throw new ArgumentException("Multiplayer play requires a project directory.");
        var processes = new List<Process>();
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, args) => { args.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            var server = Start([options.Source, "--listen", "127.0.0.1:0", "--characters"]); processes.Add(server);
            var serverError = server.StandardError.ReadToEndAsync();
            var address = ReadListeningAddress(server, cancellation.Token);
            _ = ForwardOutput(server.StandardOutput);
            var clients = new List<Process>();
            var completed = new List<Task<bool>>();
            for (var index = 0; index < options.Clients; index++)
            {
                var client = Start(BuildClientArguments(options, address, index), options.IsVisible);
                clients.Add(client); processes.Add(client);
                completed.Add(ReadClientOutput(client.StandardOutput));
                _ = client.StandardError.ReadToEndAsync().ContinueWith(result => { if (result.Result.Length > 0) Console.Error.Write(result.Result); });
            }
            Console.WriteLine($"Multiplayer: {clients.Count} clients connected to {address}. Focus a window to control its player.");
            while (!cancellation.IsCancellationRequested && !server.HasExited && clients.Any(client => !client.HasExited))
            {
                if (options.Frames == 0 && clients.Any(client => client.HasExited)) break;
                if (options.Frames > 0 && completed.All(task => task.IsCompleted))
                {
                    if (completed.Any(task => !task.GetAwaiter().GetResult())) return 1;
                    foreach (var client in clients.Where(client => !client.HasExited)) client.StandardInput.WriteLine("finish");
                    foreach (var client in clients) client.WaitForExit();
                    break;
                }
                Thread.Sleep(10);
            }
            if (server.HasExited) { Console.Error.Write(serverError.GetAwaiter().GetResult()); return 1; }
            return clients.Any(client => client.HasExited && client.ExitCode != 0) ? 1 : 0;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
            foreach (var process in processes) StopProcess(process);
        }
    }

    private static void StopProcess(Process process)
    {
        try { if (!process.HasExited) { process.Kill(true); process.WaitForExit(); } }
        catch (InvalidOperationException) { }
        finally { process.Dispose(); }
    }

    private static async Task<bool> ReadClientOutput(StreamReader reader)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line == "ViewerCompleted") return true;
            Console.WriteLine(line);
        }
        return false;
    }

    private static async Task ForwardOutput(StreamReader reader)
    {
        try { while (await reader.ReadLineAsync() is { } line) Console.WriteLine(line); }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    private static Process Start(IEnumerable<string> arguments, bool isVisible = false)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true,
            WindowStyle = isVisible ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden };
        start.ArgumentList.Add(typeof(RenderCli).Assembly.Location);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start) ?? throw new IOException("Could not start multiplayer process.");
    }

    private static string ReadListeningAddress(Process server, CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (true)
        {
            var line = server.StandardOutput.ReadLineAsync(timeout.Token).AsTask().GetAwaiter().GetResult();
            if (line is null) throw new IOException("Multiplayer server exited before listening.");
            if (line.StartsWith("Listening ")) return line[10..];
            Console.WriteLine(line);
        }
    }

    private static IEnumerable<string> BuildClientArguments(RenderOptions options, string address, int index)
    {
        var args = new List<string> { "--connect", address, "--play", "--window-slot", index.ToString(),
            "--width", Math.Min(options.Width, 640).ToString(), "--height", options.Height.ToString() };
        if (!options.IsVisible) args.Add("--hidden");
        if (options.Frames > 0) { args.Add("--frames"); args.Add(options.Frames.ToString()); }
        if (options.Capture is not null) { args.Add("--capture"); args.Add(AddPlayerSuffix(options.Capture, index)); }
        if (options.Profile is not null) { args.Add("--profile"); args.Add(AddPlayerSuffix(options.Profile, index)); }
        return args;
    }

    private static string AddPlayerSuffix(string path, int index) =>
        Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path) + $".player{index + 1}" + Path.GetExtension(path));
}
