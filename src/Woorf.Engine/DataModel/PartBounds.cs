using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

internal static class PartBounds
{
    internal static (CFrame Frame, Vector3 Size) Calculate(IEnumerable<BasePart> parts, CFrame orientation)
    {
        var rotation = orientation.Rotation;
        var inverse = rotation.Inverse();
        var minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        var hasParts = false;
        foreach (var part in parts)
        {
            hasParts = true;
            var relative = inverse * part.CFrame;
            var extent = CalculateHalfExtents(relative, part.Size);
            var lower = relative.Position - extent;
            var upper = relative.Position + extent;
            minimum = new(MathF.Min(minimum.X, lower.X), MathF.Min(minimum.Y, lower.Y), MathF.Min(minimum.Z, lower.Z));
            maximum = new(MathF.Max(maximum.X, upper.X), MathF.Max(maximum.Y, upper.Y), MathF.Max(maximum.Z, upper.Z));
        }
        if (!hasParts) return (orientation, default);
        var center = rotation.TransformVector((minimum + maximum) * 0.5f);
        return (rotation.WithPosition(center), maximum - minimum);
    }

    private static Vector3 CalculateHalfExtents(CFrame frame, Vector3 size)
    {
        var half = size * 0.5f;
        return new(
            MathF.Abs(frame.RightVector.X) * half.X + MathF.Abs(frame.UpVector.X) * half.Y + MathF.Abs(frame.ZVector.X) * half.Z,
            MathF.Abs(frame.RightVector.Y) * half.X + MathF.Abs(frame.UpVector.Y) * half.Y + MathF.Abs(frame.ZVector.Y) * half.Z,
            MathF.Abs(frame.RightVector.Z) * half.X + MathF.Abs(frame.UpVector.Z) * half.Y + MathF.Abs(frame.ZVector.Z) * half.Z);
    }
}
