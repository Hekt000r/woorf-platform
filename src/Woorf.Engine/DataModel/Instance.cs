using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public class Instance
{
    private readonly List<Instance> children = [];
    private Instance? parent;
    private string name;
    private bool isArchivable = true;
    private bool isDestroying;
    private bool isChangingParent;
    private int parentVersion;
    private readonly Dictionary<string, ScriptSignal> signals = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ScriptSignal> propertySignals = new(StringComparer.Ordinal);

    internal Instance(string className) { ClassName = className; name = className; }

    public string ClassName { get; }
    internal string ScriptSource { get; set; } = "";
    public string Name
    {
        get => name;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (name == value) return;
            name = value;
            FirePropertyChanged("Name");
        }
    }
    public bool Archivable
    {
        get => isArchivable;
        set
        {
            if (isArchivable == value) return;
            isArchivable = value;
            FirePropertyChanged("Archivable");
        }
    }
    public bool IsDestroyed { get; private set; }
    public Instance? Parent
    {
        get => parent;
        set => SetParent(value);
    }

    public static Instance Create(string className)
    {
        if (!ClassCatalog.IsCreatable(className))
            throw new InvalidOperationException($"Cannot create unsupported or non-creatable class '{className}'.");
        return new Instance(className);
    }

    public bool IsA(string className) => ClassCatalog.IsA(ClassName, className);
    public IReadOnlyList<Instance> GetChildren() => children.ToArray();

    public IEnumerable<Instance> GetDescendants()
    {
        var pending = new Stack<Instance>(children.AsEnumerable().Reverse());
        while (pending.TryPop(out var child))
        {
            yield return child;
            foreach (var descendant in child.children.AsEnumerable().Reverse()) pending.Push(descendant);
        }
    }

    public IEnumerable<Instance> GetAncestors()
    {
        for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent) yield return ancestor;
    }

    public bool IsDescendantOf(Instance ancestor) => GetAncestors().Contains(ancestor);

    public Instance? FindFirstChild(string name, bool isRecursive = false) =>
        (isRecursive ? GetDescendants() : GetChildren()).FirstOrDefault(child => child.Name == name);

    public string GetFullName()
    {
        var names = GetAncestors().Reverse().Where(ancestor => ancestor.ClassName != "DataModel")
            .Select(ancestor => ancestor.Name).Append(Name);
        return string.Join('.', names);
    }

    public void Destroy()
    {
        if (IsDestroyed || isDestroying) return;
        ValidateParent(null);
        var subtree = GetDescendants().Prepend(this).ToArray();
        foreach (var instance in subtree)
        {
            if (instance.isChangingParent)
                throw new InvalidOperationException("Cannot destroy an Instance while its Parent is changing.");
        }
        foreach (var instance in subtree) instance.isDestroying = true;
        foreach (var instance in subtree) instance.FireSignal("Destroying");
        foreach (var instance in subtree)
        {
            instance.SetParent(null, true);
            instance.IsDestroyed = true;
            instance.CloseSignals();
        }
    }

    public void ClearAllChildren()
    {
        foreach (var child in children.ToArray()) child.Destroy();
    }

    public Instance? Clone()
    {
        if (!Archivable) return null;
        var clone = Create(ClassName);
        clone.Name = Name;
        clone.ScriptSource = ScriptSource;
        foreach (var child in children)
        {
            var copiedChild = child.Clone();
            if (copiedChild is not null) copiedChild.Parent = clone;
        }
        return clone;
    }

    public static bool IsSignalName(string name) => name is "Changed" or "ChildAdded" or "ChildRemoved"
        or "AncestryChanged" or "Destroying" or "DescendantAdded" or "DescendantRemoving";

    public ScriptSignal GetSignal(string name)
    {
        if (!IsSignalName(name)) throw new InvalidOperationException($"Unsupported signal '{name}'.");
        return GetOrCreateSignal(signals, name);
    }

    public ScriptSignal GetPropertyChangedSignal(string property)
    {
        var isProperty = property is "Name" or "Parent" or "Archivable" or "ClassName"
            || ClassName == "DataModel" && property == "Workspace";
        if (!isProperty) throw new InvalidOperationException($"Unknown property '{property}'.");
        return GetOrCreateSignal(propertySignals, property);
    }

    private ScriptSignal GetOrCreateSignal(Dictionary<string, ScriptSignal> source, string key)
    {
        if (source.TryGetValue(key, out var signal)) return signal;
        signal = new ScriptSignal();
        if (IsDestroyed) signal.Close();
        source.Add(key, signal);
        return signal;
    }

    private void FirePropertyChanged(string property)
    {
        FireSignal("Changed", property);
        if (propertySignals.TryGetValue(property, out var signal)) signal.Fire();
    }

    private void FireSignal(string signalName, params object?[] arguments)
    {
        if (signals.TryGetValue(signalName, out var signal)) signal.Fire(arguments);
    }

    private void CloseSignals()
    {
        foreach (var signal in signals.Values) signal.Close();
        foreach (var signal in propertySignals.Values) signal.Close();
    }

    private void ValidateParent(Instance? value, bool canDestroy = false)
    {
        if (ClassName == "DataModel" || IsDestroyed || isDestroying && !canDestroy || isChangingParent)
            throw new InvalidOperationException("Parent is locked.");
        if (ClassCatalog.IsService(ClassName) && parent is not null && value != parent)
            throw new InvalidOperationException("Service Parent is locked to its DataModel.");
        if (value?.IsDestroyed == true || value?.isDestroying == true)
            throw new InvalidOperationException("Cannot parent to a destroyed Instance.");
        if (value == this || value?.IsDescendantOf(this) == true)
            throw new InvalidOperationException("Parent would create a hierarchy cycle.");
    }

    private void SetParent(Instance? value, bool canDestroy = false)
    {
        ValidateParent(value, canDestroy);
        if (parent == value) return;
        var previous = parent;
        var subtree = GetDescendants().Prepend(this).ToArray();
        if (subtree.Any(instance => instance.isChangingParent))
            throw new InvalidOperationException("Cannot change an overlapping hierarchy during removal.");
        var previousAncestors = GetAncestors().ToArray();
        var nextAncestors = value is null ? [] : value.GetAncestors().Prepend(value).ToArray();
        foreach (var instance in subtree) instance.isChangingParent = true;
        try
        {
            FireDescendantChanges(previousAncestors.Except(nextAncestors), subtree, "DescendantRemoving");
        }
        finally
        {
            foreach (var instance in subtree) instance.isChangingParent = false;
        }
        ValidateParent(value, canDestroy);
        previous?.children.Remove(this);
        parent = value;
        parent?.children.Add(this);
        var version = ++parentVersion;
        previous?.FireSignal("ChildRemoved", this);
        if (parentVersion != version) return;
        FirePropertyChanged("Parent");
        if (parentVersion != version) return;
        foreach (var instance in subtree)
        {
            if (instance == this || instance.IsDescendantOf(this))
                instance.FireSignal("AncestryChanged", this, value);
            if (parentVersion != version) return;
        }
        value?.FireSignal("ChildAdded", this);
        if (parentVersion != version) return;
        FireDescendantChanges(nextAncestors.Except(previousAncestors), subtree, "DescendantAdded");
    }

    private static void FireDescendantChanges(IEnumerable<Instance> ancestors, Instance[] subtree, string signalName)
    {
        foreach (var ancestor in ancestors)
        {
            foreach (var instance in subtree)
            {
                if (signalName == "DescendantAdded" && !instance.IsDescendantOf(ancestor)) continue;
                ancestor.FireSignal(signalName, instance);
            }
        }
    }
}
