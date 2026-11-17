using System.Diagnostics;

static class CharacterGraphicsTests
{
    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-character-view-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = Path.Combine(root, "character.luau");
            var capture = Path.Combine(root, "character.bmp");
            var profile = Path.Combine(root, "character.csv");
            File.WriteAllText(source, "workspace.Gravity = 0\nworkspace:SetAttribute('CharacterSpawn', Vector3.new(0,2,0))");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in new[] { cliPath, source, "--play", "--hidden", "--frames", "4", "--width", "640", "--height", "480", "--capture", capture, "--profile", profile }) start.ArgumentList.Add(arg);
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch { process.Kill(true); await process.WaitForExitAsync(); throw; }
            if (process.ExitCode != 0) throw new Exception(await error + await output);
            var pixels = File.ReadAllBytes(capture);
            var center = 54 + (239 * 640 + 320) * 3;
            if (pixels.AsSpan(center, 3).SequenceEqual(pixels.AsSpan(54, 3)))
                throw new Exception("The playable character was not visible at the follow-camera target.");
            var columns = File.ReadAllLines(profile)[^1].Split(',');
            if (columns[^1] != "2" || columns[^2] != "8")
                throw new Exception("Play mode must replace its physical collider with eight centrally batched visual parts.");
            Console.WriteLine("PASS graphical play mode spawns and draws a character using two instanced batches");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL character graphics: " + error.Message); return 1; }
        finally { Directory.Delete(root, true); }
    }
}
