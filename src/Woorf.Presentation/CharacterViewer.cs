namespace Woorf.Presentation;

using System.Numerics;
using Woorf.Engine.DataModel;
using Woorf.Graphics;

internal sealed class CharacterViewer(Player player, Action<CharacterInput>? sendInput = null)
{
    private readonly CharacterControls controls = new();
    private Part? followedRoot;
    private Model? inputCharacter;
    private Humanoid? inputHumanoid;
    private float yaw, pitch = ReadSetting(player,"ViewPitch",.4f,.15f,1.15f);
    internal Part? Root => player.Character?.PrimaryPart as Part;

    internal void ApplyInput(PerspectiveCamera camera, float forward, float right, bool isJumpPressed, bool isResetPressed,
        float yawAxis, float pitchAxis, float elapsed)
    {
        yaw += yawAxis * elapsed * 1.5f;
        pitch = Math.Clamp(pitch + pitchAxis * elapsed, 0.15f, 1.15f);
        Follow(camera, 0);
        var input = controls.Update(camera, forward, right, isJumpPressed, isResetPressed);
        if (sendInput is not null) { sendInput(input); return; }
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
        camera.Position = camera.Target + new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch))
            * ReadSetting(player,"ViewDistance",12,6,60);
        followedRoot = root;
    }

    private static float ReadSetting(Player player,string name,float fallback,float minimum,float maximum) =>
        player.GetAttribute(name) is double value && double.IsFinite(value)?Math.Clamp((float)value,minimum,maximum):fallback;
}

