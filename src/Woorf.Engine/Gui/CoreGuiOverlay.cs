using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed class CoreGuiOverlay
{
    private static readonly Color3 Background = Rgb(22, 37, 49);
    private static readonly Color3 Foreground = Rgb(227, 244, 245);
    private static Color3 Rgb(float r, float g, float b) => new(r / 255, g / 255, b / 255);
    private bool isPlayerListVisible = true;
    private bool isNetworkVisible;
    private bool isFrameVisible;
    private bool isConsoleVisible;
    private GuiRect resume;
    private bool isResumePressed;
    private bool wasPointerDown;

    private bool isMenuOpen;
    public Woorf.Engine.Events.ScriptSignal MenuOpened { get; } = new();
    public Woorf.Engine.Events.ScriptSignal MenuClosed { get; } = new();
    public bool MenuIsOpen
    {
        get => isMenuOpen;
        private set
        {
            if (isMenuOpen == value) return;
            isMenuOpen = value;
            (value ? MenuOpened : MenuClosed).Fire();
        }
    }
    public bool IsPlayerListEnabled { get; set; } = true;
    public bool IsConsoleVisible { get => isConsoleVisible; set => isConsoleVisible = value; }

    public bool KeyDown(string key, bool shift)
    {
        switch (key)
        {
            case "Escape": MenuIsOpen = !MenuIsOpen; isResumePressed = false; return true;
            case "Tab": isPlayerListVisible = !isPlayerListVisible; return true;
            case "F3" when shift: isNetworkVisible = !isNetworkVisible; return true;
            case "F4" when shift: isFrameVisible = !isFrameVisible; return true;
            case "F9": isConsoleVisible = !isConsoleVisible; return true;
            default: return false;
        }
    }

    public bool Pointer(float x, float y, bool isDown)
    {
        var isPress = isDown && !wasPointerDown;
        wasPointerDown = isDown;
        if (!MenuIsOpen) return false;
        var isInside = resume.Contains(x, y);
        if (isPress) isResumePressed = isInside;
        if (!isDown)
        {
            if (isResumePressed && isInside) MenuIsOpen = false;
            isResumePressed = false;
        }
        return true;
    }

    public IReadOnlyList<GuiDrawCommand> Capture(IReadOnlyList<Player> players, int width, int height,
        double elapsed = 0, string transport = "Local", IReadOnlyCollection<string>? logs = null)
    {
        var commands = new List<GuiDrawCommand>();
        var viewport = new GuiRect(0, 0, width, height);
        if (IsPlayerListEnabled && isPlayerListVisible)
            DrawPlayerList(commands, viewport, players);
        var y = 12;
        if (isNetworkVisible)
        {
            DrawPanel(commands, viewport, 12, y, 260, ["Network", $"Transport  {transport}",
                $"Session players  {players.Count}", "Data ping  unavailable"]);
            y += 112;
        }
        if (isFrameVisible)
        {
            var frame = elapsed > 0 ? $"Frame  {elapsed * 1000:F1} ms / {1 / elapsed:F0} FPS" : "Frame  waiting for samples";
            DrawPanel(commands, viewport, 12, y, 260, ["Performance", frame,
                $"Viewport  {width} × {height}", $"Managed heap  {GC.GetTotalMemory(false) / 1048576d:F1} MiB"]);
        }
        if (isConsoleVisible)
        {
            var capacity = Math.Max(0, (height / 2 - 32) / 23);
            var lines = new[] { "Developer Console · Client output" }
                .Concat(logs?.TakeLast(capacity) ?? []).ToArray();
            DrawPanel(commands, viewport, 12, height / 2, Math.Max(0, width - 24), lines);
        }
        if (MenuIsOpen) DrawMenu(commands, viewport, players.Count);
        return commands;
    }

    private static void DrawPlayerList(List<GuiDrawCommand> commands, GuiRect viewport, IReadOnlyList<Player> players)
    {
        var rows = players.OrderBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
            .Select(player => (player.Name, Stats: GetStats(player).ToArray())).ToArray();
        var columns = rows.SelectMany(row => row.Stats)
            .OrderByDescending(stat => (stat.FindFirstChild("IsPrimary") as ValueObject)?.Value is true)
            .ThenByDescending(stat => (stat.FindFirstChild("Priority") as ValueObject)?.Value as double? ?? 0)
            .Select(stat => stat.Name).Distinct().Take(3).ToArray();
        var width = Math.Max(0, Math.Min(150 + columns.Length * 80, viewport.Width - 24));
        var x = viewport.Width - width - 12;
        var count = Math.Min(rows.Length, Math.Max(0, ((int)viewport.Height / 2 - 35) / 23));
        var panel = new GuiRect(x, 12, width, 33 + 23 * count);
        commands.Add(new("Rectangle", panel, viewport, Background, 0.95f, Radius: 8));
        DrawText(commands, panel, x + 10, 17, width * .4f, "Players");
        for (var index = 0; index < count; index++)
            DrawText(commands, panel, x + 10, 40 + index * 23, width * .4f, rows[index].Name);
        var cell = width * .55f / Math.Max(1, columns.Length);
        for (var column = 0; column < columns.Length; column++)
        {
            var left = x + width * .45f + column * cell;
            DrawText(commands, panel, left, 17, cell, columns[column]);
            for (var index = 0; index < count; index++)
            {
                var value = rows[index].Stats.FirstOrDefault(stat => stat.Name == columns[column])?.Value;
                DrawText(commands, panel, left, 40 + index * 23, cell, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "");
            }
        }
    }

    private static IEnumerable<ValueObject> GetStats(Player player) =>
        player.FindFirstChild("leaderstats") is { ClassName: "Folder" } folder
            ? folder.GetChildren().OfType<ValueObject>().Where(stat =>
                stat.ClassName is "IntValue" or "NumberValue" or "StringValue" or "BoolValue")
            : [];

    private void DrawMenu(List<GuiDrawCommand> commands, GuiRect viewport, int playerCount)
    {
        commands.Add(new("Rectangle", viewport, viewport, Background, .65f));
        var width = Math.Max(0, Math.Min(320, viewport.Width - 24));
        var x = (viewport.Width - width) / 2;
        var y = Math.Max(0, (viewport.Height - 132) / 2);
        DrawPanel(commands, viewport, x, y, width, ["Session menu", $"{playerCount} connected"]);
        resume = new(x, y + 66, width, 44);
        commands.Add(new("Rectangle", resume, viewport, Rgb(20, 111, 111), 1, Radius: 8));
        commands.Add(new("Text", resume, viewport, Foreground, 1, "Resume"));
    }

    private static void DrawPanel(List<GuiDrawCommand> commands, GuiRect viewport, float x, float y, float width, string[] lines)
    {
        var panel = new GuiRect(x, y, width, 10 + 23 * lines.Length);
        commands.Add(new("Rectangle", panel, viewport, Background, .96f, Radius: 8));
        for (var index = 0; index < lines.Length; index++)
            DrawText(commands, panel.Intersect(viewport), x + 10, y + 5 + index * 23, width - 20, lines[index]);
    }

    private static void DrawText(List<GuiDrawCommand> commands, GuiRect clip, float x, float y, float width, string text)
    {
        var bounds = new GuiRect(x, y, width, 23);
        commands.Add(new("Text", bounds, bounds.Intersect(clip), Foreground, 1, text, XAlignment: "Left"));
    }
}
