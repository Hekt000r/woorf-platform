using System.Runtime.InteropServices;
using System.Text;
using Woorf.Engine.Datatypes;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal static class Vector3Bindings
{
    internal static int Dispatch(nint state, string operation)
    {
        if (operation == "vector3_new")
            return Push(state, new Vector3(ReadOptionalComponent(state, 1), ReadOptionalComponent(state, 2), ReadOptionalComponent(state, 3)));
        var value = Read(state, 1);
        return operation switch
        {
            "vector3_get" => ReadProperty(state, value),
            "vector3_tostring" => PushString(state, value.ToString()),
            "vector3_Dot" => PushNumber(state, value.Dot(Read(state, 2))),
            "vector3_Cross" => Push(state, value.Cross(Read(state, 2))),
            "vector3_Lerp" => Push(state, value.Lerp(Read(state, 2), (float)ReadNumber(state, 3))),
            _ => throw new InvalidOperationException("Vector3 members are read-only."),
        };
    }

    internal static Vector3 Read(nint state, int index)
    {
        if (woorf_get_vector3(state, index, out var x, out var y, out var z) == 0)
            throw new InvalidOperationException("Expected Vector3 argument.");
        return new Vector3(x, y, z);
    }

    internal static int Push(nint state, Vector3 value)
    {
        woorf_push_vector3(state, value.X, value.Y, value.Z);
        return 1;
    }

    private static float ReadOptionalComponent(nint state, int index) =>
        woorf_kind(state, index) == 0 ? 0 : (float)ReadNumber(state, index);

    private static double ReadNumber(nint state, int index)
    {
        if (woorf_get_number(state, index, out var value) == 0)
            throw new InvalidOperationException("Expected number argument.");
        return value;
    }

    private static int ReadProperty(nint state, Vector3 value)
    {
        var data = woorf_get_string(state, 2, out var length);
        var member = Marshal.PtrToStringUTF8(data, checked((int)length));
        if (member is "Dot" or "Cross" or "Lerp")
        {
            woorf_push_method(state, "vector3_" + member);
            return 1;
        }
        return member switch
        {
            "X" => PushNumber(state, value.X),
            "Y" => PushNumber(state, value.Y),
            "Z" => PushNumber(state, value.Z),
            "Magnitude" => PushNumber(state, value.Magnitude),
            "Unit" => Push(state, value.Unit),
            _ => throw new InvalidOperationException($"{member} is not a valid member of Vector3."),
        };
    }

    private static int PushNumber(nint state, double value) { woorf_push_number(state, value); return 1; }
    private static int PushString(nint state, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        woorf_push_string(state, bytes, (nuint)bytes.Length);
        return 1;
    }
}
