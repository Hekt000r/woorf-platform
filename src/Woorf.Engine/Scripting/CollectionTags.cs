using Woorf.Engine.DataModel;
using Woorf.Engine.Events;

namespace Woorf.Engine.Scripting;

internal sealed class CollectionTags(DataModel.DataModel game) : IDisposable
{
    private readonly Dictionary<string, ScriptSignal> added = [];
    private readonly Dictionary<string, ScriptSignal> removed = [];
    private readonly Dictionary<string, HashSet<Instance>> members = [];
    internal string[] GetAllTags() => game.GetDescendants().SelectMany(instance => instance.GetTags()).Distinct().ToArray();
    internal void Attach() => game.DescendantMutated += OnMutation;
    internal Instance[] GetTagged(string tag) => game.GetDescendants().Where(instance => instance.HasTag(tag)).ToArray();

    internal ScriptSignal GetSignal(string tag, bool isAdded)
    {
        var signals = isAdded ? added : removed;
        if (!signals.TryGetValue(tag, out var signal))
        {
            signals[tag] = signal = new ScriptSignal();
            members.TryAdd(tag, GetTagged(tag).ToHashSet());
        }
        return signal;
    }

    private void OnMutation(Instance origin, string property)
    {
        if (property is not ("Tags" or "Parent")) return;
        foreach (var instance in origin.GetDescendants().Prepend(origin))
        {
            foreach (var (tag, current) in members.ToArray())
            {
                var shouldContain = !instance.IsDestroyed && instance.IsDescendantOf(game) && instance.HasTag(tag);
                if (shouldContain && current.Add(instance) && added.TryGetValue(tag, out var add)) add.Fire(instance);
                else if (!shouldContain && current.Remove(instance) && removed.TryGetValue(tag, out var remove)) remove.Fire(instance);
            }
        }
    }

    public void Dispose()
    {
        game.DescendantMutated -= OnMutation;
        foreach (var signal in added.Values.Concat(removed.Values)) signal.Close();
    }
}
