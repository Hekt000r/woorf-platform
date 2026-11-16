using System.Diagnostics;
using Woorf.Engine.DataModel;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Scripting;
using Woorf.Graphics;
using Woorf.Graphics.Platform;

internal static class RenderCli
{
    internal static int? Run(string[] arguments)
    {
        if (!arguments.Contains("--render")) return null;
        RenderOptions options;
        try { options = RenderOptions.Parse(arguments); }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        var project = Directory.Exists(options.Source) ? ProjectLoader.Load(options.Source) : null;
        var source = project is null ? File.ReadAllText(options.Source) : null;
        var game = project?.Game ?? new DataModel();
        using var window = new OpenGlWindow("Woorf | WASD move · Q/E rise · arrows orbit · Esc close", options.Width, options.Height, options.IsVisible);
        using var renderer = new OpenGlRenderer(window);
        using var pacer = new FramePacer();
        Console.WriteLine("OpenGL " + renderer.DriverDescription);
        var clock = new MonotonicClock();
        using var session = new LuauSession(game, clock);
        var status = project is null ? session.Execute(source!, options.Source) : session.StartScripts(project.Scripts);
        if (status != 0) return session.Finish(status);
        var camera = new PerspectiveCamera();
        using var scene = new WorkspaceScene(game.Workspace);
        using var profile = options.Profile is null ? null : new FrameProfile(options.Profile);
        var previous = clock.ElapsedSeconds;
        var frame = 0;
        while (window.PollEvents())
        {
            var started = Stopwatch.GetTimestamp();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var now = clock.ElapsedSeconds;
            ViewerCamera.Update(camera, window, (float)Math.Clamp(now - previous, 0, 0.1));
            previous = now;
            session.Step(now);
            var simulated = Stopwatch.GetTimestamp();
            if (session.HasFailed) return session.Finish(1);
            var items = scene.Capture();
            var collected = Stopwatch.GetTimestamp();
            renderer.Draw(items, camera);
            var rendered = Stopwatch.GetTimestamp();
            frame++;
            if (options.Frames > 0 && frame >= options.Frames)
            {
                if (options.Capture is not null) renderer.Capture(options.Capture);
                window.Present();
                profile?.Record(started, simulated, collected, rendered, Stopwatch.GetTimestamp(), allocated, items.Length, renderer.LastFrameDrawCalls);
                break;
            }
            window.Present();
            var presented = Stopwatch.GetTimestamp();
            pacer.WaitUntil(started + Stopwatch.Frequency / 60);
            profile?.Record(started, simulated, collected, rendered, presented, allocated, items.Length, renderer.LastFrameDrawCalls);
        }
        return session.Finish(0);
    }
}
