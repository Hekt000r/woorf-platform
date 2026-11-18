using System.Reflection;
using System.Text;
using Woorf.Engine;
using static Woorf.Engine.Scripting.NativeMethods;

internal static class BrowserCallbackTests
{
    internal static int Run()
    {
        try
        {
            var type = typeof(LuauRuntime).Assembly.GetType("Woorf.Engine.Scripting.NativeCallbackLease");
            if (type is null) throw new Exception("Contextual native callbacks are not implemented.");
            var create = type.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!;
            var output = new StringBuilder();
            WriteOutput write = (bytes, length, _) => output.Append(System.Runtime.InteropServices.Marshal.PtrToStringUTF8(bytes, (int)length));
            using var first = Create(create, write, "first", 7);
            using var second = Create(create, write, "second", 19);
            RunScript(type, first); RunScript(type, second); RunScript(type, first);
            if (output.ToString().Replace("\r\n", "\n") != "first\t7\nsecond\t19\nfirst\t7\n")
                throw new Exception("Native callbacks crossed VM contexts: " + output);
            Console.WriteLine("PASS browser-compatible native callbacks preserve separate VM hosts and clocks");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL browser-compatible native callbacks: " + error.GetBaseException().Message);
            return 1;
        }
    }

    private static IDisposable Create(MethodInfo create, WriteOutput write, string name, double time)
    {
        HostCall host = (state, _) => { var bytes = Encoding.UTF8.GetBytes(name); woorf_push_string(state, bytes, (nuint)bytes.Length); return 1; };
        ReadClock clock = () => time;
        var lease = (IDisposable)create.Invoke(null, [write, host, clock, 1, 2])!;
        try
        {
            var handle = GetHandle(lease.GetType(), lease);
            var scheduler = Encoding.UTF8.GetBytes("function _woorf_start(fn) fn() end");
            if (woorf_initialize_scheduler(handle, scheduler, (nuint)scheduler.Length) != 0) throw new Exception("Scheduler failed.");
            return lease;
        }
        catch { lease.Dispose(); throw; }
    }

    private static void RunScript(Type type, IDisposable lease)
    {
        var handle = GetHandle(type, lease);
        var source = Encoding.UTF8.GetBytes("print(game.Name, _woorf_now())");
        if (woorf_run(handle, source, (nuint)source.Length, "browser-callback-test", 0) != 0) throw new Exception("Native script failed.");
    }

    private static nint GetHandle(Type type, IDisposable lease) =>
        (nint)type.GetProperty("Handle", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(lease)!;
}
