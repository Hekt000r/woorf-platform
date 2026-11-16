using System.Runtime.InteropServices;

namespace Woorf.Engine.Scripting;

internal static partial class NativeMethods
{
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_physical_properties(nint state, [In, MarshalAs(UnmanagedType.LPArray, SizeConst = 5)] float[] values);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_get_physical_properties(nint state, int index, [Out, MarshalAs(UnmanagedType.LPArray, SizeConst = 5)] float[] values);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_transform(nint state, int callbackIndex);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_initialize_remote(nint handle, int id);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_get_color3(nint state, int index, out float red, out float green, out float blue);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_color3(nint state, float red, float green, float blue);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_ref_shutdown(nint state, int index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_shutdown_pending(nint handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int PushArguments(nint state);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_value_kind(nint state, int index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint woorf_table_identity(nint state, int index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_nil(nint state);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_set_top(nint state, int top);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_table_next(nint state, int index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_new_table(nint state);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_raw_set(nint state, int index);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_invoke_values(nint handle, int reference, PushArguments push);
}
