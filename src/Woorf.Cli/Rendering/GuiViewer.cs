using System.Numerics;
using Woorf.Engine.DataModel;
using Woorf.Engine.Gui;
using Woorf.Graphics;
using Woorf.Graphics.Platform;

internal sealed class GuiViewer(OpenGlWindow window, Player? player, DataModel game) : IDisposable
{
    private readonly GuiScene scene = new();
    private readonly GuiRenderer renderer = new(window);
    private readonly List<GuiDrawItem> items = [];
    internal bool IsTextFocused => scene.IsTextFocused || game.CoreOverlay.MenuIsOpen;

    internal float UpdateInput()
    {
        var (width, height) = window.GetSize();
        scene.Capture(player?.FindFirstChild("PlayerGui"), width, height);
        var input = window.ReadPointerInput();
        var isPointerConsumed = player is not null && game.CoreOverlay.Pointer(input.X, input.Y, input.IsDown);
        if (!isPointerConsumed) isPointerConsumed = scene.Pointer(input.X, input.Y, input.IsDown, input.Wheel);
        if (!input.IsFocused) scene.ReleaseFocus(false);
        foreach (var key in window.ReadKeyInput())
        {
            if (player is not null && game.CoreOverlay.KeyDown(key.Key, key.IsShiftDown))
            {
                if (key.Key == "Escape" && game.CoreOverlay.MenuIsOpen)
                {
                    scene.Pointer(-1, -1, false);
                    scene.ReleaseFocus(false);
                }
            }
            else if (!game.CoreOverlay.MenuIsOpen) scene.KeyInput(key.Key, key.IsShiftDown);
        }
        if (!game.CoreOverlay.MenuIsOpen) scene.TextInput(input.Text);
        var keyboard=(UserInputService)game.GetService("UserInputService");
        for(var key='A';key<='Z';key++) keyboard.SetKey(key.ToString(),window.IsKeyDown(key),IsTextFocused);
        keyboard.SetKey("Space",window.IsKeyDown(0x20),IsTextFocused);
        return isPointerConsumed || IsTextFocused ? 0 : input.Wheel;
    }

    internal void Draw(double elapsed, string transport)
    {
        var (width, height) = window.GetSize();
        var commands = scene.Capture(player?.FindFirstChild("PlayerGui"), width, height);
        IReadOnlyList<GuiDrawCommand> overlay = player is null ? [] : game.CoreOverlay.Capture(
            ((Players)game.GetService("Players")).GetPlayers(), width, height, elapsed, transport, game.ScriptOutput);
        items.Clear();
        foreach (var command in commands.Concat(overlay))
            items.Add(new(command.Kind, ConvertBounds(command.Bounds), ConvertBounds(command.Clip),
                new Vector4(command.Color.R, command.Color.G, command.Color.B, command.Opacity),
                command.Content, command.TextSize, command.XAlignment, command.YAlignment,
                command.Wrapped, command.Radius, command.Rotation, command.Font, command.Bold,
                command.Italic, command.Underline, command.Strikethrough,
                new(command.ImageOffset.X, command.ImageOffset.Y), new(command.ImageSize.X, command.ImageSize.Y),
                command.ScaleType, new(command.TileSize.X, command.TileSize.Y)));
        if (items.Count > 0) renderer.Draw(items);
    }

    private static GuiBounds ConvertBounds(GuiRect value) => new(value.X, value.Y, value.Width, value.Height);
    public void Dispose() => renderer.Dispose();
}
