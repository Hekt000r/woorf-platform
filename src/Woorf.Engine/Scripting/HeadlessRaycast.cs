using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Scripting;

internal static class HeadlessRaycast
{
    internal static object? Cast(Instance workspace, object? originValue, object? directionValue)
    {
        if (originValue is not Vector3 origin || directionValue is not Vector3 direction)
            throw new InvalidOperationException("Raycast expects Vector3 origin and direction.");
        if (!float.IsFinite(direction.Magnitude) || !float.IsFinite(origin.Magnitude))
            throw new InvalidOperationException("Raycast expects finite vectors.");
        if (direction.Magnitude == 0) return null;
        BasePart? closest = null;
        var fraction = float.PositiveInfinity;
        var normal = new Vector3();
        foreach (var part in workspace.GetDescendants().OfType<BasePart>())
        {
            var inverse = part.CFrame.Inverse();
            if (!Intersect(inverse.TransformPoint(origin), inverse.TransformVector(direction), part.Size / 2, out var hit, out var localNormal) || hit >= fraction) continue;
            fraction = hit; closest = part; normal = part.CFrame.TransformVector(localNormal);
        }
        if (closest is null) return null;
        return new LuaTableValue(new KeyValuePair<object, object?>[]
        {
            new("Instance", closest), new("Position", origin + direction * fraction),
            new("Normal", normal), new("Distance", (double)(direction.Magnitude * fraction)),
        });
    }

    private static bool Intersect(Vector3 origin, Vector3 direction, Vector3 half, out float hit, out Vector3 normal)
    {
        hit = 0; normal = new Vector3();
        var exit = 1f;
        var coordinates = new[] { (origin.X, direction.X, half.X, new Vector3(1,0,0)),
            (origin.Y, direction.Y, half.Y, new Vector3(0,1,0)), (origin.Z, direction.Z, half.Z, new Vector3(0,0,1)) };
        foreach (var (position, delta, bound, axis) in coordinates)
        {
            if (MathF.Abs(delta) < 1e-8f) { if (position < -bound || position > bound) return false; continue; }
            var near = (-bound - position) / delta;
            var far = (bound - position) / delta;
            var surface = axis * -1;
            if (near > far) { (near, far) = (far, near); surface = axis; }
            if (near > hit) { hit = near; normal = surface; }
            exit = MathF.Min(exit, far);
            if (hit > exit) return false;
        }
        return hit >= 0 && hit <= 1;
    }
}
