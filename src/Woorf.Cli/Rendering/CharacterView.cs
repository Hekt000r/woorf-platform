using System.Numerics;
using Woorf.Engine.DataModel;
using Woorf.Graphics;

internal sealed class CharacterView
{
    private readonly RenderItem[] items = new RenderItem[8];
    private Part? previousRoot;
    private Vector3 previousPosition;
    private float phase, yaw;

    internal RenderItem[] Capture(Part root, Humanoid humanoid, double now)
    {
        var position = new Vector3(root.Position.X, root.Position.Y, root.Position.Z);
        var motion = previousRoot == root ? position - previousPosition : Vector3.Zero;
        motion.Y = 0;
        var distance = motion.Length();
        if (distance > 0.001f && distance < 4)
        {
            phase += distance * 1.1f;
            yaw = MathF.Atan2(-motion.X, -motion.Z);
        }
        previousRoot = root; previousPosition = position;
        var isAlive = humanoid.Health > 0;
        var isAirborne = isAlive && MathF.Abs(root.AssemblyLinearVelocity.Y) > 3;
        var swing = isAlive && distance > 0.001f && distance < 4 && !isAirborne ? MathF.Sin(phase) * 0.7f : 0;
        var leg = isAirborne ? 0.25f : swing;
        var rightLeg = isAirborne ? 0.25f : -swing;
        var arm = isAirborne ? -0.65f : -swing;
        var rightArm = isAirborne ? -0.65f : swing;
        var frame = Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(position);
        var skin = isAlive ? new Vector3(0.95f, 0.63f, 0.31f) : new Vector3(0.25f);
        var shirt = isAlive ? new Vector3(0.035f, 0.32f, 0.8f) : new Vector3(0.15f);
        items[0] = Create(MeshShape.Box, new(1.15f, 1.45f, 0.7f), new(0, 0.1f, 0), frame, shirt);
        items[1] = Create(MeshShape.Sphere, new(0.55f), new(0, 1.35f, 0), frame, skin);
        items[2] = Limb(new(0.4f, 1.45f, 0.5f), new(-0.82f, 0.8f, 0), arm, frame, skin);
        items[3] = Limb(new(0.4f, 1.45f, 0.5f), new(0.82f, 0.8f, 0), rightArm, frame, skin);
        items[4] = Limb(new(0.5f, 1.75f, 0.65f), new(-0.3f, -0.25f, 0), leg, frame, new(0.045f, 0.075f, 0.14f));
        items[5] = Limb(new(0.5f, 1.75f, 0.65f), new(0.3f, -0.25f, 0), rightLeg, frame, new(0.045f, 0.075f, 0.14f));
        items[6] = Create(MeshShape.Box, new(0.105f, 0.13f, 0.075f), new(-0.18f, 1.43f, -0.51f), frame, new(0.015f));
        items[7] = Create(MeshShape.Box, new(0.105f, 0.13f, 0.075f), new(0.18f, 1.43f, -0.51f), frame, new(0.015f));
        return items;
    }

    private static RenderItem Create(MeshShape shape, Vector3 size, Vector3 position, Matrix4x4 frame, Vector3 color) =>
        new(shape, Matrix4x4.CreateScale(size) * Matrix4x4.CreateTranslation(position) * frame, color);

    private static RenderItem Limb(Vector3 size, Vector3 pivot, float angle, Matrix4x4 frame, Vector3 color) =>
        new(MeshShape.Box, Matrix4x4.CreateScale(size) * Matrix4x4.CreateTranslation(0, -size.Y / 2, 0)
            * Matrix4x4.CreateRotationX(angle) * Matrix4x4.CreateTranslation(pivot) * frame, color);
}
