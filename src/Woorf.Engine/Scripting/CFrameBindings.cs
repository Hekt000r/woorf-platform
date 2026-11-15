using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Woorf.Engine.Datatypes;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal static class CFrameBindings
{
    private static readonly HashSet<string> Methods =
    [
        "Inverse", "GetComponents", "ToWorldSpace", "ToObjectSpace", "PointToWorldSpace",
        "PointToObjectSpace", "VectorToWorldSpace", "VectorToObjectSpace", "Orthonormalize",
    ];

    internal static int Dispatch(nint state, string operation)
    {
        if (CFrameConstructionBindings.IsConstructor(operation))
            return Push(state, CFrameConstructionBindings.Construct(state, operation));
        var value = Read(state, 1);
        return operation switch
        {
            "cframe_get" => ReadProperty(state, value),
            "cframe_set" => throw new InvalidOperationException("CFrame members are read-only."),
            "cframe_tostring" => PushString(state, string.Join(", ", value.GetComponents()
                .Select(component => component.ToString("G", CultureInfo.InvariantCulture)))),
            "cframe_eq" => PushBoolean(state, value == Read(state, 2)),
            "cframe_mul" => Multiply(state, value),
            "cframe_add" => Push(state, value + Vector3Bindings.Read(state, 2)),
            "cframe_sub" => Push(state, value - Vector3Bindings.Read(state, 2)),
            "cframe_Inverse" => Push(state, value.Inverse()),
            "cframe_Orthonormalize" => Push(state, value.Orthonormalize()),
            "cframe_GetComponents" => PushComponents(state, value),
            _ => TransformArguments(state, value, operation),
        };
    }

    internal static CFrame Read(nint state, int index)
    {
        var components = new float[12];
        if (woorf_get_cframe(state, index, components) == 0)
            throw new InvalidOperationException("Expected CFrame argument.");
        return CFrame.FromComponents(components);
    }

    internal static int Push(nint state, CFrame value)
    {
        woorf_push_cframe(state, value.GetComponents());
        return 1;
    }

    internal static float ReadNumber(nint state, int index)
    {
        if (woorf_get_number(state, index, out var value) == 0)
            throw new InvalidOperationException("Expected number argument.");
        return (float)value;
    }

    private static int Multiply(nint state, CFrame value)
    {
        var components = new float[12];
        if (woorf_get_cframe(state, 2, components) != 0)
            return Push(state, value * CFrame.FromComponents(components));
        return Vector3Bindings.Push(state, value.TransformPoint(Vector3Bindings.Read(state, 2)));
    }

    private static int ReadProperty(nint state, CFrame value)
    {
        var pointer = woorf_get_string(state, 2, out var length);
        var member = Marshal.PtrToStringUTF8(pointer, checked((int)length));
        if (member is not null && Methods.Contains(member))
        {
            woorf_push_method(state, "cframe_" + member);
            return 1;
        }
        return member switch
        {
            "Position" => Vector3Bindings.Push(state, value.Position),
            "Rotation" => Push(state, value.Rotation),
            "X" => PushNumber(state, value.Position.X),
            "Y" => PushNumber(state, value.Position.Y),
            "Z" => PushNumber(state, value.Position.Z),
            "RightVector" or "XVector" => Vector3Bindings.Push(state, value.RightVector),
            "UpVector" or "YVector" => Vector3Bindings.Push(state, value.UpVector),
            "ZVector" => Vector3Bindings.Push(state, value.ZVector),
            "LookVector" => Vector3Bindings.Push(state, value.LookVector),
            _ => throw new InvalidOperationException($"{member} is not a valid member of CFrame."),
        };
    }

    private static int TransformArguments(nint state, CFrame value, string operation)
    {
        var count = woorf_get_top(state) - 1;
        var isObjectSpace = operation is "cframe_ToObjectSpace" or "cframe_PointToObjectSpace" or "cframe_VectorToObjectSpace";
        var transform = isObjectSpace ? value.Inverse() : value;
        if (count == 0 && operation == "cframe_PointToWorldSpace")
            return Vector3Bindings.Push(state, value.Position);
        for (var index = 2; index <= count + 1; index++)
        {
            switch (operation)
            {
                case "cframe_ToWorldSpace":
                case "cframe_ToObjectSpace": Push(state, transform * Read(state, index)); break;
                case "cframe_PointToWorldSpace":
                case "cframe_PointToObjectSpace":
                    Vector3Bindings.Push(state, transform.TransformPoint(Vector3Bindings.Read(state, index))); break;
                case "cframe_VectorToWorldSpace":
                case "cframe_VectorToObjectSpace":
                    Vector3Bindings.Push(state, transform.TransformVector(Vector3Bindings.Read(state, index))); break;
                default: throw new InvalidOperationException($"Unsupported CFrame operation '{operation}'.");
            }
        }
        return count;
    }

    private static int PushComponents(nint state, CFrame value)
    {
        foreach (var component in value.GetComponents()) woorf_push_number(state, component);
        return 12;
    }

    private static int PushNumber(nint state, float value) { woorf_push_number(state, value); return 1; }
    private static int PushBoolean(nint state, bool value) { woorf_push_bool(state, value ? 1 : 0); return 1; }
    private static int PushString(nint state, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        woorf_push_string(state, bytes, (nuint)bytes.Length);
        return 1;
    }
}
