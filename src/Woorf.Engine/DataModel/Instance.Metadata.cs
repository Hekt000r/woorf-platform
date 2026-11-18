using System.Text.RegularExpressions;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public partial class Instance
{
    private readonly Dictionary<string, object> attributes = [];
    private readonly Dictionary<string, ScriptSignal> attributeSignals = [];
    private readonly HashSet<string> tags = [];
    public ScriptSignal AttributeChanged { get; } = new();

    public object? GetAttribute(string name) => attributes.GetValueOrDefault(name);
    public IReadOnlyDictionary<string, object> GetAttributes() => new Dictionary<string, object>(attributes);
    public void SetAttribute(string name, object? value)
    {
        if (!Regex.IsMatch(name, "^[A-Za-z0-9_]+$") || name.Length > 100 || name.StartsWith("RBX", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid attribute name.");
        if (value is not null && value is not (string or bool or double or Vector3 or CFrame or Color3 or Vector2 or UDim or UDim2))
            throw new InvalidOperationException("Unsupported attribute value.");
        if (Equals(GetAttribute(name), value)) return;
        if (value is null) attributes.Remove(name); else attributes[name] = value;
        NotifyMutation("Attributes");
        AttributeChanged.Fire(name);
        if (attributeSignals.TryGetValue(name, out var signal)) signal.Fire();
    }

    public ScriptSignal GetAttributeChangedSignal(string name)
    {
        if (!attributeSignals.TryGetValue(name, out var signal))
        {
            signal = new ScriptSignal();
            if (IsDestroyed) signal.Close();
            attributeSignals[name] = signal;
        }
        return signal;
    }

    public void AddTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (tags.Add(tag)) NotifyMutation("Tags");
    }

    public void RemoveTag(string tag)
    {
        if (tags.Remove(tag)) NotifyMutation("Tags");
    }

    public bool HasTag(string tag) => tags.Contains(tag);
    public string[] GetTags() => tags.ToArray();

    private void CopyMetadataTo(Instance clone)
    {
        foreach (var entry in attributes) clone.attributes.Add(entry.Key, entry.Value);
        clone.tags.UnionWith(tags);
    }

    private void CloseMetadataSignals()
    {
        AttributeChanged.Close();
        foreach (var signal in attributeSignals.Values) signal.Close();
    }
}
