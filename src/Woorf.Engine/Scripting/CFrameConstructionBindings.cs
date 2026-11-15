using Woorf.Engine.Datatypes;
using static Woorf.Engine.Scripting.NativeMethods;
using static Woorf.Engine.Scripting.CFrameBindings;

namespace Woorf.Engine.Scripting;

internal static class CFrameConstructionBindings
{
    internal static bool IsConstructor(string operation) => operation is "cframe_new" or "cframe_Angles"
        or "cframe_fromEulerAnglesXYZ" or "cframe_fromEulerAnglesYXZ" or "cframe_fromOrientation"
        or "cframe_fromAxisAngle" or "cframe_lookAt" or "cframe_lookAlong" or "cframe_fromMatrix";

    internal static CFrame Construct(nint state, string operation) => operation switch
    {
        "cframe_new" => ConstructNew(state),
        "cframe_Angles" or "cframe_fromEulerAnglesXYZ" =>
            CFrame.FromEulerAnglesXYZ(ReadNumber(state, 1), ReadNumber(state, 2), ReadNumber(state, 3)),
        "cframe_fromEulerAnglesYXZ" or "cframe_fromOrientation" =>
            CFrame.FromEulerAnglesYXZ(ReadNumber(state, 1), ReadNumber(state, 2), ReadNumber(state, 3)),
        "cframe_fromAxisAngle" => CFrame.FromAxisAngle(Vector3Bindings.Read(state, 1), ReadNumber(state, 2)),
        "cframe_lookAt" => ConstructLook(state, true),
        "cframe_lookAlong" => ConstructLook(state, false),
        "cframe_fromMatrix" => CFrame.FromMatrix(Vector3Bindings.Read(state, 1), Vector3Bindings.Read(state, 2),
            Vector3Bindings.Read(state, 3), woorf_kind(state, 4) == 0 ? null : Vector3Bindings.Read(state, 4)),
        _ => throw new InvalidOperationException($"Unsupported CFrame constructor '{operation}'."),
    };

    private static CFrame ConstructNew(nint state)
    {
        var count = woorf_get_top(state);
        if (count == 0) return CFrame.Identity;
        if (woorf_get_vector3(state, 1, out var x, out var y, out var z) != 0)
        {
            var position = new Vector3(x, y, z);
            return count switch
            {
                1 => CFrame.Identity.WithPosition(position),
                2 => CFrame.LookAlong(position, Vector3Bindings.Read(state, 2) - position, new Vector3(0, 1, 0)),
                _ => throw new InvalidOperationException("Unsupported CFrame.new arguments."),
            };
        }
        if (count == 3)
            return CFrame.Identity.WithPosition(new Vector3(ReadNumber(state, 1), ReadNumber(state, 2), ReadNumber(state, 3)));
        if (count == 7)
            return CFrame.FromQuaternion(new Vector3(ReadNumber(state, 1), ReadNumber(state, 2), ReadNumber(state, 3)),
                ReadNumber(state, 4), ReadNumber(state, 5), ReadNumber(state, 6), ReadNumber(state, 7));
        if (count == 12)
            return CFrame.FromComponents(Enumerable.Range(1, 12).Select(index => ReadNumber(state, index)).ToArray());
        throw new InvalidOperationException("Unsupported CFrame.new arguments.");
    }

    private static CFrame ConstructLook(nint state, bool isTarget)
    {
        var position = Vector3Bindings.Read(state, 1);
        var direction = Vector3Bindings.Read(state, 2);
        if (isTarget) direction -= position;
        var up = woorf_kind(state, 3) == 0 ? new Vector3(0, 1, 0) : Vector3Bindings.Read(state, 3);
        return CFrame.LookAlong(position, direction, up);
    }
}
