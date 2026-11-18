using System.Runtime.InteropServices;

namespace Woorf.Engine.Scripting;

internal static partial class NativeMethods
{
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_font(nint state, byte[] family, nuint length, int weight, int style);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint woorf_get_font(nint state, int index, out nuint length, out int weight, out int style);
}
