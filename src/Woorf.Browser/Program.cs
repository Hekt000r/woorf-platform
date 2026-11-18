using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using Woorf.Engine.DataModel;
using Woorf.Engine.Networking;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;
using Woorf.Graphics;
using Woorf.Engine.Gui;
using Woorf.Presentation;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("browser")]

return;

public static partial class BrowserApp
{
    private static LocalPlayHost? host;
    private static WorldView? world;
    private static ScenePacketEncoder encoder = new();
    private static CharacterViewer? viewer;
    private static readonly PerspectiveCamera camera = new();
    private static readonly BrowserClock clock = new();
    private static double previousTime;
    private static bool IsGameplayBlocked => gui.IsTextFocused || host!.Game.CoreOverlay.MenuIsOpen;
    private static GameCursor cursor = new();
    private static bool isCursorActive;
    private static ToolHotbar? hotbar;
    private static GuiScene gui = new();
    private static readonly JsonSerializerOptions frameJson = new() { Converters = { new GuiVectorJsonConverter() } };

    [JSExport]
    public static void Start(string package)
    {
        host?.Dispose(); world?.Dispose();
        var directory = "/game";
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
        Directory.CreateDirectory(directory);
        gui = new();
        cursor = new();
        isCursorActive = false;
        encoder = new();
        using var document = JsonDocument.Parse(package);
        foreach (var file in document.RootElement.GetProperty("files").EnumerateObject())
        {
            var path = Path.GetFullPath(Path.Combine(directory, file.Name));
            if (!path.StartsWith(directory + "/", StringComparison.Ordinal)) throw new InvalidDataException("Invalid package path.");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file.Value.GetString());
        }
        clock.Now = 0; previousTime = 0;
        host = new LocalPlayHost(ProjectLoader.Load(directory), clock);
        hotbar = new ToolHotbar(host.Player, host.ToggleTool, host.UseTool);
        world = new WorldView(host.Game, host.Player);
        viewer = new CharacterViewer(host.Player, input => host.SendInput(input.MoveDirection, input.ShouldJump, input.ShouldReset, input.CameraHeading));
    }

    [JSExport]
    public static string Frame(double now, float forward, float right, bool jump, bool reset,
        float yaw, float pitch, int width, int height)
    {
        if (host is null) throw new InvalidOperationException("Game has not started.");
        var frameTime = now - previousTime;
        var elapsed = (float)Math.Clamp(frameTime, 0, 0.1); previousTime = now; clock.Now = now;
        viewer!.ResizeCamera(width, height);
        viewer!.ApplyInput(camera, IsGameplayBlocked ? 0 : forward, IsGameplayBlocked ? 0 : right,
            !IsGameplayBlocked && jump, !IsGameplayBlocked && reset,
            IsGameplayBlocked ? 0 : yaw, IsGameplayBlocked ? 0 : pitch, elapsed);
        if (host.Pump() != 0) throw new InvalidOperationException("Game script failed.");
        viewer.Follow(camera, elapsed);
        cursor.Resize(width, height);
        var captured = world!.Capture(now, viewer.IsFirstPerson);
        return JsonSerializer.Serialize(new { scene = encoder.Encode(captured, camera, (float)width / height),
            gui = gui.Capture(host.Player.FindFirstChild("PlayerGui"), width, height)
                .Concat(hotbar!.Capture(width, height, host.Game.CoreOverlay.IsBackpackEnabled && !host.Game.CoreOverlay.MenuIsOpen)).Concat(
                host.Game.CoreOverlay.Capture(((Players)host.Game.GetService("Players")).GetPlayers(),
                    width, height, frameTime, "Local", host.Game.ScriptOutput))
                .Concat(cursor.Draw(isCursorActive && (!viewer.IsFirstPerson || IsGameplayBlocked))),
            position = host.Player.Character?.PrimaryPart?.Position.ToString() }, frameJson);
    }

    [JSExport]
    public static void Pointer(float x, float y, bool isDown, float wheel)
    {
        var isBlocked = host!.Game.CoreOverlay.Pointer(x, y, isDown);
        var isOverHotbar = hotbar?.Hit(x, y) == true;
        if (!isBlocked && !isOverHotbar) isBlocked = gui.Pointer(x, y, isDown, wheel);
        hotbar?.Pointer(x, y, isDown, isBlocked || IsGameplayBlocked);
        if (!isBlocked && !isOverHotbar && !IsGameplayBlocked && wheel != 0)
            viewer!.ApplyPointer(0, 0, wheel, false);
    }

    [JSExport]
    public static void GamePointer(float x, float y, float deltaX, float deltaY, bool isDown, float wheel, bool isOrbiting)
    {
        var isFirstPerson = !IsGameplayBlocked && viewer!.IsFirstPerson;
        if (!isCursorActive) cursor.Place(x, y);
        else cursor.Move(deltaX, deltaY, isFirstPerson || (isOrbiting && !IsGameplayBlocked));
        isCursorActive = true;
        Pointer(isFirstPerson ? -1 : cursor.Position.X, isFirstPerson ? -1 : cursor.Position.Y, isDown, wheel);
    }

    [JSExport]
    public static bool CameraMotion(float deltaX, float deltaY, bool isRightDown)
    {
        if (IsGameplayBlocked) return false;
        viewer!.ApplyPointer(deltaX, deltaY, 0, isRightDown);
        return viewer.IsFirstPerson;
    }

    [JSExport]
    public static void TextInput(string text)
    {
        if (!host!.Game.CoreOverlay.MenuIsOpen) gui.TextInput(text);
    }

    [JSExport]
    public static bool CoreKey(string key, bool shift)
    {
        var core = host!.Game.CoreOverlay;
        if (!core.KeyDown(key, shift))
            return !IsGameplayBlocked && core.IsBackpackEnabled && hotbar?.KeyDown(key) == true;
        if (key == "Escape" && core.MenuIsOpen)
        {
            gui.Pointer(-1, -1, false);
            gui.ReleaseFocus(false);
        }
        return true;
    }

    [JSExport]
    public static void KeyInput(string key, bool isShiftDown)
    {
        if (!host!.Game.CoreOverlay.MenuIsOpen) gui.KeyInput(key, isShiftDown);
    }

    [JSExport]
    public static bool InputKey(string key,bool isDown) =>
        ((UserInputService)host!.Game.GetService("UserInputService")).SetKey(key,isDown,IsGameplayBlocked);

    [JSExport]
    public static void GamepadButton(string key, bool isDown) =>
        ((UserInputService)host!.Game.GetService("UserInputService")).SetInput("Gamepad1", key, isDown, IsGameplayBlocked);

    [JSExport]
    public static void Blur()
    {
        isCursorActive = false;
        gui.ReleaseFocus(false);
        ((UserInputService)host!.Game.GetService("UserInputService")).ReleaseKeys();
    }

    [JSExport]
    public static string Meshes() => JsonSerializer.Serialize(new[] { MeshData.CreateBox(), MeshData.CreateSphere() });

    [JSExport]
    public static string Shaders() => JsonSerializer.Serialize(ShaderProfile.WebGlPrograms());

    private sealed class BrowserClock : IRuntimeClock
    {
        internal double Now;
        public double ElapsedSeconds => Now;
        public void Sleep(TimeSpan duration) => Now += duration.TotalSeconds;
    }
}

