using System.Diagnostics;

static class GraphicsIntegrationTests
{
    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-graphics-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var script = Path.Combine(root, "scene.luau");
            File.WriteAllText(script, "local p = Instance.new('Part', workspace) p.Anchored = true p.Size = Vector3.new(8,8,8) p.Position = Vector3.new(0,2,0)");
            var image = await Capture(cliPath, script, root, "box", 1);
            var background = Pixel(image, 5, 5);
            var center = Pixel(image, 320, 240);
            if (center == background) throw new Exception("Workspace box did not cover the camera target.");
            var source = File.ReadAllText(script);
            File.WriteAllText(script, source + "\np.Shape = Enum.PartType.Ball");
            var sphere = await Capture(cliPath, script, root, "sphere", 1);
            if (CountGeometry(sphere) >= CountGeometry(image) * 0.9)
                throw new Exception("A sphere was rendered as box geometry.");
            File.AppendAllText(script, "\np.Size = Vector3.new(8,8,16)");
            var stretched = await Capture(cliPath, script, root, "sphere-size", 1);
            if (!sphere.SequenceEqual(stretched)) throw new Exception("Sphere rendering disagrees with the physics minimum-size radius rule.");
            File.AppendAllText(script, "\ntask.delay(0.05, function() p:Destroy() end)");
            var removed = await Capture(cliPath, script, root, "removed", 30);
            if (Pixel(removed, 320, 240) != Pixel(removed, 5, 5))
                throw new Exception("Destroyed geometry remained visible.");
            Console.WriteLine("PASS OpenGL renders Workspace boxes/spheres at physics size and removes destroyed Parts");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL OpenGL scene: " + error.Message); return 1; }
        finally { Directory.Delete(root, true); }
    }

    private static async Task<byte[]> Capture(string cliPath, string script, string root, string name, int frames)
    {
        var output = Path.Combine(root, name + ".bmp");
        var profile = Path.Combine(root, name + ".csv");
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { cliPath, script, "--render", "--hidden", "--width", "640", "--height", "480", "--frames", frames.ToString(), "--capture", output, "--profile", profile })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(true); await process.WaitForExitAsync(); throw; }
        if (process.ExitCode != 0) throw new Exception($"Renderer exited {process.ExitCode}: {await stdout}{await stderr}");
        var samples = File.ReadAllLines(profile);
        if (samples.Length != frames + 1 || !samples[0].Contains("simulation_ms") || !samples[0].Contains("present_ms"))
            throw new Exception("Frame profile must include every frame and separate simulation/render/present costs.");
        var bytes = File.ReadAllBytes(output);
        if (bytes.Length != 54 + 640 * 480 * 3 || bytes[0] != 'B' || bytes[1] != 'M')
            throw new Exception("Expected a complete 640x480 BMP framebuffer capture.");
        return bytes;
    }

    private static int Pixel(byte[] image, int x, int y)
    {
        var offset = 54 + ((479 - y) * 640 + x) * 3;
        return image[offset] | image[offset + 1] << 8 | image[offset + 2] << 16;
    }

    private static int CountGeometry(byte[] image)
    {
        var background = Pixel(image, 5, 5); var count = 0;
        for (var y = 0; y < 480; y++)
            for (var x = 0; x < 640; x++)
                if (Pixel(image, x, y) != background) count++;
        return count;
    }
}
