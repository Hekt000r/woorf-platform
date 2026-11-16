using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Networking;

internal sealed record WireNode(int Id, string ClassName, string Name, int Parent, bool Archivable,
    int UserId, string Source, float[] Frame, float[] Pivot, float[] Size, bool Anchored,
    float[] WorldPivot, int PrimaryPart, WireBackendState? Backend = null, bool Enabled = true, int RunContext = 0, WirePhysicsState? Physics = null);

internal sealed partial class WireGraph
{
    private readonly Dictionary<Instance, int> identifiers = [];
    private readonly Dictionary<int, Instance> instances = [];
    private readonly Dictionary<int, ReplicaState> snapshots = [];
    private int nextId;

    internal int Identify(Instance instance)
    {
        if (!IsVisible(instance)) return 0;
        if (!isAuthority) return identifiers.GetValueOrDefault(instance);
        if (identifiers.TryGetValue(instance, out var id)) return id;
        id = ++nextId;
        identifiers.Add(instance, id);
        instances.Add(id, instance);
        return id;
    }

    internal Instance? Find(int id) => instances.TryGetValue(id, out var instance)
        && !instance.IsDestroyed && IsVisible(instance) ? instance : null;

    private bool IsVisible(Instance instance)
    {
        var root = instance.GetAncestors().Prepend(instance).FirstOrDefault(item => item.Parent == game);
        return root?.ClassName is "Workspace" or "ReplicatedStorage" or "ReplicatedFirst"
            or "Players" or "StarterPlayer" or "StarterGui" or "Teams";
    }

    private WireNode CaptureNode(Instance instance)
    {
        CapturedNodes++;
        if (instance is Model model) TrackPrimary(model);
        TrackBackendReference(instance);
        var state = ReplicaState.Capture(instance);
        return new WireNode(Identify(instance), instance.ClassName, state.Name,
            instance.Parent == game ? 0 : Identify(instance.Parent!), state.Archivable,
            state.UserId, state.Source,
            state.Frame.GetComponents(), state.PivotOffset.GetComponents(),
            [state.Size.X, state.Size.Y, state.Size.Z], state.Anchored,
            state.WorldPivot.GetComponents(), state.PrimaryPart is null ? 0 : Identify(state.PrimaryPart), state.Backend?.Encode(Identify), state.Enabled, state.RunContext, state.Physics?.Encode());
    }

    internal void Apply(WireNode[] nodes)
    {
        var current = nodes.Select(node => node.Id).ToHashSet();
        ApplyChanges(nodes, instances.Keys.Where(id => !current.Contains(id)).ToArray());
    }

    internal void ApplyChanges(WireNode[] nodes, int[] removed)
    {
        foreach (var node in nodes)
        {
            if (instances.ContainsKey(node.Id)) continue;
            var instance = node.Parent == 0 ? game.GetService(node.ClassName) : Instance.CreateRuntime(node.ClassName);
            instances.Add(node.Id, instance);
            identifiers.Add(instance, node.Id);
            if (instance is Player player) player.UserId = node.UserId;
        }
        foreach (var node in nodes) ApplyNode(node);
        foreach (var id in removed)
        {
            if (!instances.TryGetValue(id, out var instance)) continue;
            if (instance is Player player && !player.IsDestroyed)
                ((Players)game.GetService("Players")).PlayerRemoving.Fire(player);
            if (!instance.IsDestroyed) instance.Destroy();
            identifiers.Remove(instance);
            instances.Remove(id);
            snapshots.Remove(id);
        }
    }

    private void ApplyNode(WireNode node)
    {
        var instance = instances[node.Id];
        var parent = node.Parent == 0 ? game : instances[node.Parent];
        var next = new ReplicaState(node.Name, parent, node.Archivable,
            CFrame.FromComponents(node.Frame), CFrame.FromComponents(node.Pivot),
            new(node.Size[0], node.Size[1], node.Size[2]), node.Anchored,
            CFrame.FromComponents(node.WorldPivot), instances.GetValueOrDefault(node.PrimaryPart) as BasePart,
            node.UserId, node.Source, node.Backend?.Decode(id => instances.GetValueOrDefault(id)), node.Enabled, node.RunContext, node.Physics?.Decode());
        var previous = snapshots.GetValueOrDefault(node.Id);
        if (previous is null || previous.Name != next.Name) instance.Name = next.Name;
        if (previous is null || previous.Archivable != next.Archivable) instance.Archivable = next.Archivable;
        next.ApplyValues(instance, previous, value => value);
        if (instance.ClassName == "LocalScript") instance.ScriptSource = node.Source;
        if (previous is null || previous.Parent != parent) instance.Parent = parent;
        snapshots[node.Id] = next with { Backend = next.Backend?.ResolveReferences(value => value) };
    }
}
