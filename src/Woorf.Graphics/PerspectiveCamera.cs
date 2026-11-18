using System.Numerics;

namespace Woorf.Graphics;

public sealed class PerspectiveCamera
{
    public Vector3 Position { get; set; } = new(18, 14, 22);
    public Vector3 Target { get; set; } = new(0, 2, 0);
    public Vector3? Up { get; set; }
    public float FieldOfView { get; set; } = MathF.PI / 3;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 2000;

    public Matrix4x4 GetSkyInverseViewProjection(float aspect)
    {
        var centered = new PerspectiveCamera { Position = Vector3.Zero, Target = Target - Position,
            FieldOfView = FieldOfView, NearPlane = NearPlane, FarPlane = FarPlane, Up = Up };
        if (!Matrix4x4.Invert(centered.GetViewProjection(aspect), out var inverse))
            throw new InvalidOperationException("Sky camera must be invertible.");
        return inverse;
    }

    public Matrix4x4 GetViewProjection(float aspect)
    {
        if (!float.IsFinite(aspect) || aspect <= 0 || !float.IsFinite(FieldOfView) || FieldOfView <= 0 || FieldOfView >= MathF.PI
            || !float.IsFinite(NearPlane) || !float.IsFinite(FarPlane) || NearPlane <= 0 || FarPlane <= NearPlane)
            throw new ArgumentOutOfRangeException(nameof(aspect), "Camera requires a valid perspective frustum.");
        if (!IsFinite(Position) || !IsFinite(Target) || Vector3.DistanceSquared(Position, Target) < 1e-10f)
            throw new InvalidOperationException("Camera position and target must be finite and distinct.");
        var direction = Vector3.Normalize(Target - Position);
        var up = Up ?? (MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.999f ? Vector3.UnitZ : Vector3.UnitY);
        var scale = 1 / MathF.Tan(FieldOfView / 2);
        // System.Numerics uses row vectors; transpose on upload is implicit in the column-major GL interpretation.
        var projection = new Matrix4x4(scale / aspect, 0, 0, 0, 0, scale, 0, 0,
            0, 0, (FarPlane + NearPlane) / (NearPlane - FarPlane), -1,
            0, 0, 2 * FarPlane * NearPlane / (NearPlane - FarPlane), 0);
        return Matrix4x4.CreateLookAt(Position, Target, up) * projection;
    }

    private static bool IsFinite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
