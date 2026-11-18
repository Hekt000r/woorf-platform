using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed unsafe class NativeCallbackLease : IDisposable
{
    private sealed record Callbacks(WriteOutput Write, HostCall Host, ReadClock Clock);
    private GCHandle context;
    private readonly WriteOutput write;
    internal nint Handle { get; private set; }

    private NativeCallbackLease(WriteOutput write, HostCall host, ReadClock clock, int gameId, int workspaceId)
    {
        this.write = write;
        context = GCHandle.Alloc(new Callbacks(write, host, clock));
        try
        {
            Handle = CreateNative((nint)(delegate* unmanaged[Cdecl]<nint, nint, nuint, int, void>)&Write,
                (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, int>)&Dispatch,
                (nint)(delegate* unmanaged[Cdecl]<nint, double>)&ReadTime,
                GCHandle.ToIntPtr(context), gameId, workspaceId);
            if (Handle == 0) throw new InvalidOperationException("Could not create Luau state.");
        }
        catch { context.Free(); throw; }
    }

    internal static NativeCallbackLease Create(WriteOutput write, HostCall host, ReadClock clock, int gameId, int workspaceId)
        => new(write, host, clock, gameId, workspaceId);

    [DllImport("woorf_luau", EntryPoint = "woorf_create_browser", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint CreateNative(nint write, nint host, nint clock, nint context, int gameId, int workspaceId);

    [DllImport("woorf_luau", EntryPoint = "woorf_invoke_values_browser", CallingConvention = CallingConvention.Cdecl)]
    private static extern int InvokeNative(nint handle, int reference, nint push, nint context);

    internal static int InvokeValues(nint handle, int reference, PushArguments push)
    {
        var arguments = GCHandle.Alloc(push);
        try
        {
            return InvokeNative(handle, reference, (nint)(delegate* unmanaged[Cdecl]<nint, nint, int>)&Push,
                GCHandle.ToIntPtr(arguments));
        }
        finally { arguments.Free(); }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int Push(nint pointer, nint state) => ((PushArguments)GCHandle.FromIntPtr(pointer).Target!)(state);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Write(nint pointer, nint bytes, nuint length, int isError)
        => ((Callbacks)GCHandle.FromIntPtr(pointer).Target!).Write(bytes, length, isError);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int Dispatch(nint pointer, nint state, nint operation)
    {
        var callbacks = (Callbacks)GCHandle.FromIntPtr(pointer).Target!;
        return callbacks.Host(state, Marshal.PtrToStringUTF8(operation)!);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static double ReadTime(nint pointer) => ((Callbacks)GCHandle.FromIntPtr(pointer).Target!).Clock();

    public void Dispose()
    {
        if (Handle == 0) return;
        woorf_close(Handle); Handle = 0;
        context.Free(); GC.KeepAlive(write);
    }
}
