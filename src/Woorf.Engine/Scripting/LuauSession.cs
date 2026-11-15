using System.Runtime.InteropServices;
using System.Text;
using Woorf.Engine.DataModel;
using Woorf.Engine.Scheduling;
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

    internal LuauSession(DataModel.DataModel game, IRuntimeClock clock)
    {
        bindings = new InstanceBindings(game);
        write = Write;
        host = bindings.Dispatch;
        readClock = () => clock.ElapsedSeconds;
        handle = woorf_create(write, host, bindings.GetId(game), bindings.GetId(game.Workspace));
        if (handle == 0) throw new InvalidOperationException("Could not create Luau state.");
        bindings.AttachRuntime(handle);
        woorf_set_clock(handle, readClock);
        try { InitializeScheduler(); }
        catch { Dispose(); throw; }
    }

    private void InitializeScheduler()
    {
        using var stream = typeof(LuauSession).Assembly.GetManifestResourceStream("Woorf.Engine.Scripting.Scheduler.luau")
            ?? throw new InvalidOperationException("Scheduler resource is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var bytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());
        if (woorf_initialize_scheduler(handle, bytes, (nuint)bytes.Length) != 0)
            throw new InvalidOperationException("Could not initialize scheduler.");
    }

    internal int Drain(IRuntimeClock clock)
    {
        while (true)
        {
            var deadline = woorf_step(handle, clock.ElapsedSeconds);
            if (woorf_scheduler_failed(handle) != 0) return 1;
            if (deadline < 0) return 0;
            var delay = deadline - clock.ElapsedSeconds;
            if (delay > 0) clock.Sleep(TimeSpan.FromSeconds(delay));
        }
    }

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
        bindings.Dispose();
        woorf_close(handle);
        GC.KeepAlive(write);
        GC.KeepAlive(host);
        GC.KeepAlive(readClock);
        isDisposed = true;
    }
}
