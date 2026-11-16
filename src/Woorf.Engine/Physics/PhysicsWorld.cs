using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Physics;

internal sealed class PhysicsWorld : IDisposable
{
    internal const double FixedStep = 1d / 120;
    private readonly Workspace workspace;
    private readonly Dictionary<BasePart, RigidBody> bodies = [];
    private bool isHierarchyDirty = true;
    private int nextId;
    private double previousTime, accumulator;
    internal bool HasDynamicBodies => bodies.Keys.Any(part => !part.Anchored);
    internal PhysicsWorld(Workspace workspace, double now)
    {
        this.workspace = workspace; previousTime = now;
        workspace.DescendantMutated += OnMutation;
    }
    private void OnMutation(Instance instance, string property)
    {
        if (property == "Parent") isHierarchyDirty = true;
    }
    private void Synchronize()
    {
        if (isHierarchyDirty)
        {
            var current = workspace.GetDescendants().OfType<BasePart>().Where(part => !part.IsDestroyed).ToArray();
            var membership = current.ToHashSet();
            foreach (var removed in bodies.Keys.Where(part => !membership.Contains(part)).ToArray()) bodies.Remove(removed);
            foreach (var added in current)
                if (!bodies.ContainsKey(added)) bodies.Add(added, new(++nextId));
            isHierarchyDirty = false;
        }
        foreach (var (part, body) in bodies)
        {
            body.Frame = part.CFrame; body.Size = part.Size; body.IsAnchored = part.Anchored;
            body.CanCollide = part.CanCollide; body.Material = part.CustomPhysicalProperties ?? PhysicalProperties.Default;
            body.LinearVelocity = part.AssemblyLinearVelocity; body.AngularVelocity = part.AssemblyAngularVelocity;
        }
    }
    internal void Advance(double now)
    {
        Synchronize();
        var originals = bodies.Keys.ToDictionary(part => part, Snapshot.Capture);
        accumulator += Math.Clamp(now - previousTime, 0, 0.25);
        previousTime = now;
        var valid = bodies.Values.Where(body => IsFinite(body.Frame) && IsFinite(body.Size)).OrderBy(body => body.Id).ToArray();
        while (accumulator + 1e-9 >= FixedStep)
        {
            PhysicsSolver.Step(valid, (float)workspace.Gravity, (float)FixedStep);
            accumulator = Math.Max(0, accumulator - FixedStep);
        }
        foreach (var (part, body) in bodies.ToArray())
        {
            if (part.IsDestroyed || part.Anchored || !part.IsDescendantOf(workspace) || !IsFinite(body.Frame)) continue;
            if (Snapshot.Capture(part) != originals[part]) continue;
            part.ApplyPhysicsState(body.Frame, body.LinearVelocity, body.AngularVelocity);
        }
    }
    private readonly record struct Snapshot(CFrame Frame, Vector3 Size, Vector3 Linear, Vector3 Angular, bool Anchored, bool CanCollide, PhysicalProperties? Material)
    {
        internal static Snapshot Capture(BasePart part) => new(part.CFrame, part.Size, part.AssemblyLinearVelocity,
            part.AssemblyAngularVelocity, part.Anchored, part.CanCollide, part.CustomPhysicalProperties);
    }
    private static bool IsFinite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    private static bool IsFinite(CFrame value) => IsFinite(value.Position) && IsFinite(value.RightVector) && IsFinite(value.UpVector) && IsFinite(value.ZVector);
    public void Dispose() => workspace.DescendantMutated -= OnMutation;
}
