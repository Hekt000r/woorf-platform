using System.Diagnostics;
using Woorf.Engine.Scheduling;
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
        if (options.Clients > 1) return MultiplayerLauncher.Run(options);
        var title = options.IsPlayable ? "Woorf | WASD walk · Space jump · right-drag look · scroll zoom · R respawn · Esc menu"
            : "Woorf | WASD move · Q/E rise · arrows orbit · Esc close";
        using var window = new OpenGlWindow(title, options.Width, options.Height, options.IsVisible);
        if (options.WindowSlot >= 0) window.SetPosition(options.WindowSlot % 2 * (options.Width + 16), 40 + options.WindowSlot / 2 * (options.Height + 40));
        using var renderer = new OpenGlRenderer(window);
        Console.WriteLine("OpenGL " + renderer.DriverDescription);
        var clock = new MonotonicClock();
        using var runtime = new RenderRuntime(options, clock);
        if (runtime.StartupStatus != 0) return runtime.Finish(runtime.StartupStatus);
        var status = DrawFrames(options, window, renderer, runtime, clock);
        if (options.WindowSlot >= 0 && options.Frames > 0) WaitForLauncher(runtime, clock);
        return runtime.Finish(status);
    }

    private static void WaitForLauncher(RenderRuntime runtime, MonotonicClock clock)
    {
        Console.WriteLine("ViewerCompleted");
        var released = Console.In.ReadLineAsync();
        var deadline = clock.ElapsedSeconds + 30;
        while (!released.IsCompleted)
        {
            if (clock.ElapsedSeconds >= deadline) throw new IOException("Multiplayer launcher did not release the completed viewer.");
            runtime.Step(clock.ElapsedSeconds); Thread.Sleep(5);
        }
    }

    private static int DrawFrames(RenderOptions options, OpenGlWindow window, OpenGlRenderer renderer, RenderRuntime runtime, MonotonicClock clock)
    {
        using var pacer = new FramePacer();
        var viewer = runtime.CreateViewer();
        using var gui = new GuiViewer(window, runtime.Player,runtime.Game);
        var camera = new PerspectiveCamera();
        using var world = new WorldView(runtime.Game, runtime.Player);
        using var profile = options.Profile is null ? null : new FrameProfile(options.Profile);
        var previous = clock.ElapsedSeconds;
        var frame = 0;
        while (window.PollEvents(viewer is null))
        {
            var started = Stopwatch.GetTimestamp();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var now = clock.ElapsedSeconds;
            var elapsed = (float)Math.Clamp(now - previous, 0, 0.1);
            var wheel = gui.UpdateInput();
            if (viewer is null) ViewerCamera.Update(camera, window, elapsed); else viewer.Update(camera, window, elapsed, gui.IsTextFocused, wheel);
            var frameTime = now - previous;
            previous = now;
            runtime.Step(now);
            var simulated = Stopwatch.GetTimestamp();
            if (runtime.HasFailed) return 1;
            viewer?.Follow(camera, elapsed);
            var captured = world.Capture(now, viewer?.IsFirstPerson == true);
            var items = captured.Items;
            var collected = Stopwatch.GetTimestamp();
            renderer.Draw(items, camera, captured.Sky);
            gui.Draw(frameTime, options.ConnectAddress is null ? "Local" : "TCP");
            var rendered = Stopwatch.GetTimestamp();
            frame++;
            var shouldFinish = options.Frames > 0 && frame >= options.Frames;
            if (shouldFinish && options.Capture is not null) renderer.Capture(options.Capture);
            window.Present();
            var presented = Stopwatch.GetTimestamp();
            if (!shouldFinish) pacer.WaitUntil(started + Stopwatch.Frequency / 60);
            profile?.Record(started, simulated, collected, rendered, presented, allocated, items.Length, renderer.LastFrameDrawCalls);
            if (shouldFinish) break;
        }
        return 0;
    }
}
