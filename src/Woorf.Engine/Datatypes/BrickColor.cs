namespace Woorf.Engine.Datatypes;

public readonly struct BrickColor : IEquatable<BrickColor>
{
    private const int DefaultNumber = 194;
    private static readonly Dictionary<int, int> ByNumber = BrickColorPalette.Entries
        .Select((entry, index) => (entry.Number, index)).ToDictionary(item => item.Number, item => item.index);
    private static readonly Dictionary<string, int> ByName = BrickColorPalette.Entries
        .GroupBy(entry => entry.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First().Number, StringComparer.Ordinal);
    private static readonly int[] Palette = BrickColorPalette.Entries.Where(entry => entry.Index >= 0)
        .OrderBy(entry => entry.Index).Select(entry => entry.Number).ToArray();
    private readonly int number;

    public BrickColor(int number) => this.number = ByNumber.ContainsKey(number) ? number : DefaultNumber;
    public BrickColor(string name) : this(ByName.GetValueOrDefault(name, DefaultNumber)) { }

    public int Number => number == 0 ? DefaultNumber : number;
    public string Name => Entry.Name;
    public Color3 Color => new(Entry.R / 255f, Entry.G / 255f, Entry.B / 255f);
    private (int Number, string Name, int Index, byte R, byte G, byte B) Entry => BrickColorPalette.Entries[ByNumber[Number]];

    public static BrickColor FromPalette(int index) => index >= 0 && index < Palette.Length
        ? new(Palette[index]) : throw new ArgumentOutOfRangeException(nameof(index), "BrickColor palette index must be between 0 and 127.");

    public static BrickColor Random() => FromPalette(System.Random.Shared.Next(Palette.Length));

    public static BrickColor Closest(Color3 color)
    {
        if (!float.IsFinite(color.R) || !float.IsFinite(color.G) || !float.IsFinite(color.B))
            throw new ArgumentException("BrickColor requires finite RGB components.");
        var best = float.PositiveInfinity;
        var number = DefaultNumber;
        foreach (var entry in BrickColorPalette.Entries)
        {
            var distance = MathF.Abs(color.R - entry.R / 255f) + MathF.Abs(color.G - entry.G / 255f) + MathF.Abs(color.B - entry.B / 255f);
            if (distance >= best) continue;
            best = distance;
            number = entry.Number;
        }
        return new(number);
    }

    public bool Equals(BrickColor other) => Number == other.Number;
    public override bool Equals(object? obj) => obj is BrickColor other && Equals(other);
    public override int GetHashCode() => Number.GetHashCode();
    public static bool operator ==(BrickColor left, BrickColor right) => left.Equals(right);
    public static bool operator !=(BrickColor left, BrickColor right) => !left.Equals(right);
    public override string ToString() => Name;
}
