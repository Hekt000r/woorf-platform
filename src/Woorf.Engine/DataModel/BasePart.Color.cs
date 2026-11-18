using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

public abstract partial class BasePart
{
    internal object? ColorOverride { get; private set; }

    public Color3 Color
    {
        get => ColorOverride switch
        {
            Color3 color => color,
            BrickColor brick => brick.Color,
            _ => GetAttribute("RenderColor") is Color3 legacy ? legacy : default(BrickColor).Color
        };
        set => SetColorOverride(value);
    }

    public BrickColor BrickColor
    {
        get => ColorOverride is BrickColor brick ? brick : BrickColor.Closest(Color);
        set => SetColorOverride(value);
    }

    internal void SetColorOverride(object? value)
    {
        if (Equals(ColorOverride, value)) return;

        var previousBrick = BrickColor;
        ColorOverride = value;
        FirePropertyChanged(nameof(Color));
        if (previousBrick != BrickColor) FirePropertyChanged(nameof(BrickColor));
    }
}
