using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Physics;

internal static class BoxCollision
{
    internal const float Skin = 0.004f;
    internal static bool FindAxis(RigidBody a, RigidBody b, out Vector3 normal, out float depth, out int feature)
    {
        normal = default; depth = float.PositiveInfinity; feature = -1;
        var delta = b.Frame.Position - a.Frame.Position;
        for (var index = 0; index < 15; index++)
        {
            var axis = index < 3 ? a.Axis(index) : index < 6 ? b.Axis(index - 3) : a.Axis((index - 6) / 3).Cross(b.Axis((index - 6) % 3));
            var length = axis.Magnitude;
            if (length < 0.00001f) continue;
            axis /= length;
            var overlap = Radius(a, axis) + Radius(b, axis) - MathF.Abs(delta.Dot(axis));
            if (overlap < -Skin) return false;
            if (overlap >= depth) continue;
            depth = overlap; feature = index;
            normal = axis * (delta.Dot(axis) < 0 ? -1 : 1);
        }
        return feature >= 0;
    }
    private static float Radius(RigidBody body, Vector3 axis) => MathF.Abs(axis.Dot(body.Axis(0))) * body.Half(0)
        + MathF.Abs(axis.Dot(body.Axis(1))) * body.Half(1) + MathF.Abs(axis.Dot(body.Axis(2))) * body.Half(2);

    internal static List<Vector3> GetPoints(RigidBody a, RigidBody b, Vector3 normal, int feature)
    {
        if (feature >= 6) return [EdgeContact(a, b, normal, (feature - 6) / 3, (feature - 6) % 3)];
        var reference = feature < 3 ? a : b;
        var incident = feature < 3 ? b : a;
        var axis = feature % 3;
        var outward = feature < 3 ? normal : normal * -1;
        var center = reference.Frame.Position + outward * reference.Half(axis);
        var polygon = IncidentFace(incident, outward);
        for (var offset = 1; offset <= 2; offset++)
        {
            var side = (axis + offset) % 3;
            polygon = Clip(polygon, center, reference.Axis(side), reference.Half(side));
            polygon = Clip(polygon, center, reference.Axis(side) * -1, reference.Half(side));
        }
        var points = new List<Vector3>();
        foreach (var vertex in polygon)
        {
            var penetration = (center - vertex).Dot(outward);
            if (penetration >= -Skin) points.Add(vertex + outward * (penetration * 0.5f));
        }
        return points;
    }
    private static List<Vector3> IncidentFace(RigidBody body, Vector3 normal)
    {
        var axis = 0;
        for (var index = 1; index < 3; index++)
            if (MathF.Abs(body.Axis(index).Dot(normal)) > MathF.Abs(body.Axis(axis).Dot(normal))) axis = index;
        var direction = body.Axis(axis).Dot(normal) > 0 ? -1 : 1;
        var center = body.Frame.Position + body.Axis(axis) * (body.Half(axis) * direction);
        var u = body.Axis((axis + 1) % 3) * body.Half((axis + 1) % 3);
        var v = body.Axis((axis + 2) % 3) * body.Half((axis + 2) % 3);
        return [center + u + v, center - u + v, center - u - v, center + u - v];
    }
    private static List<Vector3> Clip(List<Vector3> polygon, Vector3 center, Vector3 normal, float half)
    {
        var result = new List<Vector3>();
        if (polygon.Count == 0) return result;
        var previous = polygon[^1]; var previousDistance = (previous - center).Dot(normal) - half;
        foreach (var current in polygon)
        {
            var distance = (current - center).Dot(normal) - half;
            if ((distance <= 0) != (previousDistance <= 0))
                result.Add(previous + (current - previous) * (previousDistance / (previousDistance - distance)));
            if (distance <= 0) result.Add(current);
            previous = current; previousDistance = distance;
        }
        return result;
    }
    private static Vector3 EdgeCenter(RigidBody body, Vector3 direction, int axis)
    {
        var point = body.Frame.Position;
        for (var index = 0; index < 3; index++)
            if (index != axis) point += body.Axis(index) * (body.Half(index) * (body.Axis(index).Dot(direction) < 0 ? -1 : 1));
        return point;
    }
    private static Vector3 EdgeContact(RigidBody a, RigidBody b, Vector3 normal, int edgeA, int edgeB)
    {
        var centerA = EdgeCenter(a, normal, edgeA); var centerB = EdgeCenter(b, normal * -1, edgeB);
        var u = a.Axis(edgeA); var v = b.Axis(edgeB); var delta = centerA - centerB;
        var dot = u.Dot(v); var divisor = 1 - dot * dot;
        var first = divisor > 1e-8f ? (dot * v.Dot(delta) - u.Dot(delta)) / divisor : 0;
        first = Math.Clamp(first, -a.Half(edgeA), a.Half(edgeA));
        var second = Math.Clamp(v.Dot(delta) + dot * first, -b.Half(edgeB), b.Half(edgeB));
        first = Math.Clamp(dot * second - u.Dot(delta), -a.Half(edgeA), a.Half(edgeA));
        return (centerA + u * first + centerB + v * second) * 0.5f;
    }
}
