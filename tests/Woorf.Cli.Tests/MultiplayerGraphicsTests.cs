using System.Diagnostics;

static class MultiplayerGraphicsTests
{
    internal static async Task<int> RunAsync(string cliPath, bool isVisible = false)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-multiplayer-graphics-" + Guid.NewGuid().ToString("N"));
        Process? process = null;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src/ServerScriptService"));
            File.WriteAllText(Path.Combine(root, "woorf.project.json"), "{}");
            File.WriteAllText(Path.Combine(root, "src/ServerScriptService/Main.server.luau"), """
                workspace.Gravity = 80
                workspace:SetAttribute("CharacterSpawn", Vector3.new(0, 3, 0))
                local floor = Instance.new("Part", workspace)
                floor.Anchored = true; floor.Size = Vector3.new(100,1,100)
                local time = 0
                game:GetService("RunService").Heartbeat:Connect(function(delta)
                    time += delta
                    for _, player in game:GetService("Players"):GetPlayers() do
                        local root = player.Character and player.Character.PrimaryPart
                        if root then root.Position = Vector3.new(player.UserId * 4 + math.sin(time) * 0.5, root.Position.Y, 0) end
                    end
                end)
                """);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in new[] { cliPath, root, "--play", "--clients", "2", "--frames", "120",
                "--capture", Path.Combine(root, "view.bmp"), "--profile", Path.Combine(root, "view.csv") }) start.ArgumentList.Add(arg);
            if (!isVisible) start.ArgumentList.Add("--hidden");
            process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            if (isVisible) await CheckVisibleWindows(process, timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            if (process.ExitCode != 0) throw new Exception(await output + await error);
            for (var index = 1; index <= 2; index++) CheckCapture(root, index);
            Console.WriteLine(isVisible ? "PASS multiplayer launcher displays two visible player windows side by side"
                : "PASS two graphical clients render both shared-server characters and exit together");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL multiplayer graphics: " + error.Message); return 1; }
        finally
        {
            if (process is not null) { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } process.Dispose(); }
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static async Task CheckVisibleWindows(Process launcher, CancellationToken cancellation)
    {
        while (!launcher.HasExited)
        {
            var windows = PlayerWindowProbe.Capture(launcher.Id);
            if (windows.Length == 2 && windows.All(window => window.IsVisible))
            {
                var ordered = windows.OrderBy(window => window.Left).ToArray();
                if (ordered[0].Top == ordered[1].Top && ordered[1].Left - ordered[0].Left >= 640) return;
            }
            await Task.Delay(25, cancellation);
        }
        throw new Exception("Multiplayer launcher exited without displaying two visible player windows side by side.");
    }

    private static void CheckCapture(string root, int index)
    {
        var frame = File.ReadAllLines(Path.Combine(root, $"view.player{index}.csv"))[^1].Split(',');
        if (frame[^2] != "17" || frame[^1] != "2")
            throw new Exception($"Player {index} must render floor plus two eight-part avatars with their colliders hidden.");
        var image = File.ReadAllBytes(Path.Combine(root, $"view.player{index}.bmp"));
        if (image.Length < 54 || image[0] != 'B' || image[1] != 'M') throw new Exception("Missing graphical client capture.");
    }
}
