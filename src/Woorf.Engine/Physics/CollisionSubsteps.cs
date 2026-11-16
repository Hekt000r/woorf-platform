using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Physics;

internal static class CollisionSubsteps
{
    internal static int Calculate(IReadOnlyList<RigidBody> bodies, float gravity, float step)
    {
        foreach (var body in bodies) body.UpdateBounds();
        var subdivisions = 1;
        for (var index = 0; index < bodies.Count; index++)
        {
            var a = bodies[index];
            if (a.IsAnchored || !a.CanCollide) continue;
            for (var other = 0; other < bodies.Count; other++)
            {
                var b = bodies[other];
                if (!b.CanCollide || a == b || !b.IsAnchored && a.Id >= b.Id) continue;
                var relative = a.LinearVelocity - (b.IsAnchored ? default : b.LinearVelocity);
                var acceleration = b.IsAnchored ? new Vector3(0, -gravity * step * step, 0) : default;
                var travel = relative * step + acceleration;
                var angular = AngularTravel(a, step) + AngularTravel(b, step);
                if (!MayIntersect(a, b, travel, angular + acceleration.Magnitude)) continue;
                var minimum = MinimumSize(a);
                if (!b.IsAnchored) minimum = MathF.Min(minimum, MinimumSize(b));
                var required = MathF.Ceiling((relative.Magnitude * step + acceleration.Magnitude + angular) / MathF.Max(minimum * 0.25f, 0.00025f));
                subdivisions = Math.Max(subdivisions, (int)Math.Clamp(required, 1, 64));
            }
        }
        return subdivisions;
    }

    private static float MinimumSize(RigidBody body) => MathF.Min(body.Size.X, MathF.Min(body.Size.Y, body.Size.Z));
    private static float AngularTravel(RigidBody body, float step) => body.IsAnchored || body.IsRotationLocked || body.Shape == PartType.Ball
        ? 0 : body.AngularVelocity.Magnitude * body.Size.Magnitude * 0.5f * step;

    private static bool MayIntersect(RigidBody a, RigidBody b, Vector3 travel, float angular)
    {
        var padding = angular + BoxCollision.Skin;
        return a.Minimum.X + MathF.Min(0, travel.X) <= b.Maximum.X + padding
            && a.Maximum.X + MathF.Max(0, travel.X) >= b.Minimum.X - padding
            && a.Minimum.Y + MathF.Min(0, travel.Y) <= b.Maximum.Y + padding
            && a.Maximum.Y + MathF.Max(0, travel.Y) >= b.Minimum.Y - padding
            && a.Minimum.Z + MathF.Min(0, travel.Z) <= b.Maximum.Z + padding
            && a.Maximum.Z + MathF.Max(0, travel.Z) >= b.Minimum.Z - padding;
    }
}
