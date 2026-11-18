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
    private float? zoom;
    private Vector3 orbitTarget;
    private float Distance => ClampDistance(zoom ?? ReadSetting(player, "ViewDistance", 12, 6, 60));
    internal bool IsFirstPerson => player.CameraMode.Name == "LockFirstPerson" || Distance <= .5f;

    private float ClampDistance(float distance) =>
        Math.Clamp(distance, (float)player.CameraMinZoomDistance, (float)player.CameraMaxZoomDistance);

    internal void ApplyPointer(float deltaX, float deltaY, float wheel, bool isRightDown)
    {
        if (wheel != 0 && player.CameraMode.Name != "LockFirstPerson")
        {
            var distance = Distance;
            var next = (distance + 1.5f) * MathF.Pow(1.3f, -wheel) - 1.5f;
            zoom = ClampDistance(next);
        }
        if (isRightDown || IsFirstPerson)
        {
            yaw -= deltaX * .004f;
            pitch = Math.Clamp(pitch + deltaY * .004f, -1.5f, 1.5f);
        }
    }

    internal Part? Root => player.Character?.PrimaryPart as Part;

    internal void ApplyInput(PerspectiveCamera camera, float forward, float right, bool isJumpPressed, bool isResetPressed,
        float yawAxis, float pitchAxis, float elapsed)
    {
        yaw += yawAxis * elapsed * 1.5f;
        pitch = Math.Clamp(pitch + pitchAxis * elapsed, -1.5f, 1.5f);
        Follow(camera, 0);
        var input = controls.Update(camera, forward, right, isJumpPressed, isResetPressed, IsFirstPerson);
        if (sendInput is not null) { sendInput(input); return; }
        if (inputCharacter != player.Character)
        {
            inputCharacter = player.Character;
            inputHumanoid = inputCharacter?.FindFirstChild("Humanoid") as Humanoid;
        }
        var humanoid = inputHumanoid;
        if (humanoid is null || humanoid.IsDestroyed || humanoid.Parent != inputCharacter) return;
        humanoid.CameraHeading = input.CameraHeading;
        humanoid.Move(input.MoveDirection);
        if (input.ShouldJump) humanoid.Jump = true;
        if (input.ShouldReset) humanoid.Health = 0;
    }

    internal void Follow(PerspectiveCamera camera, float elapsed)
    {
        var root = Root;
        if (root is null || root.IsDestroyed) { followedRoot = null; return; }
        var target = new Vector3(root.Position.X, root.Position.Y + 0.4f, root.Position.Z);
        var shouldSnap = followedRoot != root || Vector3.DistanceSquared(orbitTarget, target) > 225;
        var blend = shouldSnap ? 1 : 1 - MathF.Exp(-12 * Math.Clamp(elapsed, 0, 0.1f));
        orbitTarget = Vector3.Lerp(orbitTarget, target, blend);
        var back = new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
        if (IsFirstPerson)
        {
            camera.Position = target + new Vector3(0, 1.6f, 0);
            camera.Target = camera.Position - back;
        }
        else
        {
            camera.Target = orbitTarget;
            camera.Position = orbitTarget + back * Distance;
        }
        followedRoot = root;
    }

    private static float ReadSetting(Player player,string name,float fallback,float minimum,float maximum) =>
        player.GetAttribute(name) is double value && double.IsFinite(value)?Math.Clamp((float)value,minimum,maximum):fallback;
}

