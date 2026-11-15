using System.Runtime.InteropServices;

namespace Woorf.Engine.Scripting;

internal static partial class NativeMethods
{
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
