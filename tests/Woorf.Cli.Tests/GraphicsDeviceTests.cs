using System.Numerics;
using Woorf.Graphics;
using Woorf.Graphics.Platform;

static class GraphicsDeviceTests
{
    internal static int Run()
    {
        try
        {
            using var window = new OpenGlWindow("Woorf graphics checks", 161, 121, false);
            using var renderer = new OpenGlRenderer(window);
            var camera = new PerspectiveCamera { Position = new(0, 0, 10), Target = Vector3.Zero };
            var invalid = new RenderItem(MeshShape.Box, Matrix4x4.CreateScale(0), Vector3.One);
            var wasRejected = false;
            try { renderer.Draw([invalid], camera); }
            catch (ArgumentException) { wasRejected = true; }
            if (!wasRejected) throw new Exception("Singular draw transforms must be rejected before reaching the shader.");
            var near = new RenderItem(MeshShape.Sphere, Matrix4x4.CreateScale(2), new(1, 0, 0));
            var far = new RenderItem(MeshShape.Box, Matrix4x4.CreateScale(4) * Matrix4x4.CreateTranslation(0, 0, -3), new(0, 0, 1));
            var first = Capture(renderer, [near, far], camera);
            var second = Capture(renderer, [far, near], camera);
            if (!first.SequenceEqual(second)) throw new Exception("Depth occlusion depends on submission order.");
            var offset = 54 + (60 * 484) + 80 * 3;
            if (first[offset + 2] <= first[offset] * 2) throw new Exception("The near red sphere did not occlude the far blue box.");
            renderer.Draw(Enumerable.Range(0, 200).Select(index => index % 2 == 0 ? near : far).ToArray(), camera);
            if (renderer.LastFrameDrawCalls != 2)
                throw new Exception("The central renderer must submit 200 boxes/spheres in two instanced draws.");
            camera.Position = new(0, 0, -10);
            var reversed = Capture(renderer, [near, far], camera);
            if (reversed[offset] <= reversed[offset + 2] * 2)
                throw new Exception("Moving the camera behind the scene must reveal the blue box in front of the sphere.");
            using var pacer = new FramePacer();
            var timer = System.Diagnostics.Stopwatch.StartNew();
            for (var index = 0; index < 10; index++)
                pacer.WaitUntil(System.Diagnostics.Stopwatch.GetTimestamp() + System.Diagnostics.Stopwatch.Frequency / 200);
            if (timer.Elapsed.TotalMilliseconds < 45 || timer.Elapsed.TotalMilliseconds > 140)
                throw new Exception("Ten 5 ms frame waits must avoid the coarse Windows sleep quantum.");
            Console.WriteLine("PASS OpenGL validates draw transforms and depth-tests indexed sphere/box meshes independent of order");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL OpenGL device: " + error.Message); return 1; }
    }

    private static byte[] Capture(OpenGlRenderer renderer, RenderItem[] items, PerspectiveCamera camera)
    {
        var path = Path.Combine(Path.GetTempPath(), "woorf-frame-" + Guid.NewGuid().ToString("N") + ".bmp");
        try { renderer.Draw(items, camera); renderer.Capture(path); return File.ReadAllBytes(path); }
        finally { File.Delete(path); }
    }
}
