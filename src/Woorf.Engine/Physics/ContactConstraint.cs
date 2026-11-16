using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Physics;

internal sealed class ContactConstraint
{
    private readonly RigidBody a, b;
    private readonly Vector3 normal, offsetA, offsetB;
    private readonly float restitutionVelocity, friction;
    private float normalImpulse;
    private Vector3 frictionImpulse;
    internal ContactConstraint(RigidBody a, RigidBody b, Vector3 normal, Vector3 point, float bounceThreshold)
    {
        this.a = a; this.b = b; this.normal = normal;
        offsetA = point - a.Frame.Position; offsetB = point - b.Frame.Position;
        var speed = RelativeVelocity().Dot(normal);
        var elasticity = Mix(a.Material.Elasticity, a.Material.ElasticityWeight, b.Material.Elasticity, b.Material.ElasticityWeight);
        restitutionVelocity = speed < -bounceThreshold ? -elasticity * speed : 0;
        friction = Mix(a.Material.Friction, a.Material.FrictionWeight, b.Material.Friction, b.Material.FrictionWeight);
    }
    private static float Mix(float first, float firstWeight, float second, float secondWeight) => firstWeight + secondWeight > 0
        ? (first * firstWeight + second * secondWeight) / (firstWeight + secondWeight) : (first + second) * 0.5f;
    private Vector3 RelativeVelocity() => b.VelocityAt(offsetB) - a.VelocityAt(offsetA);
    private float EffectiveMass(Vector3 axis) => a.InverseMass + b.InverseMass
        + axis.Dot(a.InverseInertia(offsetA.Cross(axis)).Cross(offsetA) + b.InverseInertia(offsetB.Cross(axis)).Cross(offsetB));
    private void Apply(Vector3 impulse) { a.ApplyImpulse(impulse * -1, offsetA); b.ApplyImpulse(impulse, offsetB); }
    internal void Solve()
    {
        var mass = EffectiveMass(normal);
        if (mass <= 1e-12f) return;
        var previous = normalImpulse;
        normalImpulse = MathF.Max(0, normalImpulse + (restitutionVelocity - RelativeVelocity().Dot(normal)) / mass);
        Apply(normal * (normalImpulse - previous));
        var velocity = RelativeVelocity();
        var tangent = velocity - normal * velocity.Dot(normal);
        var speed = tangent.Magnitude;
        if (speed < 1e-8f) return;
        tangent /= speed;
        var tangentMass = EffectiveMass(tangent);
        if (tangentMass <= 1e-12f) return;
        var next = frictionImpulse - tangent * (speed / tangentMass);
        var limit = friction * normalImpulse;
        if (next.Magnitude > limit) next = next.Unit * limit;
        Apply(next - frictionImpulse);
        frictionImpulse = next;
    }
}
