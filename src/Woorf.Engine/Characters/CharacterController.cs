using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Physics;

namespace Woorf.Engine.Characters;

internal sealed class CharacterController : IDisposable
{
    private readonly PhysicsWorld world;
    private readonly Part collider;
    private readonly Humanoid humanoid;
    private bool isDisposed;
    private const float WalkableNormalY = 0.7f;

    internal CharacterController(PhysicsWorld world, Part collider, Humanoid humanoid)
    {
        this.world = world; this.collider = collider; this.humanoid = humanoid;
        world.SetRotationLocked(collider, true);
        world.BeforeStep += BeforeStep;
    }

    internal bool IsGrounded => !isDisposed && TryGetSupport(out _);
    internal void SetMoveDirection(Vector3 direction) => humanoid.Move(direction);
    internal void RequestJump() => humanoid.Jump = true;

    private bool TryGetSupport(out Vector3 normal)
    {
        normal = default;
        var body = world.GetBody(collider);
        if (body is null || collider.Anchored || humanoid.Health <= 0) return false;
        foreach (var contact in world.GetContacts(collider))
        {
            if (contact.Normal.Y < WalkableNormalY || body.LinearVelocity.Dot(contact.Normal) > 1) continue;
            if (contact.Normal.Y > normal.Y) normal = contact.Normal;
        }
        return normal.Y >= WalkableNormalY;
    }

    private void BeforeStep(float elapsed)
    {
        var shouldJump = humanoid.ConsumeJump();
        var body = world.GetBody(collider);
        if (body is null || collider.Anchored) return;
        var desired = humanoid.Health > 0 ? humanoid.MoveDirection * (float)humanoid.WalkSpeed : default;
        var isGrounded = TryGetSupport(out var support);
        var velocityY = body.LinearVelocity.Y;
        if (isGrounded)
        {
            var projected = desired - support * desired.Dot(support);
            if (projected.Magnitude > 1e-6f) projected = projected.Unit * desired.Magnitude;
            desired = projected;
            velocityY = desired.Y;
        }
        if (shouldJump && isGrounded && humanoid.Health > 0) velocityY = (float)humanoid.JumpPower;
        desired = ClipAgainstWalls(desired);
        body.LinearVelocity = new(desired.X, velocityY, desired.Z);
    }

    private Vector3 ClipAgainstWalls(Vector3 desired)
    {
        foreach (var contact in world.GetContacts(collider))
        {
            if (contact.Normal.Y >= WalkableNormalY) continue;
            var horizontal = new Vector3(contact.Normal.X, 0, contact.Normal.Z);
            if (horizontal.Magnitude < 1e-6f) continue;
            var normal = horizontal.Unit;
            var inward = desired.Dot(normal);
            if (inward < 0) desired -= normal * inward;
        }
        return desired;
    }

    public void Dispose()
    {
        if (isDisposed) return;
        world.BeforeStep -= BeforeStep;
        world.SetRotationLocked(collider, false);
        isDisposed = true;
    }
}
