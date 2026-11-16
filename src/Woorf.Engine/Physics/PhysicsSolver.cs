using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Physics;

internal static class PhysicsSolver
{
    internal static IReadOnlyList<BodyContact> Step(IReadOnlyList<RigidBody> bodies, float gravity, float step)
    {
        var subdivisions = 1;
        foreach (var body in bodies.Where(body => !body.IsAnchored && body.CanCollide))
        {
            var minimum = MathF.Min(body.Size.X, MathF.Min(body.Size.Y, body.Size.Z));
            var speed = body.LinearVelocity.Magnitude + MathF.Abs(gravity) * step;
            if (!body.IsRotationLocked) speed += body.AngularVelocity.Magnitude * body.Size.Magnitude * 0.5f;
            var required = MathF.Ceiling(speed * step / MathF.Max(minimum * 0.25f, 0.00025f));
            subdivisions = Math.Max(subdivisions, (int)Math.Clamp(required, 1, 64));
        }
        IReadOnlyList<BodyContact> contacts = [];
        for (var index = 0; index < subdivisions; index++) contacts = StepDiscrete(bodies, gravity, step / subdivisions, step);
        return contacts;
    }
    private static IReadOnlyList<BodyContact> StepDiscrete(IReadOnlyList<RigidBody> bodies, float gravity, float step, float fixedStep)
    {
        foreach (var body in bodies) { body.Integrate(gravity, step); body.UpdateBounds(); }
        var sorted = bodies.Where(body => body.CanCollide).OrderBy(body => body.Minimum.X).ThenBy(body => body.Id).ToArray();
        var contacts = new List<ContactConstraint>();
        var pairs = new List<(RigidBody A, RigidBody B, Vector3 Normal, float Depth)>();
        for (var index = 0; index < sorted.Length; index++)
        {
            var a = sorted[index];
            for (var other = index + 1; other < sorted.Length && sorted[other].Minimum.X <= a.Maximum.X + BoxCollision.Skin; other++)
            {
                var b = sorted[other];
                if (a.IsAnchored && b.IsAnchored || !Overlaps(a, b)) continue;
                if (!ShapeCollision.Find(a, b, out var normal, out var depth, out var points)) continue;
                foreach (var point in points) contacts.Add(new(a, b, normal, point, MathF.Max(1, MathF.Abs(gravity) * fixedStep * 2)));
                pairs.Add((a, b, normal, depth));
            }
        }
        for (var iteration = 0; iteration < 24; iteration++)
            foreach (var contact in contacts) contact.Solve();
        foreach (var pair in pairs)
        {
            var inverseMass = pair.A.InverseMass + pair.B.InverseMass;
            var correction = pair.Normal * (0.8f * MathF.Max(0, pair.Depth - 0.002f) / inverseMass);
            pair.A.Frame = pair.A.Frame.WithPosition(pair.A.Frame.Position - correction * pair.A.InverseMass);
            pair.B.Frame = pair.B.Frame.WithPosition(pair.B.Frame.Position + correction * pair.B.InverseMass);
        }
        var result = new List<BodyContact>();
        foreach (var pair in pairs)
        {
            if (!ShapeCollision.Find(pair.A, pair.B, out var normal, out var depth, out var points)) continue;
            foreach (var point in points) result.Add(new(pair.A, pair.B, point, normal, -depth));
        }
        return result;
    }
    private static bool Overlaps(RigidBody a, RigidBody b) => a.Minimum.Y <= b.Maximum.Y + BoxCollision.Skin
        && b.Minimum.Y <= a.Maximum.Y + BoxCollision.Skin && a.Minimum.Z <= b.Maximum.Z + BoxCollision.Skin && b.Minimum.Z <= a.Maximum.Z + BoxCollision.Skin;
}

internal readonly record struct BodyContact(RigidBody A, RigidBody B, Vector3 Point, Vector3 Normal, float Separation);
