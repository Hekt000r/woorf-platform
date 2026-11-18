using System.Numerics;
using Woorf.Engine.DataModel;
using Woorf.Engine.Gui;
using Woorf.Graphics;
using Woorf.Graphics.Platform;

internal sealed class GuiViewer(OpenGlWindow window, Player? player, DataModel game, ToolHotbar? hotbar) : IDisposable
{
    private readonly GameCursor cursor = new();
    private (float X, float Y)? previousPointer;
    private bool wasCursorFrozen;
    private bool hasCursorOrigin;
    private bool isCursorVisible;
    private readonly GuiScene scene = new();
    private readonly GuiRenderer renderer = new(window);
    private readonly List<GuiDrawItem> items = [];
    internal UserInputService Keyboard => (UserInputService)game.GetService("UserInputService");
    internal bool IsTextFocused => scene.IsTextFocused || game.CoreOverlay.MenuIsOpen;

    internal float UpdateInput(bool isFirstPerson)
    {
        var (width, height) = window.GetSize();
        scene.Capture(player?.FindFirstChild("PlayerGui"), width, height);
        var input = window.ReadPointerInput();
        cursor.Resize(width, height);
        var isCameraActive = player is not null && !IsTextFocused;
        var isCursorFrozen = isCameraActive && (isFirstPerson || window.IsKeyDown(0x02));
        isCursorVisible = player is not null && input.IsFocused && !(isCameraActive && isFirstPerson);
        if (input.IsFocused && previousPointer is { } previous)
            cursor.Move(input.X - previous.X, input.Y - previous.Y, isCursorFrozen || wasCursorFrozen);
        if (!hasCursorOrigin && input.IsFocused && input.X >= 0 && input.Y >= 0)
        {
            cursor.Place(input.X, input.Y);
            hasCursorOrigin = true;
        }
        // Camera capture is released after this input pass. Rebase on the next sample.
        previousPointer = input.IsFocused && !(wasCursorFrozen && !isCursorFrozen) ? (input.X, input.Y) : null;
        wasCursorFrozen = isCursorFrozen;
        var x = player is null ? input.X : isCursorVisible ? cursor.Position.X : -1;
        var y = player is null ? input.Y : isCursorVisible ? cursor.Position.Y : -1;
        var isPointerConsumed = player is not null && game.CoreOverlay.Pointer(x, y, input.IsDown);
        var isOverHotbar = hotbar?.Hit(x, y) == true;
        if (!isPointerConsumed && !isOverHotbar) isPointerConsumed = scene.Pointer(x, y, input.IsDown, input.Wheel);
        hotbar?.Pointer(x, y, input.IsDown, isPointerConsumed || IsTextFocused || window.IsKeyDown(0x11));
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
            else if (!game.CoreOverlay.MenuIsOpen)
            {
                var handled = !IsTextFocused && game.CoreOverlay.IsBackpackEnabled && hotbar?.KeyDown(key.Key) == true;
                if (!handled) scene.KeyInput(key.Key, key.IsShiftDown);
            }
        }
        if (!game.CoreOverlay.MenuIsOpen) scene.TextInput(input.Text);
        var keyboard = Keyboard;
        for(var key='A';key<='Z';key++) keyboard.SetKey(key.ToString(),window.IsKeyDown(key),IsTextFocused);
        keyboard.SetKey("Space",window.IsKeyDown(0x20),IsTextFocused);
        if (player is not null) GamepadWindowInput.Update(window, keyboard, IsTextFocused);
        return isPointerConsumed || IsTextFocused ? 0 : input.Wheel;
    }

    internal void Draw(double elapsed, string transport)
    {
        var (width, height) = window.GetSize();
        var commands = scene.Capture(player?.FindFirstChild("PlayerGui"), width, height);
        IReadOnlyList<GuiDrawCommand> overlay = player is null ? [] : game.CoreOverlay.Capture(
            ((Players)game.GetService("Players")).GetPlayers(), width, height, elapsed, transport, game.ScriptOutput);
        var inventory = hotbar?.Capture(width, height, game.CoreOverlay.IsBackpackEnabled && !game.CoreOverlay.MenuIsOpen) ?? [];
        items.Clear();
        foreach (var command in commands.Concat(inventory).Concat(overlay).Concat(cursor.Draw(isCursorVisible)))
            items.Add(new(command.Kind, ConvertBounds(command.Bounds), ConvertBounds(command.Clip),
                new Vector4(command.Color.R, command.Color.G, command.Color.B, command.Opacity),
                command.Content, command.TextSize, command.XAlignment, command.YAlignment,
                command.Wrapped, command.Radius, command.Rotation, command.Font, command.Bold,
                command.Italic, command.Underline, command.Strikethrough,
                new(command.ImageOffset.X, command.ImageOffset.Y), new(command.ImageSize.X, command.ImageSize.Y),
                command.ScaleType, new(command.TileSize.X, command.TileSize.Y),
                command.Gradient is { } gradient ? new(new(gradient.Start.X, gradient.Start.Y),
                    new(gradient.End.X, gradient.End.Y), ConvertBounds(command.GradientBounds),
                    gradient.Stops.Select(stop => new GuiGradientSample(stop.Time,
                        new(stop.Color.R, stop.Color.G, stop.Color.B, stop.Opacity))).ToArray()) : null));
        if (items.Count > 0) renderer.Draw(items);
    }

    private static GuiBounds ConvertBounds(GuiRect value) => new(value.X, value.Y, value.Width, value.Height);
    public void Dispose() => renderer.Dispose();
}
