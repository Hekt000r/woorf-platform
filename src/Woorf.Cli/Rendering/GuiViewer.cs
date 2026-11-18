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
    internal bool IsTextFocused => scene.IsTextFocused;

    internal void UpdateInput()
    {
        var (width, height) = window.GetSize();
        scene.Capture(player?.FindFirstChild("PlayerGui"), width, height);
        var input = window.ReadPointerInput();
        scene.Pointer(input.X, input.Y, input.IsDown, input.Wheel);
        if (!input.IsFocused) scene.ReleaseFocus(false);
        foreach (var key in window.ReadKeyInput()) scene.KeyInput(key.Key, key.IsShiftDown);
        scene.TextInput(input.Text);
        var keyboard=(UserInputService)game.GetService("UserInputService");
        for(var key='A';key<='Z';key++) keyboard.SetKey(key.ToString(),window.IsKeyDown(key),scene.IsTextFocused);
        keyboard.SetKey("Space",window.IsKeyDown(0x20),scene.IsTextFocused);
    }

    internal void Draw()
    {
        var (width, height) = window.GetSize();
        var commands = scene.Capture(player?.FindFirstChild("PlayerGui"), width, height);
        items.Clear();
        foreach (var command in commands)
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
