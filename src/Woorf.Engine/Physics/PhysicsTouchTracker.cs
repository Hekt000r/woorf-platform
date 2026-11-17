using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Physics;

internal sealed class PhysicsTouchTracker(Workspace workspace) : IDisposable
{
    private Dictionary<RigidBody, BasePart> parts = [];
    private HashSet<Pair> pairs = [], suppressed = [];
    private readonly List<(Pair Pair, bool IsBeginning)> pending = [];

    internal void Synchronize(IReadOnlyDictionary<BasePart, RigidBody> bodies, HashSet<BasePart> changed)
    {
        foreach (var removed in parts.Values.Except(bodies.Keys)) removed.SetTouchingParts([]);
        parts = bodies.ToDictionary(entry => entry.Value, entry => entry.Key);
        var current = FindPairs(bodies.Values.ToArray());
        pairs.RemoveWhere(pair => !IsPresent(pair) || Affects(pair, changed) && !current.Contains(pair));
        suppressed.RemoveWhere(pair => !IsPresent(pair) || Affects(pair, changed) && !current.Contains(pair));
        foreach (var pair in current.Where(pair => Affects(pair, changed)))
        {
            if (pairs.Add(pair)) suppressed.Add(pair);
        }
    }

    internal void Capture(IReadOnlyList<RigidBody> bodies, IReadOnlySet<RigidBody> moved)
    {
        var current = FindPairs(bodies);
        var movingParts = moved.Select(body => parts[body]).ToHashSet();
        foreach (var pair in pairs.Except(current))
            if (!suppressed.Contains(pair)) pending.Add((pair, false));
        foreach (var pair in current)
        {
            if (!pairs.Contains(pair) || suppressed.Contains(pair) && Affects(pair, movingParts))
            {
                pending.Add((pair, true));
                suppressed.Remove(pair);
            }
        }
        suppressed.IntersectWith(current);
        pairs = current;
    }

    internal void Publish()
    {
        var snapshots = parts.ToDictionary(entry => entry.Value, entry =>
            new TouchSnapshot(entry.Key.Frame, entry.Key.Size, entry.Value.Parent, entry.Key.IsAnchored, entry.Key.CanTouch));
        var current = pairs.Where(pair => IsCurrent(pair, snapshots)).ToArray();
        foreach (var part in parts.Values)
            part.SetTouchingParts(current.Where(pair => pair.A == part || pair.B == part)
                .Select(pair => pair.A == part ? pair.B : pair.A));
        var notifications = pending.ToArray();
        pending.Clear();
        foreach (var (pair, isBeginning) in notifications)
        {
            if (!CanDispatch(pair, snapshots)) continue;
            (isBeginning ? pair.A.Touched : pair.A.TouchEnded).Fire(pair.B);
            if (!CanDispatch(pair, snapshots)) continue;
            (isBeginning ? pair.B.Touched : pair.B.TouchEnded).Fire(pair.A);
        }
    }

    private HashSet<Pair> FindPairs(IReadOnlyList<RigidBody> bodies)
    {
        var current = new HashSet<Pair>();
        var sorted = bodies.OrderBy(body => body.Minimum.X).ThenBy(body => body.Id).ToArray();
        for (var index = 0; index < sorted.Length; index++)
        {
            var a = sorted[index];
            for (var other = index + 1; other < sorted.Length && sorted[other].Minimum.X <= a.Maximum.X + BoxCollision.Skin; other++)
            {
                var b = sorted[other];
                if (a.IsAnchored && b.IsAnchored || !(a.CanTouch && b.CanTouch || a.CanCollide && b.CanCollide)) continue;
                if (a.Minimum.Y > b.Maximum.Y + BoxCollision.Skin || b.Minimum.Y > a.Maximum.Y + BoxCollision.Skin
                    || a.Minimum.Z > b.Maximum.Z + BoxCollision.Skin || b.Minimum.Z > a.Maximum.Z + BoxCollision.Skin) continue;
                if (!ShapeCollision.Find(a, b, out _, out _, out _)) continue;
                current.Add(a.Id < b.Id ? new(parts[a], parts[b]) : new(parts[b], parts[a]));
            }
        }
        return current;
    }

    private bool CanDispatch(Pair pair, IReadOnlyDictionary<BasePart, TouchSnapshot> snapshots) =>
        pair.A.CanTouch && pair.B.CanTouch && IsCurrent(pair, snapshots);

    private bool IsCurrent(Pair pair, IReadOnlyDictionary<BasePart, TouchSnapshot> snapshots) => IsPresent(pair)
        && snapshots.TryGetValue(pair.A, out var a) && TouchSnapshot.Capture(pair.A) == a
        && snapshots.TryGetValue(pair.B, out var b) && TouchSnapshot.Capture(pair.B) == b;

    private bool IsPresent(Pair pair) => !pair.A.IsDestroyed && !pair.B.IsDestroyed
        && pair.A.IsDescendantOf(workspace) && pair.B.IsDescendantOf(workspace);
    private static bool Affects(Pair pair, IReadOnlySet<BasePart> changed) => changed.Contains(pair.A) || changed.Contains(pair.B);

    public void Dispose()
    {
        foreach (var part in parts.Values) part.SetTouchingParts([]);
        parts.Clear(); pairs.Clear(); suppressed.Clear(); pending.Clear();
    }

    private readonly record struct Pair(BasePart A, BasePart B);
    private readonly record struct TouchSnapshot(CFrame Frame, Vector3 Size, Instance? Parent, bool Anchored, bool CanTouch)
    {
        internal static TouchSnapshot Capture(BasePart part) => new(part.CFrame, part.Size, part.Parent, part.Anchored, part.CanTouch);
    }
}
