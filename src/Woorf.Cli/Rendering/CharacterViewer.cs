using System.Numerics;
using Woorf.Engine.DataModel;
using Woorf.Graphics;
using Woorf.Graphics.Platform;

internal sealed class CharacterViewer(Player player)
{
    private readonly CharacterControls controls = new();
    private Part? followedRoot;
    private Model? inputCharacter;
    private Humanoid? inputHumanoid;
    private float yaw, pitch = 0.4f;
    internal Part? Root => player.Character?.PrimaryPart as Part;

    internal void Update(PerspectiveCamera camera, OpenGlWindow window, float elapsed)
    {
        float Axis(int positive, int negative) => (window.IsKeyDown(positive) ? 1 : 0) - (window.IsKeyDown(negative) ? 1 : 0);
        ApplyInput(camera, Axis('W', 'S'), Axis('D', 'A'), window.IsKeyDown(0x20), window.IsKeyDown('R'),
            Axis(0x25, 0x27), Axis(0x26, 0x28), elapsed);
    }

    internal void ApplyInput(PerspectiveCamera camera, float forward, float right, bool isJumpPressed, bool isResetPressed,
        float yawAxis, float pitchAxis, float elapsed)
    {
        yaw += yawAxis * elapsed * 1.5f;
        pitch = Math.Clamp(pitch + pitchAxis * elapsed, 0.15f, 1.15f);
        Follow(camera, 0);
        var input = controls.Update(camera, forward, right, isJumpPressed, isResetPressed);
        if (inputCharacter != player.Character)
        {
            inputCharacter = player.Character;
            inputHumanoid = inputCharacter?.FindFirstChild("Humanoid") as Humanoid;
        }
        var humanoid = inputHumanoid;
        if (humanoid is null || humanoid.IsDestroyed || humanoid.Parent != inputCharacter) return;
        humanoid.Move(input.MoveDirection);
        if (input.ShouldJump) humanoid.Jump = true;
        if (input.ShouldReset) humanoid.Health = 0;
    }

    internal void Follow(PerspectiveCamera camera, float elapsed)
    {
        var root = Root;
        if (root is null || root.IsDestroyed) { followedRoot = null; return; }
        var target = new Vector3(root.Position.X, root.Position.Y + 0.4f, root.Position.Z);
        var shouldSnap = followedRoot != root || Vector3.DistanceSquared(camera.Target, target) > 225;
        var blend = shouldSnap ? 1 : 1 - MathF.Exp(-12 * Math.Clamp(elapsed, 0, 0.1f));
        camera.Target = Vector3.Lerp(camera.Target, target, blend);
        camera.Position = camera.Target + new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch)) * 12;
        followedRoot = root;
    }
}
