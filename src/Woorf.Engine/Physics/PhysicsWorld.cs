using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Physics;

internal sealed class PhysicsWorld : IDisposable
{
    internal const double FixedStep = 1d / 120;
    private readonly Workspace workspace;
    private readonly PhysicsTouchTracker touches;
    private readonly Dictionary<BasePart, RigidBody> bodies = [];
    private bool isHierarchyDirty = true;
    private int nextId;
    private double previousTime, accumulator;
    private readonly HashSet<BasePart> rotationLocked = [];
    private readonly Dictionary<BasePart, List<PhysicsContact>> contacts = [];
    private readonly Dictionary<BasePart, Snapshot> expectedStates = [];
    internal event Action<float>? BeforeStep;
    internal RigidBody? GetBody(BasePart part) => !part.IsDestroyed && part.IsDescendantOf(workspace) ? bodies.GetValueOrDefault(part) : null;
    internal void SetRotationLocked(BasePart part, bool value)
    {
        if (value) rotationLocked.Add(part); else rotationLocked.Remove(part);
        if (bodies.TryGetValue(part, out var body)) body.IsRotationLocked = value;
    }
    internal IReadOnlyList<PhysicsContact> GetContacts(BasePart part) =>
        IsContactCurrent(part) && contacts.TryGetValue(part, out var found)
            ? found.Where(contact => IsContactCurrent(contact.Other)).ToArray() : [];
    private bool IsContactCurrent(BasePart part) => !part.IsDestroyed && part.CanCollide && part.IsDescendantOf(workspace)
        && expectedStates.TryGetValue(part, out var expected) && Snapshot.Capture(part) == expected;
    internal bool HasDynamicBodies => bodies.Keys.Any(part => !part.Anchored);
    internal PhysicsWorld(Workspace workspace, double now)
    {
        this.workspace = workspace; touches = new(workspace); previousTime = now;
        workspace.DescendantMutated += OnMutation;
    }
    private void OnMutation(Instance instance, string property)
    {
        if (property == "Parent") isHierarchyDirty = true;
    }
    private void Synchronize()
    {
        var changedTouchGeometry = new HashSet<BasePart>();
        if (isHierarchyDirty)
        {
            var current = workspace.GetDescendants().OfType<BasePart>().Where(part => !part.IsDestroyed).ToArray();
            var membership = current.ToHashSet();
            foreach (var removed in bodies.Keys.Where(part => !membership.Contains(part)).ToArray())
            {
                bodies.Remove(removed); rotationLocked.Remove(removed); contacts.Remove(removed); expectedStates.Remove(removed);
            }
            foreach (var added in current)
                if (!bodies.ContainsKey(added)) bodies.Add(added, new(++nextId));
            isHierarchyDirty = false;
        }
        foreach (var (part, body) in bodies)
        {
            var state = Snapshot.Capture(part);
            if (!expectedStates.TryGetValue(part, out var expected) || state != expected) InvalidateContacts(part);
            if (!expectedStates.ContainsKey(part) || state.Frame != expected.Frame || state.Size != expected.Size
                || state.Anchored != expected.Anchored || state.CanTouch != expected.CanTouch || state.Shape != expected.Shape)
                changedTouchGeometry.Add(part);
            expectedStates[part] = state;
            body.Frame = part.CFrame; body.Size = part.Size; body.IsAnchored = part.Anchored;
            body.CanCollide = part.CanCollide; body.Material = part.CustomPhysicalProperties ?? PhysicalProperties.Default;
            body.CanTouch = part.CanTouch;
            body.LinearVelocity = part.AssemblyLinearVelocity; body.AngularVelocity = part.AssemblyAngularVelocity;
            body.Shape = part is Part shaped ? shaped.Shape : PartType.Block;
            body.IsRotationLocked = rotationLocked.Contains(part);
            body.UpdateBounds();
        }
        touches.Synchronize(bodies, changedTouchGeometry);
    }
    private void InvalidateContacts(BasePart part)
    {
        contacts.Remove(part);
        foreach (var found in contacts.Values) found.RemoveAll(contact => contact.Other == part);
    }
    internal void Advance(double now)
    {
        Synchronize();
        var originals = bodies.Keys.ToDictionary(part => part, Snapshot.Capture);
        accumulator += Math.Clamp(now - previousTime, 0, 0.25);
        previousTime = now;
        while (accumulator + 1e-9 >= FixedStep)
        {
            BeforeStep?.Invoke((float)FixedStep);
            var valid = bodies.Where(entry => !entry.Key.IsDestroyed && entry.Key.IsDescendantOf(workspace)
                && IsFinite(entry.Value.Frame) && IsFinite(entry.Value.Size)).Select(entry => entry.Value).OrderBy(body => body.Id).ToArray();
            var result = PhysicsSolver.Step(valid, (float)workspace.Gravity, (float)FixedStep, touches.Capture);
            CaptureContacts(result);
            accumulator = Math.Max(0, accumulator - FixedStep);
        }
        foreach (var (part, body) in bodies.ToArray())
        {
            if (part.IsDestroyed || part.Anchored || !part.IsDescendantOf(workspace) || !IsFinite(body.Frame)) continue;
            if (Snapshot.Capture(part) != originals[part]) continue;
            expectedStates[part] = originals[part] with { Frame = body.Frame, Linear = body.LinearVelocity, Angular = body.AngularVelocity };
            part.ApplyPhysicsState(body.Frame, body.LinearVelocity, body.AngularVelocity);
        }
        touches.Publish();
    }
    private void CaptureContacts(IReadOnlyList<BodyContact> result)
    {
        contacts.Clear();
        var parts = bodies.ToDictionary(entry => entry.Value, entry => entry.Key);
        foreach (var contact in result)
        {
            var a = parts[contact.A]; var b = parts[contact.B];
            if (!contacts.ContainsKey(a)) contacts[a] = [];
            if (!contacts.ContainsKey(b)) contacts[b] = [];
            contacts[a].Add(new(b, contact.Point, contact.Normal * -1, contact.Separation));
            contacts[b].Add(new(a, contact.Point, contact.Normal, contact.Separation));
        }
    }
    private readonly record struct Snapshot(CFrame Frame, Vector3 Size, Vector3 Linear, Vector3 Angular, bool Anchored, bool CanCollide, PhysicalProperties? Material, PartType Shape, bool CanTouch)
    {
        internal static Snapshot Capture(BasePart part) => new(part.CFrame, part.Size, part.AssemblyLinearVelocity,
            part.AssemblyAngularVelocity, part.Anchored, part.CanCollide, part.CustomPhysicalProperties, part is Part shaped ? shaped.Shape : PartType.Block, part.CanTouch);
    }
    private static bool IsFinite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    private static bool IsFinite(CFrame value) => IsFinite(value.Position) && IsFinite(value.RightVector) && IsFinite(value.UpVector) && IsFinite(value.ZVector);
    public void Dispose() { workspace.DescendantMutated -= OnMutation; touches.Dispose(); }
}
