using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal sealed class ReplicaMap(DataModel.DataModel server, DataModel.DataModel client)
{
    private readonly Dictionary<Instance, Instance> replicas = [];
    private readonly Dictionary<Instance, Instance> originals = [];
    private readonly Dictionary<Instance, ReplicaState> snapshots = [];
    private HashSet<Instance> visibleInstances = [];

    internal Instance? ToClient(Instance instance) => visibleInstances.Contains(instance) ? replicas.GetValueOrDefault(instance) : null;
    internal Instance? ToServer(Instance instance) => originals.TryGetValue(instance, out var original) && visibleInstances.Contains(original) ? original : null;

    internal void Synchronize()
    {
        var visible = server.GetChildren().Where(root => root.ClassName is
            "Workspace" or "ReplicatedStorage" or "ReplicatedFirst" or "Players" or "Teams" or "Lighting" or "StarterPack")
            .SelectMany(root => root.GetDescendants().Prepend(root))
            .Where(instance => instance is not Camera && !instance.GetAncestors().Any(ancestor => ancestor is Camera)).ToArray();
        var current = visible.ToHashSet();
        visibleInstances = current;
        foreach (var source in visible) EnsureReplica(source);
        foreach (var source in visible) Apply(source);
        foreach (var source in visible.OfType<Player>())
            snapshots[source].CharacterState?.ApplyReference((Player)replicas[source], ToClient);
        foreach (var source in replicas.Keys.Where(source => !current.Contains(source)).ToArray())
        {
            var replica = replicas[source];
            if (!replica.IsDestroyed) replica.Destroy();
            replicas.Remove(source);
            originals.Remove(replica);
            snapshots.Remove(source);
        }
    }

    private void EnsureReplica(Instance source)
    {
        if (replicas.ContainsKey(source)) return;
        var replica = source.Parent == server ? client.GetService(source.ClassName) : Instance.CreateRuntime(source.ClassName);
        if (source is Player player) ((Player)replica).UserId = player.UserId;
        replicas.Add(source, replica);
        originals.Add(replica, source);
    }

    private void Apply(Instance source)
    {
        var replica = replicas[source];
        if (replica.IsDestroyed) return;
        var next = ReplicaState.Capture(source);
        var previous = snapshots.GetValueOrDefault(source);
        if (previous is null || previous.Name != next.Name) replica.Name = next.Name;
        if (previous is null || previous.Parent != next.Parent)
            replica.Parent = source.Parent == server ? client : replicas.GetValueOrDefault(source.Parent!);
        if (previous is null || previous.Archivable != next.Archivable) replica.Archivable = next.Archivable;
        next.ApplyValues(replica, previous, ToClient);
        snapshots[source] = next with { Backend = next.Backend?.ResolveReferences(ToClient) };
    }
}
