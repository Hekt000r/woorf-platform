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
        if (!arguments.Contains("--render") && !arguments.Contains("--play")) return null;
        RenderOptions options;
        try { options = RenderOptions.Parse(arguments); }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        var project = Directory.Exists(options.Source) ? ProjectLoader.Load(options.Source) : null;
        var source = project is null ? File.ReadAllText(options.Source) : null;
        var game = project?.Game ?? new DataModel();
        var title = options.IsPlayable ? "Woorf | WASD walk · Space jump · arrows orbit · R respawn · Esc close"
            : "Woorf | WASD move · Q/E rise · arrows orbit · Esc close";
        using var window = new OpenGlWindow(title, options.Width, options.Height, options.IsVisible);
        using var renderer = new OpenGlRenderer(window);
        using var pacer = new FramePacer();
        Console.WriteLine("OpenGL " + renderer.DriverDescription);
        var clock = new MonotonicClock();
        using var session = new LuauSession(game, clock);
        var status = project is null ? session.Execute(source!, options.Source) : session.StartScripts(project.Scripts);
        if (status != 0) return session.Finish(status);
        var player = options.IsPlayable ? CreateLocalPlayer(game) : null;
        var viewer = player is null ? null : new CharacterViewer(player);
        var composer = player is null ? null : new CharacterFrameComposer(player);
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
            var elapsed = (float)Math.Clamp(now - previous, 0, 0.1);
            if (viewer is null) ViewerCamera.Update(camera, window, elapsed); else viewer.Update(camera, window, elapsed);
            previous = now;
            session.Step(now);
            var simulated = Stopwatch.GetTimestamp();
            if (session.HasFailed) return session.Finish(1);
            viewer?.Follow(camera, elapsed);
            scene.SetHiddenPart(viewer?.Root);
            var items = scene.Capture();
            if (composer is not null) items = composer.Capture(items, now);
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

    private static Player CreateLocalPlayer(DataModel game)
    {
        var players = (Players)game.GetService("Players");
        players.CharacterAutoLoads = true;
        var player = new Player(1, "Player1"); player.Parent = players;
        return player;
    }
}
