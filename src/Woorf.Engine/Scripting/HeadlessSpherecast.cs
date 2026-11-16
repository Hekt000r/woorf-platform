using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Physics;

namespace Woorf.Engine.Scripting;

internal static class HeadlessSpherecast
{
    internal static object? Cast(Instance workspace, object? originValue, double radiusValue, object? directionValue, object? options)
    {
        if (originValue is not Vector3 origin || directionValue is not Vector3 direction
            || !float.IsFinite(origin.Magnitude) || !float.IsFinite(direction.Magnitude))
            throw new ArgumentException("Spherecast expects finite Vector3 origin and direction.");
        if (!double.IsFinite(radiusValue) || radiusValue <= 0 || radiusValue > 256)
            throw new ArgumentException("Spherecast radius must be positive and at most 256.");
        if (direction.Magnitude > 1024) throw new ArgumentException("Spherecast direction exceeds 1024 studs.");
        if (direction.Magnitude == 0) return null;
        var radius = (float)radiusValue;
        var filter = new QueryFilter(options);
        BasePart? closest = null; var fraction = float.PositiveInfinity; var normal = new Vector3();
        foreach (var part in workspace.GetDescendants().OfType<BasePart>())
        {
            if (!filter.Allows(part)) continue;
            var inverse = part.CFrame.Inverse(); var start = inverse.TransformPoint(origin); var delta = inverse.TransformVector(direction);
            bool isHit; float time; Vector3 localNormal;
            if (part is Part { Shape: PartType.Ball })
            {
                var combined = radius + MathF.Min(part.Size.X, MathF.Min(part.Size.Y, part.Size.Z)) * 0.5f;
                if (start.Magnitude <= combined) continue;
                isHit = ShapeQueries.RaySphere(start, delta, combined, out time, out localNormal);
            }
            else isHit = ShapeQueries.SweepBox(start, delta, part.Size * 0.5f, radius, out time, out localNormal);
            if (!isHit || time >= fraction) continue;
            closest = part; fraction = time; normal = part.CFrame.TransformVector(localNormal);
        }
        if (closest is null) return null;
        return new LuaTableValue(new KeyValuePair<object, object?>[]
        {
            new("Instance", closest), new("Position", origin + direction * fraction - normal * radius),
            new("Normal", normal), new("Distance", (double)(direction.Magnitude * fraction)),
        });
    }
}
