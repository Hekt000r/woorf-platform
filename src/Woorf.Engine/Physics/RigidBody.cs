using Woorf.Engine.Datatypes;
using Woorf.Engine.DataModel;

namespace Woorf.Engine.Physics;

internal sealed class RigidBody(int id)
{
    internal int Id { get; } = id;
    internal CFrame Frame;
    internal Vector3 LinearVelocity, AngularVelocity, Size;
    internal PhysicalProperties Material;
    internal bool IsAnchored, CanCollide;
    internal bool CanTouch = true;
    internal bool IsRotationLocked;
    internal PartType Shape = PartType.Block;
    internal float Radius => MathF.Min(Size.X, MathF.Min(Size.Y, Size.Z)) * 0.5f;
    internal float Volume => Shape == PartType.Ball ? 4f / 3 * MathF.PI * Radius * Radius * Radius : Size.X * Size.Y * Size.Z;
    internal Vector3 Minimum, Maximum;
    internal float InverseMass => IsAnchored ? 0 : 1 / MathF.Max(1e-9f, Volume * Material.Density);
    internal Vector3 Axis(int index) => index switch { 0 => Frame.RightVector, 1 => Frame.UpVector, _ => Frame.ZVector };
    internal float Half(int index) => (index switch { 0 => Size.X, 1 => Size.Y, _ => Size.Z }) * 0.5f;
    internal Vector3 InverseInertia(Vector3 torque)
    {
        if (IsAnchored || IsRotationLocked) return default;
        if (Shape == PartType.Ball) return torque * (2.5f * InverseMass / (Radius * Radius));
        var scale = 12 * InverseMass;
        var local = new Vector3(torque.Dot(Axis(0)) * scale / (Size.Y * Size.Y + Size.Z * Size.Z),
            torque.Dot(Axis(1)) * scale / (Size.X * Size.X + Size.Z * Size.Z),
            torque.Dot(Axis(2)) * scale / (Size.X * Size.X + Size.Y * Size.Y));
        return Frame.TransformVector(local);
    }
    internal void Integrate(float gravity, float step)
    {
        if (IsRotationLocked) AngularVelocity = default;
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
        if (Shape == PartType.Ball)
        {
            var radius = new Vector3(Radius, Radius, Radius);
            Minimum = Frame.Position - radius; Maximum = Frame.Position + radius;
        }
    }
    internal Vector3 VelocityAt(Vector3 offset) => LinearVelocity + AngularVelocity.Cross(offset);
    internal void ApplyImpulse(Vector3 impulse, Vector3 offset)
    {
        if (IsAnchored) return;
        LinearVelocity += impulse * InverseMass;
        AngularVelocity += InverseInertia(offset.Cross(impulse));
    }
}
