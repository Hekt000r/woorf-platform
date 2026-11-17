using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

public sealed partial class Humanoid
{
    private double walkSpeed = 16, jumpPower = 30;
    private Vector3 moveDirection;
    private bool shouldJump;

    public double WalkSpeed
    {
        get => walkSpeed;
        set { ValidateMovementNumber(value); if (walkSpeed == value) return; walkSpeed = value; FirePropertyChanged("WalkSpeed"); }
    }
    public double JumpPower
    {
        get => jumpPower;
        set { ValidateMovementNumber(value); if (jumpPower == value) return; jumpPower = value; FirePropertyChanged("JumpPower"); }
    }
    public Vector3 MoveDirection => moveDirection;
    public bool Jump
    {
        get => shouldJump;
        set { if (shouldJump == value) return; shouldJump = value; FirePropertyChanged("Jump"); }
    }

    public void Move(Vector3 direction)
    {
        if (!float.IsFinite(direction.X) || !float.IsFinite(direction.Y) || !float.IsFinite(direction.Z))
            throw new ArgumentException("Movement direction must be finite.", nameof(direction));
        var horizontal = new Vector3(direction.X, 0, direction.Z);
        var maximum = MathF.Max(MathF.Abs(horizontal.X), MathF.Abs(horizontal.Z));
        if (maximum > 1) horizontal /= maximum;
        if (maximum > 1 || horizontal.Magnitude > 1) horizontal = horizontal.Unit;
        if (moveDirection == horizontal) return;
        moveDirection = horizontal;
        FirePropertyChanged("MoveDirection");
    }

    internal bool ConsumeJump()
    {
        var pending = shouldJump;
        Jump = false;
        return pending;
    }
    private static void ValidateMovementNumber(double value)
    {
        if (!double.IsFinite(value) || value < 0 || value > float.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), "Movement tuning must be finite and nonnegative.");
    }
}
