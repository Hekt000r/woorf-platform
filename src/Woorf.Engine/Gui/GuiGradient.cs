using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed record GuiGradientStop(float Time, Color3 Color, float Opacity);

public sealed record GuiGradient(Vector2 Start, Vector2 End, GuiGradientStop[] Stops)
{
    internal static GuiGradient Create(GuiInstance gradient, GuiRect bounds)
    {
        var colors = ((SequenceValue)gradient.GetGuiProperty("Color")!).Components;
        var transparency = ((SequenceValue)gradient.GetGuiProperty("Transparency")!).Components;
        var colorTimes = colors.Where((_, index) => index % 4 == 0).ToLookup(time => time);
        var alphaTimes = transparency.Where((_, index) => index % 3 == 0).ToLookup(time => time);
        var stops = new List<GuiGradientStop>();
        foreach (var time in colorTimes.Select(group => group.Key).Union(alphaTimes.Select(group => group.Key)).Order())
        {
            if (colorTimes[time].Count() > 1 || alphaTimes[time].Count() > 1)
                stops.Add(CreateStop(colors, transparency, time, true));
            stops.Add(CreateStop(colors, transparency, time, false));
        }

        var angle = (float)((double)gradient.GetGuiProperty("Rotation")! % 360) * MathF.PI / 180;
        var offset = (Vector2)gradient.GetGuiProperty("Offset")!;
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var extent = (MathF.Abs(direction.X) * bounds.Width + MathF.Abs(direction.Y) * bounds.Height) / 2;
        var x = bounds.Width * (0.5f + offset.X);
        var y = bounds.Height * (0.5f + offset.Y);
        return new(new(x - direction.X * extent, y - direction.Y * extent),
            new(x + direction.X * extent, y + direction.Y * extent), stops.ToArray());
    }

    private static GuiGradientStop CreateStop(float[] colors, float[] transparency, float time, bool fromLeft) =>
        new(time, new(Sample(colors, 4, time, 1, fromLeft), Sample(colors, 4, time, 2, fromLeft),
            Sample(colors, 4, time, 3, fromLeft)), 1 - Math.Clamp(Sample(transparency, 3, time, 1, fromLeft), 0, 1));

    private static float Sample(float[] sequence, int stride, float time, int channel, bool fromLeft)
    {
        if (time == 0 && fromLeft) return sequence[channel];
        for (var next = stride; next < sequence.Length; next += stride)
        {
            if (fromLeft && time == sequence[next]) return sequence[next + channel];
            if (time >= sequence[next]) continue;
            var previous = next - stride;
            var fraction = (time - sequence[previous]) / (sequence[next] - sequence[previous]);
            return sequence[previous + channel] + (sequence[next + channel] - sequence[previous + channel]) * fraction;
        }
        return sequence[sequence.Length - stride + channel];
    }
}
