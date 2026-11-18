using Woorf.Engine.Datatypes;
using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public sealed partial class GuiInstance : Instance
{
    private readonly Dictionary<string, object> properties;
    private readonly Dictionary<string, ScriptSignal> guiSignals = [];
    private bool isFocused;
    private static readonly HashSet<string> Computed = ["AbsolutePosition", "AbsoluteSize", "AbsoluteRotation", "AbsoluteContentSize", "AbsoluteCanvasSize", "AbsoluteWindowSize", "TextBounds", "TextFits", "ContentText"];

    internal GuiInstance(string className) : base(className)
    {
        properties = GuiDefaults.Create(className);
        GetSignal("Destroying").Connect(_ => { foreach (var signal in guiSignals.Values) signal.Close(); });
    }

    public bool HasGuiProperty(string property) => properties.ContainsKey(property);
    public object GetGuiProperty(string property) => properties.TryGetValue(property, out var value)
        ? value : throw new InvalidOperationException($"Unknown {ClassName} property '{property}'.");
    public T Get<T>(string property) => (T)GetGuiProperty(property);
    public IReadOnlyDictionary<string, object> GetGuiProperties() => new Dictionary<string, object>(properties);

    public void SetGuiProperty(string property, object value)
    {
        if (Computed.Contains(property)) throw new InvalidOperationException($"{property} is read-only.");
        var previous = GetGuiProperty(property);
        var next = GuiDefaults.Validate(property, previous, value);
        Assign(property, next);
        if (property == "Text") Assign("ContentText", next);
    }

    public void SetComputedProperty(string property, object value)
    {
        if (!Computed.Contains(property)) throw new InvalidOperationException($"{property} is not computed.");
        if (!HasGuiProperty(property)) return;
        Assign(property, GuiDefaults.Validate(property, properties[property], value));
    }

    private void Assign(string property, object next)
    {
        if (Equals(properties[property], next)) return;
        properties[property] = next;
        FirePropertyChanged(property);
    }

    public bool HasGuiSignal(string name) => IsA("GuiObject") && name is "MouseEnter" or "MouseLeave" or "MouseMoved" or "InputBegan" or "InputChanged" or "InputEnded"
        || IsA("GuiButton") && name is "Activated" or "MouseButton1Click" or "MouseButton1Down" or "MouseButton1Up" or "MouseButton2Click" or "MouseButton2Down" or "MouseButton2Up"
        || ClassName == "TextBox" && name is "Focused" or "FocusLost";

    public ScriptSignal GetGuiSignal(string name)
    {
        if (!HasGuiSignal(name)) throw new InvalidOperationException($"Unsupported {ClassName} signal '{name}'.");
        if (!guiSignals.TryGetValue(name, out var signal))
        {
            guiSignals[name] = signal = new ScriptSignal();
            if (IsDestroyed) signal.Close();
        }
        return signal;
    }

    public void FireGuiSignal(string name, params object?[] arguments)
    {
        if (!IsDestroyed && guiSignals.TryGetValue(name, out var signal)) signal.Fire(arguments);
    }

    public bool IsFocused => isFocused;
    public void SetFocused(bool value, bool submitted = false)
    {
        if (ClassName != "TextBox") throw new InvalidOperationException("Expected TextBox.");
        if (IsDestroyed || isFocused == value) return;
        isFocused = value;
        if (value && Get<bool>("ClearTextOnFocus")) SetGuiProperty("Text", "");
        if (value) FireGuiSignal("Focused"); else FireGuiSignal("FocusLost", submitted, null);
    }

    protected override bool HasProperty(string property) => HasGuiProperty(property) || base.HasProperty(property);
    protected override void CopyPropertiesTo(Instance clone)
    {
        var target = (GuiInstance)clone;
        foreach (var (property, value) in properties)
            if (!Computed.Contains(property)) target.properties[property] = value;
        if (target.HasGuiProperty("ContentText")) target.properties["ContentText"] = target.properties["Text"];
    }
}
