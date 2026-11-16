using System.Runtime.InteropServices;
using System.Text;
using Woorf.Engine.DataModel;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Networking;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed class LuauSession : IDisposable
{
    private readonly InstanceBindings bindings;
    private readonly WriteOutput write;
    private readonly HostCall host;
    private readonly ReadClock readClock;
    private readonly nint handle;
    private bool isDisposed;
    private readonly DataModel.DataModel game;
    private readonly IRuntimeClock clock;
    private double lastHeartbeat;
    private readonly ScriptLifecycle lifecycle;
    private bool hasLifecycleFailure;
    private bool hasFinishedFailure;

    internal LuauSession(DataModel.DataModel game, IRuntimeClock clock, RuntimeEndpoint? endpoint = null)
    {
        this.game = game; this.clock = clock; lastHeartbeat = clock.ElapsedSeconds;
        bindings = new InstanceBindings(game, endpoint);
        write = Write;
        host = bindings.Dispatch;
        readClock = () => clock.ElapsedSeconds;
        handle = woorf_create(write, host, bindings.GetId(game), bindings.GetId(game.Workspace));
        if (handle == 0) throw new InvalidOperationException("Could not create Luau state.");
        bindings.AttachRuntime(handle);
        woorf_set_clock(handle, readClock);
        try { InitializeScheduler(); }
        catch { bindings.Dispose(); woorf_close(handle); throw; }
        lifecycle = new ScriptLifecycle(game, this, endpoint?.IsClient == true);
    }

    private void InitializeScheduler()
    {
        using var stream = typeof(LuauSession).Assembly.GetManifestResourceStream("Woorf.Engine.Scripting.Scheduler.luau")
            ?? throw new InvalidOperationException("Scheduler resource is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var remoteStream = typeof(LuauSession).Assembly.GetManifestResourceStream("Woorf.Engine.Scripting.RemoteFunctions.luau")!;
        using var remoteReader = new StreamReader(remoteStream, Encoding.UTF8);
        using var backendStream = typeof(LuauSession).Assembly.GetManifestResourceStream("Woorf.Engine.Scripting.BackendTypes.luau")!;
        using var backendReader = new StreamReader(backendStream, Encoding.UTF8);
        using var lifecycleStream = typeof(LuauSession).Assembly.GetManifestResourceStream("Woorf.Engine.Scripting.ScriptLifecycle.luau")!;
        using var lifecycleReader = new StreamReader(lifecycleStream, Encoding.UTF8);
        var bytes = Encoding.UTF8.GetBytes(reader.ReadToEnd() + "\n" + lifecycleReader.ReadToEnd() + "\n" + remoteReader.ReadToEnd() + "\n" + backendReader.ReadToEnd());
        if (woorf_initialize_scheduler(handle, bytes, (nuint)bytes.Length) != 0)
            throw new InvalidOperationException("Could not initialize scheduler.");
    }

    internal int StartScripts(IEnumerable<Woorf.Engine.Projects.ProjectScript> scripts) => lifecycle.Start(scripts);
    internal int StartClientScripts() => lifecycle.Synchronize();
    internal void StopScript(Instance script) => woorf_stop_script(handle, bindings.GetId(script));

    internal int Drain(IRuntimeClock clock)
    {
        while (true)
        {
            var deadline = Step(clock.ElapsedSeconds);
            if (HasFailed) return 1;
            if (deadline < 0) return 0;
            var delay = deadline - clock.ElapsedSeconds;
            if (delay > 0) clock.Sleep(TimeSpan.FromSeconds(delay));
        }
    }

    internal void InitializeRemote(RemoteFunction remote)
    {
        if (woorf_initialize_remote(handle, bindings.GetId(remote)) != 0)
            throw new InvalidOperationException("Could not initialize RemoteFunction.");
    }

    internal double Step(double now)
    {
        if (lifecycle.Synchronize() != 0) { hasLifecycleFailure = true; return -1; }
        var run = (RunService)game.GetService("RunService");
        if (now - lastHeartbeat >= 1d / 60)
        {
            var elapsed = now - lastHeartbeat;
            lastHeartbeat = now;
            run.Heartbeat.Fire(elapsed);
        }
        var next = woorf_step(handle, now);
        if (lifecycle.HasPending) return now;
        return next < 0 && run.Heartbeat.HasConnections ? now + 1d / 60 : next;
    }
    internal bool HasFailed => isDisposed ? hasFinishedFailure : hasLifecycleFailure || woorf_scheduler_failed(handle) != 0;
    internal int Finish(int status) { Dispose(); return status != 0 || HasFailed ? 1 : 0; }

    internal int Execute(string source, string filePath, Instance? script = null)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        var bytes = Encoding.UTF8.GetBytes(source);
        return woorf_run(handle, bytes, (nuint)bytes.Length, "@" + filePath, bindings.GetId(script));
    }

    private static void Write(nint data, nuint length, int isError)
    {
        var text = Marshal.PtrToStringUTF8(data, checked((int)length));
        var writer = isError != 0 ? Console.Error : Console.Out;
        writer.Write(text);
    }

    public void Dispose()
    {
        if (isDisposed) return;
        if (bindings.HasShutdown && woorf_execution_failed(handle) == 0)
        {
            bindings.BeginShutdown();
            var deadline = clock.ElapsedSeconds + 30;
            while (woorf_execution_failed(handle) == 0 && woorf_shutdown_pending(handle) > 0 && clock.ElapsedSeconds < deadline)
            {
                var next = Step(clock.ElapsedSeconds);
                if (next < 0) break;
                var delay = Math.Min(next, deadline) - clock.ElapsedSeconds;
                if (delay > 0) clock.Sleep(TimeSpan.FromSeconds(delay));
            }
        }
        hasFinishedFailure = HasFailed;
        lifecycle.Dispose();
        bindings.Dispose();
        woorf_close(handle);
        GC.KeepAlive(write);
        GC.KeepAlive(host);
        GC.KeepAlive(readClock);
        isDisposed = true;
    }
}
