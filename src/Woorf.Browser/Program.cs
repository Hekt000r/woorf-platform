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
        viewer!.ApplyInput(camera, IsGameplayBlocked ? 0 : forward, IsGameplayBlocked ? 0 : right,
            !IsGameplayBlocked && jump, !IsGameplayBlocked && reset,
            IsGameplayBlocked ? 0 : yaw, IsGameplayBlocked ? 0 : pitch, elapsed);
        if (host.Pump() != 0) throw new InvalidOperationException("Game script failed.");
        viewer.Follow(camera, elapsed);
        var captured = world!.Capture(now, viewer.IsFirstPerson);
        return JsonSerializer.Serialize(new { scene = encoder.Encode(captured, camera, (float)width / height),
            gui = gui.Capture(host.Player.FindFirstChild("PlayerGui"), width, height).Concat(
                host.Game.CoreOverlay.Capture(((Players)host.Game.GetService("Players")).GetPlayers(),
                    width, height, frameTime, "Local", host.Game.ScriptOutput)),
            position = host.Player.Character?.PrimaryPart?.Position.ToString() }, frameJson);
    }

    [JSExport]
    public static void Pointer(float x, float y, bool isDown, float wheel)
    {
        if (host!.Game.CoreOverlay.Pointer(x, y, isDown)) return;
        if (!gui.Pointer(x, y, isDown, wheel) && !IsGameplayBlocked && wheel != 0)
            viewer!.ApplyPointer(0, 0, wheel, false);
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
        if (!core.KeyDown(key, shift)) return false;
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
    public static void InputKey(string key,bool isDown) =>
        ((UserInputService)host!.Game.GetService("UserInputService")).SetKey(key,isDown,IsGameplayBlocked);

    [JSExport]
    public static void Blur()
    {
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

