using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal sealed record ReplicatedGuiState(IReadOnlyDictionary<string, object> Properties)
{
    internal static ReplicatedGuiState? Capture(Instance instance) => instance is GuiInstance gui
        ? new(gui.GetGuiProperties().Where(entry => !IsComputed(entry.Key)).ToDictionary()) : null;

    private static bool IsComputed(string name) => name.StartsWith("Absolute", StringComparison.Ordinal)
        || name is "ContentText" or "TextBounds" or "TextFits";

    internal void Apply(Instance instance, ReplicatedGuiState? previous)
    {
        if (instance is not GuiInstance gui) return;
        foreach (var (name, value) in Properties)
            if (previous is null || !Equals(previous.Properties.GetValueOrDefault(name), value)) gui.SetGuiProperty(name, value);
    }

    internal Dictionary<string, WireValue> Encode() => Properties.ToDictionary(entry => entry.Key, entry => WireValue.Encode(entry.Value, _ => 0));
    internal static ReplicatedGuiState Decode(Dictionary<string, WireValue> properties) => new(
        properties.ToDictionary(entry => entry.Key, entry => entry.Value.Decode(_ => null)
            ?? throw new InvalidDataException("GUI properties cannot be nil.")));
}
