using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Physics;

internal sealed class RigidBody(int id)
{
    internal int Id { get; } = id;
    internal CFrame Frame;
    internal Vector3 LinearVelocity, AngularVelocity, Size;
    internal PhysicalProperties Material;
    internal bool IsAnchored, CanCollide;
    internal Vector3 Minimum, Maximum;
    internal float InverseMass => IsAnchored ? 0 : 1 / MathF.Max(1e-9f, Size.X * Size.Y * Size.Z * Material.Density);
    internal Vector3 Axis(int index) => index switch { 0 => Frame.RightVector, 1 => Frame.UpVector, _ => Frame.ZVector };
    internal float Half(int index) => (index switch { 0 => Size.X, 1 => Size.Y, _ => Size.Z }) * 0.5f;
    internal Vector3 InverseInertia(Vector3 torque)
    {
        if (IsAnchored) return default;
        var scale = 12 * InverseMass;
        var local = new Vector3(torque.Dot(Axis(0)) * scale / (Size.Y * Size.Y + Size.Z * Size.Z),
            torque.Dot(Axis(1)) * scale / (Size.X * Size.X + Size.Z * Size.Z),
            torque.Dot(Axis(2)) * scale / (Size.X * Size.X + Size.Y * Size.Y));
        return Frame.TransformVector(local);
    }
    internal void Integrate(float gravity, float step)
    {
        if (IsAnchored) return;
        LinearVelocity += new Vector3(0, -gravity * step, 0);
        Frame = Frame.WithPosition(Frame.Position + LinearVelocity * step);
        var speed = AngularVelocity.Magnitude;
        if (speed > 1e-7f)
            Frame = (CFrame.FromAxisAngle(AngularVelocity / speed, speed * step) * Frame.Rotation)
                .WithPosition(Frame.Position).Orthonormalize();
    }
    internal void UpdateBounds()
    {
        var right = Axis(0); var up = Axis(1); var back = Axis(2);
        var extent = new Vector3(MathF.Abs(right.X) * Half(0) + MathF.Abs(up.X) * Half(1) + MathF.Abs(back.X) * Half(2),
            MathF.Abs(right.Y) * Half(0) + MathF.Abs(up.Y) * Half(1) + MathF.Abs(back.Y) * Half(2),
            MathF.Abs(right.Z) * Half(0) + MathF.Abs(up.Z) * Half(1) + MathF.Abs(back.Z) * Half(2));
        Minimum = Frame.Position - extent; Maximum = Frame.Position + extent;
    }
    internal Vector3 VelocityAt(Vector3 offset) => LinearVelocity + AngularVelocity.Cross(offset);
    internal void ApplyImpulse(Vector3 impulse, Vector3 offset)
    {
        if (IsAnchored) return;
        LinearVelocity += impulse * InverseMass;
        AngularVelocity += InverseInertia(offset.Cross(impulse));
    }
}
