using System.Runtime.InteropServices;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Scripting;

internal static partial class NativeMethods
{
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void woorf_push_sequence(nint state, int kind, float[] components, int count);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint woorf_get_sequence(nint state, int index, int kind, out int count);

    internal static SequenceValue ReadSequence(nint state, int index, int kind)
    {
        var data = woorf_get_sequence(state, index, kind, out var count);
        if (data == 0) throw new InvalidOperationException("Expected sequence.");
        var components = new float[count];
        Marshal.Copy(data, components, 0, count);
        return new(kind, components);
    }
}
