using System.Diagnostics;
using System.Numerics;
using Woorf.Graphics;
using Woorf.Graphics.Platform;

static class SkyGraphicsTests
{
    internal static int Run()
    {
        var folder = Path.Combine(Path.GetTempPath(), "woorf-sky-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var image = Path.Combine(folder, "red.bmp"); WriteFace(image, 255, 0, 0);
            var script = Path.Combine(folder, "sky.luau");
            File.WriteAllText(script, "local sky = Instance.new('Sky', game:GetService('Lighting'))\n"
                + "for _, face in {'SkyboxRt','SkyboxLf','SkyboxUp','SkyboxDn','SkyboxBk','SkyboxFt'} do sky[face] = "
                + System.Text.Json.JsonSerializer.Serialize(image.Replace('\\', '/')) + " end");
            var capture = Path.Combine(folder, "sky.bmp");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { Path.Combine(AppContext.BaseDirectory, "Woorf.Cli.dll"), script, "--render", "--hidden", "--frames", "2", "--width", "160", "--height", "120", "--capture", capture })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(20000)) { process.Kill(true); throw new Exception("Sky viewer timed out."); }
            if (process.ExitCode != 0) throw new Exception(error.GetAwaiter().GetResult() + output.GetAwaiter().GetResult());
            var pixels = File.ReadAllBytes(capture);
            var center = 54 + (60 * 480) + 80 * 3;
            if (pixels[center + 2] < 240 || pixels[center] > 10 || pixels[center + 1] > 10)
                throw new Exception("Six red sky images must render a red background.");
            CheckFaces(folder);
            Console.WriteLine("PASS scripted Lighting sky renders textured background through the real OpenGL viewer");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL sky graphics: " + error.Message); return 1; }
        finally { Directory.Delete(folder, true); }
    }
    private static void CheckFaces(string folder)
    {
        var colors = new (byte R, byte G, byte B)[] { (255, 0, 0), (0, 255, 255), (0, 255, 0), (255, 0, 255), (0, 0, 255), (255, 255, 0) };
        var paths = Enumerable.Range(0, 6).Select(index => Path.Combine(folder, index + ".bmp")).ToArray();
        for (var index = 0; index < paths.Length; index++) WriteFace(paths[index], colors[index].R, colors[index].G, colors[index].B);
        using var window = new OpenGlWindow("Sky face checks", 160, 120, false);
        using var renderer = new OpenGlRenderer(window);
        var camera = new PerspectiveCamera { Position = Vector3.Zero };
        var directions = new[] { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ };
        var sky = new Skybox(paths, Vector3.Zero);
        for (var index = 0; index < directions.Length; index++)
        {
            camera.Target = directions[index];
            var pixels = Capture(renderer, camera, sky, folder);
            CheckCenter(pixels, colors[index], "Sky face direction " + index);
        }
        camera.Target = -Vector3.UnitZ;
        var origin = Capture(renderer, camera, sky, folder);
        camera.Position = new(100, 20, -40); camera.Target = camera.Position - Vector3.UnitZ;
        if (!origin.SequenceEqual(Capture(renderer, camera, sky, folder))) throw new Exception("Sky must not move when only camera position changes.");
        CheckCenter(Capture(renderer, camera, new Skybox(paths, new(0, 90, 0)), folder), colors[0], "Sky Y rotation in degrees");
        camera.Position = Vector3.Zero; camera.Target = -Vector3.UnitX;
        CheckCenter(Capture(renderer, camera, new Skybox(paths, new(90, 90, 90)), folder), colors[0], "Sky rotations must apply Y then X then Z");
        CheckVerticalImage(renderer, camera, paths, folder);
    }
    private static void CheckVerticalImage(OpenGlRenderer renderer, PerspectiveCamera camera, string[] paths, string folder)
    {
        var data = File.ReadAllBytes(paths[5]);
        for (var pixel = 54; pixel < 60; pixel += 3) { data[pixel] = 255; data[pixel + 1] = 0; data[pixel + 2] = 0; }
        for (var pixel = 62; pixel < 68; pixel += 3) { data[pixel] = 0; data[pixel + 1] = 0; data[pixel + 2] = 255; }
        var striped = Path.Combine(folder, "striped.bmp"); File.WriteAllBytes(striped, data);
        paths[5] = striped; camera.Position = Vector3.Zero; camera.Target = new(0, 0.7f, -1);
        var pixels = Capture(renderer, camera, new Skybox(paths, Vector3.Zero), folder);
        var center = 54 + 60 * 480 + 80 * 3;
        if (pixels[center + 2] < pixels[center] + 100) throw new Exception("Sky images must keep their top edge above their bottom edge.");
    }
    private static byte[] Capture(OpenGlRenderer renderer, PerspectiveCamera camera, Skybox sky, string folder)
    {
        var path = Path.Combine(folder, "face.bmp"); renderer.Draw([], camera, sky); renderer.Capture(path);
        return File.ReadAllBytes(path);
    }
    private static void CheckCenter(byte[] pixels, (byte R, byte G, byte B) color, string context)
    {
        var center = 54 + 60 * 480 + 80 * 3;
        if (Math.Abs(pixels[center] - color.B) > 4 || Math.Abs(pixels[center + 1] - color.G) > 4 || Math.Abs(pixels[center + 2] - color.R) > 4)
            throw new Exception(context + " sampled the wrong face.");
    }
    internal static void WriteFace(string path, byte r, byte g, byte b)
    {
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write((ushort)0x4D42); writer.Write(70); writer.Write(0); writer.Write(54);
        writer.Write(40); writer.Write(2); writer.Write(2); writer.Write((ushort)1); writer.Write((ushort)24);
        writer.Write(0); writer.Write(16); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
        for (var row = 0; row < 2; row++)
        {
            for (var column = 0; column < 2; column++) { writer.Write(b); writer.Write(g); writer.Write(r); }
            writer.Write((ushort)0);
        }
    }
}
