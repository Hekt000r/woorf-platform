using System.Numerics;
using Woorf.Graphics;
using Woorf.Graphics.Platform;

internal static class ViewerCamera
{
    internal static void Update(PerspectiveCamera camera, OpenGlWindow window, float elapsed)
    {
        float Axis(int positive, int negative) => (window.IsKeyDown(positive) ? 1 : 0) - (window.IsKeyDown(negative) ? 1 : 0);
        var forward = Vector3.Normalize(camera.Target - camera.Position);
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        var speed = window.IsKeyDown(0x10) ? 35 : 12;
        var movement = (forward * Axis('W', 'S') + right * Axis('D', 'A') + Vector3.UnitY * Axis('E', 'Q')) * speed * elapsed;
        camera.Position += movement; camera.Target += movement;
        var yaw = Axis(0x25, 0x27) * elapsed;
        var pitch = Axis(0x26, 0x28) * elapsed;
        var offset = camera.Position - camera.Target;
        offset = Vector3.Transform(offset, Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw));
        var candidate = Vector3.Transform(offset, Quaternion.CreateFromAxisAngle(right, pitch));
        if (MathF.Abs(Vector3.Normalize(candidate).Y) < 0.98f) offset = candidate;
        camera.Position = camera.Target + offset;
    }
}
