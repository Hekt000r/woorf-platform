using System.Numerics;
using Woorf.Graphics;
using EngineVector = Woorf.Engine.Datatypes.Vector3;

internal readonly record struct CharacterInput(EngineVector MoveDirection, bool ShouldJump, bool ShouldReset);

internal sealed class CharacterControls
{
    private bool wasJumpPressed, wasResetPressed;

    internal CharacterInput Update(PerspectiveCamera camera, float forwardAxis, float rightAxis, bool isJumpPressed, bool isResetPressed)
    {
        var forward = camera.Target - camera.Position;
        forward.Y = 0;
        forward = forward.LengthSquared() > 0.00001f ? Vector3.Normalize(forward) : -Vector3.UnitZ;
        var right = Vector3.Cross(forward, Vector3.UnitY);
        var direction = forward * forwardAxis + right * rightAxis;
        if (direction.LengthSquared() > 1) direction = Vector3.Normalize(direction);
        var result = new CharacterInput(new(direction.X, 0, direction.Z),
            isJumpPressed && !wasJumpPressed, isResetPressed && !wasResetPressed);
        wasJumpPressed = isJumpPressed; wasResetPressed = isResetPressed;
        return result;
    }
}
