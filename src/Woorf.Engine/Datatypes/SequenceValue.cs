namespace Woorf.Engine.Datatypes;

internal sealed class SequenceValue(int kind, float[] components) : IEquatable<SequenceValue>
{
    internal int Kind { get; } = kind;
    internal float[] Components { get; } = components;

    internal static SequenceValue White { get; } = new(2, [0, 1, 1, 1, 1, 1, 1, 1]);
    internal static SequenceValue Opaque { get; } = new(3, [0, 0, 0, 1, 0, 0]);

    internal static bool HasValidComponents(double kind, float[]? components)
    {
        if (components is null || kind is not (0 or 1 or 2 or 3)) return false;
        var stride = kind is 0 or 2 ? 4 : 3;
        if (components.Length % stride != 0 || components.Any(value => !float.IsFinite(value))) return false;
        if (kind < 2) return components.Length == stride && components[0] is >= 0 and <= 1;
        if (components.Length < 2 * stride || components.Length > 20 * stride ||
            components[0] != 0 || components[^stride] != 1) return false;
        for (var index = stride; index < components.Length; index += stride)
            if (components[index] < components[index - stride]) return false;
        return true;
    }

    public bool Equals(SequenceValue? other) =>
        other is not null && Kind == other.Kind && Components.AsSpan().SequenceEqual(other.Components);

    public override bool Equals(object? obj) => Equals(obj as SequenceValue);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Kind);
        foreach (var component in Components) hash.Add(component);
        return hash.ToHashCode();
    }
}
