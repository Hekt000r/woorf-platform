using System.Runtime.InteropServices;

namespace Woorf.Engine.Scripting;

internal static class NativeMethods
{
    private const string Library = "woorf_luau";
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void WriteOutput(nint bytes, nuint length, int isError);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int HostCall(nint state, [MarshalAs(UnmanagedType.LPUTF8Str)] string operation);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate double ReadClock();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint woorf_create(WriteOutput write, HostCall host, int gameId, int workspaceId);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_close(nint handle);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_run(nint handle, byte[] source, nuint length,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string chunkName, int scriptId);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_kind(nint state, int index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_get_id(nint state, int index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint woorf_get_string(nint state, int index, out nuint length);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_get_bool(nint state, int index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_string(nint state, byte[] bytes, nuint length);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_bool(nint state, int value);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_instance(nint state, int id);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_list(nint state, int[] ids, int count);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_method(nint state,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string operation);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_signal(nint state, int id);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_connection(nint state, int id);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_get_signal_id(nint state, int index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_get_connection_id(nint state, int index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_ref_function(nint state, int index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_unref_callback(nint handle, int reference);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_invoke(nint handle, int reference, int argumentCount,
        int firstInstanceId, int secondInstanceId, byte[]? stringArgument, nuint length);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_initialize_scheduler(nint handle, byte[] source, nuint length);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern double woorf_step(nint handle, double now);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_scheduler_failed(nint handle);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_set_clock(nint handle, ReadClock clock);
}
