using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Physics;

internal static class ShapeQueries
{
    internal static Vector3 ClosestPoint(Vector3 point, Vector3 half) => new(Math.Clamp(point.X, -half.X, half.X),
        Math.Clamp(point.Y, -half.Y, half.Y), Math.Clamp(point.Z, -half.Z, half.Z));

    internal static bool SweepBox(Vector3 origin, Vector3 direction, Vector3 half, float radius, out float fraction, out Vector3 normal)
    {
        fraction = float.PositiveInfinity; normal = default;
        if ((origin - ClosestPoint(origin, half)).Magnitude <= radius) return false;
        var start = new[] { origin.X, origin.Y, origin.Z };
        var delta = new[] { direction.X, direction.Y, direction.Z };
        var bounds = new[] { half.X, half.Y, half.Z };
        for (var axis = 0; axis < 3; axis++)
        {
            var u = (axis + 1) % 3; var v = (axis + 2) % 3;
            foreach (var sign in new[] { -1, 1 })
            {
                if (MathF.Abs(delta[axis]) < 1e-12f) continue;
                var time = (sign * (bounds[axis] + radius) - start[axis]) / delta[axis];
                if (time < 0 || time > 1 || MathF.Abs(start[u] + delta[u] * time) > bounds[u]
                    || MathF.Abs(start[v] + delta[v] * time) > bounds[v]) continue;
                fraction = MathF.Min(fraction, time);
            }
            foreach (var first in new[] { -1, 1 })
                foreach (var second in new[] { -1, 1 })
                    SweepEdge(start, delta, bounds, axis, first, second, radius, ref fraction);
        }
        foreach (var x in new[] { -half.X, half.X })
            foreach (var y in new[] { -half.Y, half.Y })
                foreach (var z in new[] { -half.Z, half.Z })
                    if (RaySphere(origin - new Vector3(x,y,z), direction, radius, out var time, out _)) fraction = MathF.Min(fraction, time);
        if (!float.IsFinite(fraction)) return false;
        var center = origin + direction * fraction;
        normal = (center - ClosestPoint(center, half)).Unit;
        return true;
    }
    private static void SweepEdge(float[] start, float[] delta, float[] half, int axis, int first, int second, float radius, ref float fraction)
    {
        var u = (axis + 1) % 3; var v = (axis + 2) % 3;
        var origin = new Vector3(start[u] - first * half[u], start[v] - second * half[v], 0);
        var direction = new Vector3(delta[u], delta[v], 0);
        if (!RaySphere(origin, direction, radius, out var time, out _) || time >= fraction) return;
        if (MathF.Abs(start[axis] + delta[axis] * time) <= half[axis]) fraction = time;
    }
    internal static bool RaySphere(Vector3 origin, Vector3 direction, float radius, out float fraction, out Vector3 normal)
    {
        fraction = 0; normal = default;
        var offset = origin.Dot(origin) - radius * radius;
        if (offset <= 0) return true;
        var lengthSquared = direction.Dot(direction);
        var projection = origin.Dot(direction);
        var discriminant = projection * projection - lengthSquared * offset;
        if (lengthSquared <= 0 || projection >= 0 || discriminant < 0) return false;
        fraction = (-projection - MathF.Sqrt(discriminant)) / lengthSquared;
        normal = (origin + direction * fraction).Unit;
        return fraction >= 0 && fraction <= 1;
    }
}
