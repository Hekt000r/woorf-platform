using Woorf.Engine.Datatypes;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    internal int CallBrickColor(nint state, string operation) => Push(state, operation switch
    {
        "brickcolor_new" => NewBrickColor(state),
        "brickcolor_palette" => BrickColor.FromPalette(ToColorIndex(Number(state, 1))),
        "brickcolor_random" => BrickColor.Random(),
        "brickcolor_White" => new BrickColor(1),
        "brickcolor_Gray" => new BrickColor(194),
        "brickcolor_DarkGray" => new BrickColor(199),
        "brickcolor_Black" => new BrickColor(26),
        "brickcolor_Red" => new BrickColor(21),
        "brickcolor_Yellow" => new BrickColor(24),
        "brickcolor_Green" => new BrickColor(28),
        "brickcolor_Blue" => new BrickColor(23),
        "brickcolor_get" => ReadBrickColorMember(state),
        _ => throw new InvalidOperationException("Unsupported BrickColor constructor.")
    });

    private BrickColor NewBrickColor(nint state)
    {
        var count = woorf_get_top(state);
        if (count == 0) return default;
        if (count == 3) return BrickColor.Closest(new((float)Number(state, 1), (float)Number(state, 2), (float)Number(state, 3)));
        if (count != 1) throw new ArgumentException("Expected a BrickColor name, number, Color3, or three RGB components.");
        return Read(state, 1) switch
        {
            double value => new BrickColor(ToColorIndex(value)),
            string name => new BrickColor(name),
            Color3 color => BrickColor.Closest(color),
            _ => throw new ArgumentException("Expected a BrickColor name, number, or Color3.")
        };
    }

    private static int ToColorIndex(double value) => double.IsFinite(value) && value >= int.MinValue && value <= int.MaxValue
        && value == Math.Truncate(value) ? (int)value : int.MinValue;

    private object ReadBrickColorMember(nint state)
    {
        var value = Read(state, 1) is BrickColor color ? color : throw new ArgumentException("Expected BrickColor.");
        return Text(state, 2) switch
        {
            "Number" => (double)value.Number, "Name" => value.Name, "Color" => value.Color,
            "r" or "R" => (double)value.Color.R, "g" or "G" => (double)value.Color.G, "b" or "B" => (double)value.Color.B,
            _ => throw new InvalidOperationException("Unsupported BrickColor member.")
        };
    }
}
