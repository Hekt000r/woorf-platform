using System.Diagnostics;
using System.Numerics;
using Woorf.Graphics;
using Woorf.Graphics.Platform;

internal static class GuiGraphicsTests
{
    internal static int Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "woorf-gui-gpu-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "src", "StarterGui"));
        try
        {
            if (typeof(Woorf.Graphics.OpenGlRenderer).Assembly.GetType("Woorf.Graphics.GuiRenderer") is null)
                throw new Exception("A central GUI renderer is required to draw GUI over the 3D framebuffer.");
            File.WriteAllText(Path.Combine(directory, "woorf.project.json"), "{}");
            var imagePath = Path.Combine(directory, "image.bmp"); WriteImage(imagePath);
            File.WriteAllText(Path.Combine(directory, "src", "StarterGui", "Hud.client.luau"), """
                local p=game:GetService('Players').LocalPlayer
                local s=Instance.new('ScreenGui', p.PlayerGui)
                local f=Instance.new('Frame',s)
                f.Position=UDim2.fromOffset(20,20); f.Size=UDim2.fromOffset(80,60)
                f.BackgroundColor3=Color3.new(1,0,0); f.BorderSizePixel=0
                local t=Instance.new('TextLabel',s)
                t.Position=UDim2.fromOffset(10,90); t.Size=UDim2.fromOffset(140,25)
                t.Text='Woorf GUI'; t.TextColor3=Color3.new(1,1,1); t.BackgroundTransparency=1; t.BorderSizePixel=0
                local image=Instance.new('ImageLabel',s)
                image.Position=UDim2.fromOffset(110,20); image.Size=UDim2.fromOffset(40,40)
                image.BackgroundTransparency=1; image.BorderSizePixel=0
                image.ImageRectOffset=Vector2.new(2,0); image.ImageRectSize=Vector2.new(2,2)
                image.Image=IMAGE_PATH
                """.Replace("IMAGE_PATH", System.Text.Json.JsonSerializer.Serialize(imagePath.Replace('\\', '/'))));
            var capture = Path.Combine(directory, "gui.bmp");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { Path.Combine(AppContext.BaseDirectory, "Woorf.Cli.dll"), directory, "--play", "--hidden", "--frames", "2", "--width", "160", "--height", "120", "--capture", capture })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(20000)) { process.Kill(true); throw new Exception("GUI viewer timed out."); }
            if (process.ExitCode != 0) throw new Exception(error.GetAwaiter().GetResult() + output.GetAwaiter().GetResult());
            var pixels = File.ReadAllBytes(capture);
            var center = 54 + (119 - 40) * 480 + 40 * 3;
            if (pixels[center + 2] < 245 || pixels[center] > 10 || pixels[center + 1] > 10)
                throw new Exception("Client-scripted GUI frame must render as a red overlay in screen pixels.");
            var white = 0;
            for (var y = 90; y < 115; y++)
                for (var x = 10; x < 150; x++)
                {
                    var index = 54 + (119 - y) * 480 + x * 3;
                    if (pixels[index] > 180 && pixels[index + 1] > 180 && pixels[index + 2] > 180) white++;
                }
            if (white < 40) throw new Exception("TextLabel must render legible glyph pixels.");
            var imagePixel = 54 + (119 - 40) * 480 + 115 * 3;
            if (pixels[imagePixel] < 240 || pixels[imagePixel + 2] > 10)
                throw new Exception("ImageRectOffset and ImageRectSize must crop the selected blue source rectangle.");
            CheckSceneDepth(directory);
            Console.WriteLine("PASS client GUI rectangles and text render over the real OpenGL scene");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL GUI graphics: " + error.Message); return 1; }
        finally { Directory.Delete(directory, true); }
    }

    private static void WriteImage(string path)
    {
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write((ushort)0x4D42); writer.Write(78); writer.Write(0); writer.Write(54); writer.Write(40);
        writer.Write(4); writer.Write(2); writer.Write((ushort)1); writer.Write((ushort)24); writer.Write(0); writer.Write(24);
        writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
        for (var y = 0; y < 2; y++)
            for (var x = 0; x < 4; x++)
            {
                writer.Write((byte)(x >= 2 ? 255 : 0)); writer.Write((byte)0); writer.Write((byte)(x < 2 ? 255 : 0));
            }
    }

    private static void CheckSceneDepth(string directory)
    {
        using var window = new OpenGlWindow("GUI depth regression", 160, 120, false);
        using var renderer = new OpenGlRenderer(window); using var gui = new GuiRenderer(window);
        var camera = new PerspectiveCamera { Position = new(0, 0, 5), Target = Vector3.Zero };
        RenderItem[] items = [new(MeshShape.Box, Matrix4x4.CreateTranslation(0, 0, 1), new(1, 0, 0)),
            new(MeshShape.Box, Matrix4x4.Identity, new(0, 0, 1))];
        renderer.Draw(items, camera);
        gui.Draw([new("Rectangle", new(0, 0, 10, 10), new(0, 0, 160, 120), Vector4.One)]);
        renderer.Draw(items, camera);
        var path = Path.Combine(directory, "depth.bmp"); renderer.Capture(path); var pixels = File.ReadAllBytes(path);
        var center = 54 + 60 * 480 + 80 * 3;
        if (pixels[center + 2] <= pixels[center]) throw new Exception("Drawing GUI must preserve depth testing for the next 3D frame.");
    }
}
