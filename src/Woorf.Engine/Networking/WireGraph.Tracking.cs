using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal sealed partial class WireGraph : IDisposable
{
    private readonly DataModel.DataModel game;
    private readonly bool isAuthority;
    private readonly Dictionary<Model, BasePart> primaryReferences = [];
    private readonly Dictionary<BasePart, HashSet<Model>> dependents = [];
    private readonly Dictionary<Instance, Instance> backendReferences = [];
    private readonly Dictionary<Instance, HashSet<Instance>> backendDependents = [];
    private readonly HashSet<Instance> dirty = [];
    private readonly Dictionary<int, WireNode> captured = [];
    private readonly HashSet<int> pendingChanges = [];
    private WireNode[] current = [];
    private bool isInitialized;
    private long snapshotRevision = -1;
    internal long CapturedNodes { get; private set; }
    internal long FullScans { get; private set; }
    internal long Revision { get; private set; }

    internal WireGraph(DataModel.DataModel game, bool isAuthority)
    {
        this.game = game;
        this.isAuthority = isAuthority;
        if (isAuthority) game.DescendantMutated += MarkDirty;
    }

    internal WireNode[] Capture()
    {
        UpdateDirty();
        if (snapshotRevision != Revision)
        {
            current = captured.Values.ToArray();
            snapshotRevision = Revision;
        }
        return current;
    }

    internal void UpdateDirty()
    {
        if (!isInitialized)
        {
            FullScans++;
            foreach (var root in game.GetChildren().Where(IsVisible))
                dirty.UnionWith(root.GetDescendants().Prepend(root));
            isInitialized = true;
        }
        if (dirty.Count == 0) return;
        var changed = dirty.ToArray();
        dirty.Clear();
        foreach (var instance in changed)
        {
            if (!instance.IsDestroyed && IsVisible(instance))
            {
                var node = CaptureNode(instance);
                captured[node.Id] = node;
                pendingChanges.Add(node.Id);
            }
            else
            {
                if (instance is Model model) ForgetPrimary(model);
                ForgetBackendReference(instance);
                if (identifiers.TryGetValue(instance, out var id) && captured.Remove(id))
                    pendingChanges.Add(id);
            }
        }
        Revision++;
    }

    internal (WireNode[] Nodes, int[] Removed) GetChanges() => (
        pendingChanges.Where(captured.ContainsKey).Select(id => captured[id]).ToArray(),
        pendingChanges.Where(id => !captured.ContainsKey(id)).ToArray());

    internal void CompleteChanges() => pendingChanges.Clear();

    private void MarkDirty(Instance instance, string property)
    {
        if (property == "Parent")
        {
            foreach (var affected in instance.GetDescendants().Prepend(instance))
            {
                if (IsVisible(affected) || identifiers.ContainsKey(affected)) dirty.Add(affected);
                if (affected is BasePart part && dependents.TryGetValue(part, out var models))
                    dirty.UnionWith(models);
                if (backendDependents.TryGetValue(affected, out var holders)) dirty.UnionWith(holders);
            }
        }
        else if (property != "Descendants" && (IsVisible(instance) || identifiers.ContainsKey(instance)))
            dirty.Add(instance);
        if (property is "Parent" or "Descendants" or "CFrame" or "Position" or "Size" or "PivotOffset")
        {
            foreach (var model in instance.GetAncestors().Prepend(instance).OfType<Model>())
                if (model.HasAutomaticWorldPivot && IsVisible(model)) dirty.Add(model);
        }
    }
    private void TrackPrimary(Model model)
    {
        if (primaryReferences.GetValueOrDefault(model) == model.PrimaryPart) return;
        ForgetPrimary(model);
        if (model.PrimaryPart is not BasePart part) return;
        primaryReferences[model] = part;
        if (!dependents.TryGetValue(part, out var models)) dependents[part] = models = [];
        models.Add(model);
    }

    private void ForgetPrimary(Model model)
    {
        if (!primaryReferences.Remove(model, out var part)) return;
        if (dependents.TryGetValue(part, out var models))
        {
            models.Remove(model);
            if (models.Count == 0) dependents.Remove(part);
        }
    }

    private void TrackBackendReference(Instance instance)
    {
        var reference = instance switch { Player player => player.Team, ValueObject value => value.Value as Instance, _ => null };
        if (backendReferences.GetValueOrDefault(instance) == reference) return;
        ForgetBackendReference(instance);
        if (reference is null) return;
        backendReferences[instance] = reference;
        if (!backendDependents.TryGetValue(reference, out var holders)) backendDependents[reference] = holders = [];
        holders.Add(instance);
    }

    private void ForgetBackendReference(Instance instance)
    {
        if (!backendReferences.Remove(instance, out var reference)) return;
        if (!backendDependents.TryGetValue(reference, out var holders)) return;
        holders.Remove(instance);
        if (holders.Count == 0) backendDependents.Remove(reference);
    }

    public void Dispose()
    {
        if (isAuthority) game.DescendantMutated -= MarkDirty;
    }

}
