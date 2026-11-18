using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed partial class GuiScene
{
    private readonly List<GuiDrawCommand> commands = [];
    private readonly List<GuiHitTarget> targets = [];
    private readonly List<float> commandLayers = [];
    public IReadOnlyList<GuiDrawCommand> Commands => commands;
    public bool IsTextFocused => focused is { IsDestroyed: false };

    public IReadOnlyList<GuiDrawCommand> Capture(Instance? playerGui, int width, int height)
    {
        commands.Clear(); targets.Clear(); commandLayers.Clear();
        if (playerGui is null) { ReleaseFocus(false); return commands; }
        var viewport = new GuiRect(0, 0, width, height);
        var screens = playerGui.GetDescendants().OfType<GuiInstance>().Where(item => item.ClassName == "ScreenGui")
            .OrderBy(item => Number(item, "DisplayOrder"));
        foreach (var screen in screens)
        {
            if (!Value(screen, "Enabled", true)) continue;
            screen.SetComputedProperty("AbsoluteSize", new Vector2(width, height));
            var commandStart = commands.Count; var targetStart = targets.Count;
            CaptureChildren(screen, viewport, viewport, 1, 1);
            if (EnumName(screen, "ZIndexBehavior", "Sibling") == "Global") SortGlobal(commandStart, targetStart);
        }
        if (focused is not null && !targets.Any(target => target.Instance == focused)) ReleaseFocus(false);
        if (focused is not null && !focused.IsFocused) focused = null;
        var requestedFocus = targets.LastOrDefault(target => target.Instance.ClassName == "TextBox" && target.Instance.IsFocused)?.Instance;
        if (requestedFocus is not null && requestedFocus != focused) Focus(requestedFocus);
        return commands;
    }

    private void CaptureChildren(Instance parent, GuiRect bounds, GuiRect clip, float scale, float opacity)
    {
        var children = parent.GetChildren().OfType<GuiInstance>().ToArray();
        var scaleItem = children.FirstOrDefault(item => item.ClassName == "UIScale");
        if (scaleItem is not null) scale *= Number(scaleItem, "Scale", 1);
        var content = ApplyPadding(children, bounds, scale);
        var layout = children.FirstOrDefault(item => item.ClassName is "UIListLayout" or "UIGridLayout");
        var objects = children.Where(item => item.HasGuiProperty("Visible") && Value(item, "Visible", true)).ToArray();
        var placements = Arrange(objects, layout, content, scale);
        foreach (var child in objects.OrderBy(item => Number(item, "ZIndex", 1)))
        {
            var rectangle = placements[child];
            CaptureObject(child, rectangle, clip, scale, opacity);
        }
    }

    private void CaptureObject(GuiInstance item, GuiRect bounds, GuiRect clip, float scale, float opacity)
    {
        item.SetComputedProperty("AbsolutePosition", new Vector2(bounds.X, bounds.Y));
        item.SetComputedProperty("AbsoluteSize", new Vector2(bounds.Width, bounds.Height));
        var commandStart = commands.Count;
        if (item.ClassName == "CanvasGroup") opacity *= 1 - Number(item, "GroupTransparency");
        targets.Add(new(item, bounds, clip));
        DrawAppearance(item, bounds, clip, opacity, scale);
        while (commandLayers.Count < commands.Count) commandLayers.Add(Number(item, "ZIndex", 1));
        var childClip = Value(item, "ClipsDescendants", false) || item.ClassName == "ScrollingFrame" ? clip.Intersect(bounds) : clip;
        var contentBounds = item.ClassName == "ScrollingFrame" ? MeasureCanvas(item, bounds, scale * GetOwnScale(item)) : bounds;
        CaptureChildren(item, contentBounds, childClip, scale, opacity);
        if (item.ClassName == "ScrollingFrame") DrawScrollBars(item, bounds, clip, opacity);
        while (commandLayers.Count < commands.Count) commandLayers.Add(Number(item, "ZIndex", 1));
        if (item.ClassName == "CanvasGroup") ApplyGroupColor(item, commandStart);
    }

    private void SortGlobal(int commandStart, int targetStart)
    {
        var ordered = Enumerable.Range(commandStart, commands.Count - commandStart).OrderBy(index => commandLayers[index]).Select(index => commands[index]).ToArray();
        for (var index = 0; index < ordered.Length; index++) commands[commandStart + index] = ordered[index];
        var hits = targets.Skip(targetStart).OrderBy(target => Number(target.Instance, "ZIndex", 1)).ToArray();
        targets.RemoveRange(targetStart, targets.Count - targetStart); targets.AddRange(hits);
    }

    private void ApplyGroupColor(GuiInstance item, int start)
    {
        var tint = Value(item, "GroupColor3", new Color3(1, 1, 1));
        for (var index = start; index < commands.Count; index++)
        {
            var command = commands[index];
            commands[index] = command with { Color = new Color3(command.Color.R * tint.R, command.Color.G * tint.G, command.Color.B * tint.B) };
        }
    }

    private static T Value<T>(GuiInstance item, string property, T fallback) =>
        item.HasGuiProperty(property) && item.GetGuiProperty(property) is T value ? value : fallback;
    private static float Number(GuiInstance item, string property, float fallback = 0) =>
        item.HasGuiProperty(property) ? Convert.ToSingle(item.GetGuiProperty(property)) : fallback;
    private static string EnumName(GuiInstance item, string property, string fallback) =>
        item.HasGuiProperty(property) && item.GetGuiProperty(property) is GuiEnum value ? value.Name : fallback;
    private static float Resolve(UDim value, float parent, float scale) => value.Scale * parent + value.Offset * scale;
}
