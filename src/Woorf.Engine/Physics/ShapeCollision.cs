using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Physics;

internal static class ShapeCollision
{
    internal static bool Find(RigidBody a, RigidBody b, out Vector3 normal, out float depth, out List<Vector3> points)
    {
        points = [];
        if (a.Shape == PartType.Block && b.Shape == PartType.Block)
        {
            if (!BoxCollision.FindAxis(a, b, out normal, out depth, out var feature)) return false;
            points = BoxCollision.GetPoints(a, b, normal, feature); return true;
        }
        if (a.Shape == PartType.Ball && b.Shape == PartType.Ball)
        {
            var delta = b.Frame.Position - a.Frame.Position;
            var distance = delta.Magnitude;
            normal = distance > 1e-8f ? delta / distance : new Vector3(1, 0, 0);
            depth = a.Radius + b.Radius - distance;
            points.Add(a.Frame.Position + normal * (a.Radius - depth * 0.5f));
            return depth >= -BoxCollision.Skin;
        }
        var sphere = a.Shape == PartType.Ball ? a : b;
        var box = a.Shape == PartType.Ball ? b : a;
        var localCenter = box.Frame.Inverse().TransformPoint(sphere.Frame.Position);
        var half = box.Size * 0.5f;
        var closest = new Vector3(Math.Clamp(localCenter.X, -half.X, half.X),
            Math.Clamp(localCenter.Y, -half.Y, half.Y), Math.Clamp(localCenter.Z, -half.Z, half.Z));
        var offset = closest - localCenter;
        var length = offset.Magnitude;
        if (length > 1e-8f)
        {
            normal = box.Frame.TransformVector(offset / length);
            depth = sphere.Radius - length;
        }
        else
        {
            var gaps = new[] { half.X - MathF.Abs(localCenter.X), half.Y - MathF.Abs(localCenter.Y), half.Z - MathF.Abs(localCenter.Z) };
            var axis = Array.IndexOf(gaps, gaps.Min());
            var coordinate = axis == 0 ? localCenter.X : axis == 1 ? localCenter.Y : localCenter.Z;
            normal = box.Axis(axis) * (coordinate < 0 ? 1 : -1);
            depth = sphere.Radius + gaps[axis];
        }
        points.Add(sphere.Frame.Position + normal * (sphere.Radius - depth * 0.5f));
        if (sphere != a) normal *= -1;
        return depth >= -BoxCollision.Skin;
    }
}
