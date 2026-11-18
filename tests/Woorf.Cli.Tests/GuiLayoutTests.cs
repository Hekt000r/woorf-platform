using Woorf.Engine.DataModel;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Gui;
using Woorf.Engine.Datatypes;

internal static class GuiLayoutTests
{
    internal static int Run()
    {
        try
        {
            CheckLocalScripts();
            CheckLayoutAvailable();
            CheckNestedLayout();
            CheckButtonAndFocus();
            CheckRequestedFocus();
            CheckInactiveHover();
            CheckTextSelection();
            CheckTextNavigation();
            CheckButtonColor();
            CheckEditingVisuals();
            Console.WriteLine("PASS play mode starts an isolated client and PlayerGui LocalScripts");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL GUI layout: " + error.Message); return 1; }
    }

    private static void CheckLayoutAvailable()
    {
        if (typeof(Instance).Assembly.GetType("Woorf.Engine.Gui.GuiScene") is null)
            throw new Exception("GUI layout must produce shared viewport-relative draw commands and hit targets.");
    }

    private static GuiInstance Create(string name, Instance parent)
    {
        var item = (GuiInstance)Instance.Create(name); item.Parent = parent; return item;
    }

    private static void CheckNestedLayout()
    {
        var root = Instance.Create("Folder"); var screen = Create("ScreenGui", root);
        var parent = Create("Frame", screen);
        parent.SetGuiProperty("Position", new UDim2(0.5f, 0, 0.5f, 0));
        parent.SetGuiProperty("Size", new UDim2(0, 200, 0, 100)); parent.SetGuiProperty("AnchorPoint", new Vector2(0.5f, 0.5f));
        parent.SetGuiProperty("ClipsDescendants", true);
        var child = Create("TextButton", parent); child.SetGuiProperty("Position", new UDim2(1, -10, 0, 0));
        child.SetGuiProperty("Size", UDim2.FromOffset(30, 20));
        var scene = new GuiScene(); scene.Capture(root, 800, 600);
        if (parent.Get<Vector2>("AbsolutePosition") != new Vector2(300, 250)
            || child.Get<Vector2>("AbsolutePosition") != new Vector2(490, 250)) throw new Exception("Scale, offset and anchor must compose relative to the parent.");
        var activations = 0; child.GetGuiSignal("Activated").Connect(_ => activations++);
        scene.Pointer(515, 255, true); scene.Pointer(515, 255, false);
        if (activations != 0) throw new Exception("Clipped child pixels must not receive clicks.");
        scene.Pointer(495, 255, true); scene.Pointer(495, 255, false);
        if (activations != 1) throw new Exception("Visible child pixels must receive clicks.");
    }

    private static void CheckButtonAndFocus()
    {
        var root = Instance.Create("Folder"); var screen = Create("ScreenGui", root);
        var button = Create("TextButton", screen); button.SetGuiProperty("Size", UDim2.FromOffset(100, 40));
        var label = Create("TextLabel", button); label.SetGuiProperty("Size", UDim2.FromScale(1, 1)); label.SetGuiProperty("BackgroundTransparency", 1d);
        var activations = 0; button.GetGuiSignal("Activated").Connect(_ => activations++);
        var scene = new GuiScene(); scene.Capture(root, 300, 200);
        scene.Pointer(10, 10, true); scene.Pointer(10, 10, false);
        if (activations != 1) throw new Exception("An inactive decorative label must not block its button's activation.");
        scene.Pointer(10, 10, true); scene.Pointer(150, 10, false);
        if (activations != 1) throw new Exception("Release outside the pressed button must cancel activation.");
        var box = Create("TextBox", screen); box.SetGuiProperty("Position", UDim2.FromOffset(0, 60));
        var submitted = false; box.GetGuiSignal("FocusLost").Connect(values => submitted = values[0] is true);
        scene.Capture(root, 300, 200); scene.Pointer(10, 70, true); scene.Pointer(10, 70, false); scene.TextInput("hello\b!\r");
        if (box.Get<string>("Text") != "hell!" || !submitted || scene.IsTextFocused) throw new Exception("Text input must edit and submit the focused textbox.");
    }

    private static void CheckRequestedFocus()
    {
        var root = Instance.Create("Folder"); var screen = Create("ScreenGui", root);
        var box = Create("TextBox", screen); box.SetFocused(true);
        var scene = new GuiScene(); scene.Capture(root, 300, 200); scene.TextInput("script focus");
        if (box.Get<string>("Text") != "script focus" || !scene.IsTextFocused)
            throw new Exception("CaptureFocus requested by script must route keyboard input to that textbox.");
        box.SetFocused(false); scene.Capture(root, 300, 200); scene.TextInput("ignored");
        if (scene.IsTextFocused || box.Get<string>("Text") != "script focus")
            throw new Exception("ReleaseFocus requested by script must stop text input.");
    }

    private static void CheckInactiveHover()
    {
        var root = Instance.Create("Folder"); var screen = Create("ScreenGui", root);
        var label = Create("TextLabel", screen); var entered = 0; var left = 0;
        label.GetGuiSignal("MouseEnter").Connect(_ => entered++); label.GetGuiSignal("MouseLeave").Connect(_ => left++);
        var scene = new GuiScene(); scene.Capture(root, 300, 200);
        scene.Pointer(10, 10, false); scene.Pointer(150, 150, false);
        if (entered != 1 || left != 1) throw new Exception("Inactive GUI objects must still receive mouse hover enter and leave events.");
    }

    private static void CheckTextSelection()
    {
        var root = Instance.Create("Folder"); var screen = Create("ScreenGui", root); var box = Create("TextBox", screen);
        box.SetGuiProperty("ClearTextOnFocus", false); box.SetGuiProperty("Text", "replace this"); box.SetFocused(true);
        var scene = new GuiScene(); scene.Capture(root, 300, 200);
        box.SetGuiProperty("SelectionStart", 1d); box.SetGuiProperty("CursorPosition", 8d); scene.TextInput("keep");
        if (box.Get<string>("Text") != "keep this") throw new Exception("Typing must replace the selected text range.");
        box.SetGuiProperty("SelectionStart", 1d); box.SetGuiProperty("CursorPosition", 5d); scene.TextInput("\b");
        if (box.Get<string>("Text") != " this") throw new Exception("Backspace must delete the selected range.");
    }

    private static void CheckTextNavigation()
    {
        var root = Instance.Create("Folder"); var screen = Create("ScreenGui", root); var box = Create("TextBox", screen);
        box.SetGuiProperty("ClearTextOnFocus", false); box.SetGuiProperty("Text", "abcd"); box.SetFocused(true);
        var scene = new GuiScene(); scene.Capture(root, 300, 200);
        var keyInput = typeof(GuiScene).GetMethod("KeyInput") ?? throw new Exception("Focused textboxes need keyboard cursor navigation.");
        keyInput.Invoke(scene, ["Left", false]); scene.TextInput("X");
        if (box.Get<string>("Text") != "abcXd") throw new Exception("Left arrow must move the insertion cursor.");
        keyInput.Invoke(scene, ["Home", false]); keyInput.Invoke(scene, ["Right", true]); scene.TextInput("Q");
        if (box.Get<string>("Text") != "QbcXd") throw new Exception("Shift-arrow must select text to replace.");
        keyInput.Invoke(scene, ["Home", false]); keyInput.Invoke(scene, ["Delete", false]);
        if (box.Get<string>("Text") != "bcXd") throw new Exception("Delete must remove the character after the cursor.");
    }

    private static void CheckButtonColor()
    {
        var root = Instance.Create("Folder"); var screen = Create("ScreenGui", root); var button = Create("TextButton", screen);
        button.SetGuiProperty("BackgroundColor3", new Color3(1, 0, 0)); button.SetGuiProperty("BorderSizePixel", 0d);
        var scene = new GuiScene(); scene.Capture(root, 300, 200); scene.Pointer(10, 10, false);
        var hoveredColor = scene.Capture(root, 300, 200).First(command => command.Kind == "Rectangle").Color;
        if (hoveredColor.R >= 1) throw new Exception("AutoButtonColor must visibly respond to pointer hover.");
        scene.Pointer(10, 10, true);
        var pressedColor = scene.Capture(root, 300, 200).First(command => command.Kind == "Rectangle").Color;
        if (pressedColor.R >= hoveredColor.R) throw new Exception("Pressed buttons must provide distinct visual feedback.");
        button.SetGuiProperty("AutoButtonColor", false);
        if (scene.Capture(root, 300, 200).First(command => command.Kind == "Rectangle").Color.R != 1)
            throw new Exception("AutoButtonColor=false must preserve the scripted color.");
    }

    private static void CheckEditingVisuals()
    {
        var root = Instance.Create("Folder"); var screen = Create("ScreenGui", root); var box = Create("TextBox", screen);
        box.SetGuiProperty("BackgroundTransparency", 1d); box.SetGuiProperty("BorderSizePixel", 0d);
        box.SetGuiProperty("TextXAlignment", GuiEnum.From("TextXAlignment", "Left"));
        box.SetGuiProperty("TextYAlignment", GuiEnum.From("TextYAlignment", "Top")); box.SetFocused(true);
        var scene = new GuiScene(); scene.Capture(root, 300, 200); scene.TextInput("abcd");
        var commands = scene.Capture(root, 300, 200);
        if (!commands.Any(command => command.Kind == "Rectangle" && command.Opacity > 0 && command.Bounds.Width <= 2))
            throw new Exception("The focused textbox must show its insertion caret.");
        box.SetGuiProperty("SelectionStart", 1d); box.SetGuiProperty("CursorPosition", 3d);
        if (!scene.Capture(root, 300, 200).Any(command => command.Kind == "Rectangle" && command.Opacity > 0 && command.Bounds.Width > 2))
            throw new Exception("A selected text range must have a visible selection highlight.");
    }

    private static void CheckLocalScripts()
    {
        var directory = Path.Combine(Path.GetTempPath(), "woorf-gui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "src", "StarterGui"));
        try
        {
            File.WriteAllText(Path.Combine(directory, "woorf.project.json"), "{}");
            File.WriteAllText(Path.Combine(directory, "src", "StarterGui", "Hud.client.luau"),
                "local p=game:GetService('Players').LocalPlayer; assert(p); p.PlayerGui:SetAttribute('Started', true)");
            using var runtime = new RenderRuntime(new(directory, false, 1, null, 320, 240, null, true), new MonotonicClock());
            if (runtime.StartupStatus != 0) throw new Exception("Client script startup failed.");
            var player = ((Players)runtime.Game.GetService("Players")).LocalPlayer;
            if (player?.FindFirstChild("PlayerGui")?.GetAttribute("Started") is not true)
                throw new Exception("Playable runtime must start StarterGui LocalScripts in its owning client's PlayerGui.");
        }
        finally { Directory.Delete(directory, true); }
    }
}
