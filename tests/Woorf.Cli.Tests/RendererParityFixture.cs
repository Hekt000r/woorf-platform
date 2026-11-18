using System.Numerics;
using System.Text.Json;
using Woorf.Engine.DataModel;
using Woorf.Graphics;
using Woorf.Graphics.Platform;

internal static class RendererParityFixture
{
    internal static int Export(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            using var window = new OpenGlWindow("Renderer parity", 160, 120, false);
            using var renderer = new OpenGlRenderer(window);
            var encoder = new ScenePacketEncoder();
            var frames = new List<object>();
            foreach (var (name, camera, items, sky) in CreateCases())
            {
                renderer.Draw(items, camera, sky);
                var capture = Path.Combine(directory, name + ".bmp"); renderer.Capture(capture);
                var reference = ReadPixels(capture);
                frames.Add(new { name, packet = encoder.Encode(new(items, sky), camera, 4f/3), reference });
            }
            File.WriteAllText(Path.Combine(directory,"fixtures.json"), JsonSerializer.Serialize(new
            {
                width = 160, height = 120, frames, shaders = ShaderProfile.WebGlPrograms(),
                meshes = new[] { MeshData.CreateBox(), MeshData.CreateSphere() },
            }));
            Console.WriteLine($"PASS exported {frames.Count} native GPU references for WebGL comparison");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL renderer parity export: " + error); return 1; }
    }

    private static IEnumerable<(string, PerspectiveCamera, RenderItem[], Skybox?)> CreateCases()
    {
        var sky = (Sky)Instance.Create("Sky");
        var textures = Sky.FaceProperties.Select(sky.GetTexture).ToArray();
        var background = new Skybox(textures, Vector3.Zero);
        foreach (var (name, target) in new (string, Vector3)[]
        {
            ("right",Vector3.UnitX), ("left",-Vector3.UnitX), ("up",Vector3.UnitY),
            ("down",-Vector3.UnitY), ("back",Vector3.UnitZ), ("front",-Vector3.UnitZ),
        }) yield return (name, new() { Position = Vector3.Zero, Target = target }, [], background);
        yield return ("translated", new() { Position = new(40,20,30), Target = new(40,20,29) }, [], background);
        yield return ("oriented", new() { Position = Vector3.Zero, Target = -Vector3.UnitZ }, [], new(textures,new(10,90,20)));
        RenderItem[] shapes = [new(MeshShape.Box, Matrix4x4.CreateScale(2,3,1) * Matrix4x4.CreateRotationY(.6f), new(.15f,.6f,.8f)),
            new(MeshShape.Sphere, Matrix4x4.CreateTranslation(3,0,0), new(.8f,.3f,.1f))];
        yield return ("lighting",new() { Position = new(6,5,10), Target = Vector3.Zero },shapes,background);
        yield return ("no-sky",new() { Position = new(6,5,10), Target = Vector3.Zero },shapes,null);
        var root = (Part)Instance.Create("Part"); root.Position = new(0,4,0);
        var humanoid = (Humanoid)Instance.Create("Humanoid"); var view = new CharacterView();
        foreach (var (name, direction) in new (string, Woorf.Engine.Datatypes.Vector3)[]
            { ("character-forward",new(0,0,-1)), ("character-right",new(1,0,0)), ("character-back",new(0,0,1)) })
        {
            humanoid.Move(direction);
            yield return (name,new() { Position = new(0,7,12), Target = new(0,4,0) },view.Capture(root,humanoid,0).ToArray(),background);
        }
    }

    private static byte[] ReadPixels(string path)
    {
        var bmp = File.ReadAllBytes(path); var pixels = new byte[160*120*4];
        for (var pixel = 0; pixel < 160*120; pixel++)
        {
            pixels[pixel*4] = bmp[54+pixel*3+2]; pixels[pixel*4+1] = bmp[54+pixel*3+1];
            pixels[pixel*4+2] = bmp[54+pixel*3]; pixels[pixel*4+3] = 255;
        }
        return pixels;
    }
}
