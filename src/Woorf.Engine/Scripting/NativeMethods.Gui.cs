using System.Runtime.InteropServices;

namespace Woorf.Engine.Scripting;

internal static partial class NativeMethods
{
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_gui_value(nint state, int kind, double a, double b, double c, double d);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int woorf_get_gui_value(nint state, int index, int kind, [Out] double[] values);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_wrap_input_object(nint state);
}
