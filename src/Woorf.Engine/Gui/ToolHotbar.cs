using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed class ToolHotbar(Player player, Action<Tool> toggle, Action<bool> use)
{
    private readonly List<Tool> tools = [];
    private GuiRect[] slots = [];
    private int pressedSlot = -1;
    private bool wasDown, isUsing;

    public bool KeyDown(string key)
    {
        if (key.Length != 1 || key[0] is < '0' or > '9') return false;
        Refresh();
        var index = key[0] == '0' ? 9 : key[0] - '1';
        if (index >= tools.Count) return false;
        toggle(tools[index]);
        return true;
    }

    public bool Hit(float x, float y) => Array.Exists(slots, slot => slot.Contains(x, y));

    public void Pointer(float x, float y, bool isDown, bool isBlocked)
    {
        var slot = Array.FindIndex(slots, bounds => bounds.Contains(x, y));
        if (isDown && !wasDown)
        {
            pressedSlot = isBlocked ? -1 : slot;
            if (slot < 0 && !isBlocked && player.Character?.GetChildren().OfType<Tool>().Any() == true)
            {
                isUsing = true;
                use(true);
            }
        }
        if ((!isDown || isBlocked) && isUsing)
        {
            isUsing = false;
            use(false);
        }
        if (!isDown && wasDown && !isBlocked && slot >= 0 && slot == pressedSlot && slot < tools.Count)
            toggle(tools[slot]);
        wasDown = isDown;
    }

    public IEnumerable<GuiDrawCommand> Capture(int width, int height, bool isVisible)
    {
        Refresh();
        var count = isVisible ? Math.Min(tools.Count, 10) : 0;
        if (count != slots.Length) slots = new GuiRect[count];
        var viewport = new GuiRect(0, 0, width, height);
        if (count == 0) yield break;
        var stride = Math.Min(64f, width / (float)count);
        for (var index = 0; index < count; index++)
        {
            var tool = tools[index];
            var bounds = new GuiRect((width - stride * count) / 2 + index * stride + 2, height - 55, stride - 4, 42);
            slots[index] = bounds;
            var equipped = tool.Parent == player.Character;
            var color = equipped ? new Color3(.08f, .38f, .43f) : new Color3(.1f, .15f, .2f);
            yield return new("Rectangle", bounds, viewport, color, .95f, Radius: 5);
            yield return new("Text", bounds, bounds.Intersect(viewport), new Color3(1, 1, 1), 1,
                $"{(index + 1) % 10}  {tool.Name}", 12, Wrapped: true);
        }
    }

    private void Refresh()
    {
        var backpack = player.FindFirstChild("Backpack");
        var character = player.Character;
        tools.RemoveAll(tool => tool.IsDestroyed || tool.Parent is null ||
            tool.Parent != backpack && tool.Parent != character);
        foreach (var tool in (backpack?.GetChildren() ?? []).Concat(character?.GetChildren() ?? []).OfType<Tool>())
            if (!tools.Contains(tool)) tools.Add(tool);
    }
}
