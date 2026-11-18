using Woorf.Engine.DataModel;
using Woorf.Engine.Gui;

internal static class GuiEditingTests
{
    internal static int Run()
    {
        var failures = 0;
        foreach (var (name, check) in new (string, Action)[]
        {
            ("GUI backspace removes a whole emoji", CheckBackspace),
            ("GUI arrows collapse a selection before moving", CheckSelection),
        })
        {
            try { check(); Console.WriteLine("PASS " + name); }
            catch (Exception error) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + error.Message); }
        }
        return failures;
    }

    private static (GuiScene Scene, GuiInstance Box) CreateBox(string text)
    {
        var root = Instance.Create("Folder");
        var screen = Instance.Create("ScreenGui"); screen.Parent = root;
        var box = (GuiInstance)Instance.Create("TextBox"); box.Parent = screen;
        box.SetGuiProperty("ClearTextOnFocus", false); box.SetGuiProperty("Text", text); box.SetFocused(true);
        var scene = new GuiScene(); scene.Capture(root, 300, 200);
        return (scene, box);
    }

    private static void CheckBackspace()
    {
        var (scene, box) = CreateBox("A😀");
        scene.TextInput("\b");
        if (box.Get<string>("Text") != "A" || box.Get<double>("CursorPosition") != 2)
            throw new Exception("Backspace must remove both UTF-16 code units and leave the cursor after A.");
        scene.TextInput("B");
        if (box.Get<string>("Text") != "AB") throw new Exception("Typing after deletion must preserve valid text.");
    }

    private static void CheckSelection()
    {
        var (scene, box) = CreateBox("abcdef");
        box.SetGuiProperty("SelectionStart", 2d); box.SetGuiProperty("CursorPosition", 5d);
        scene.KeyInput("Left"); scene.TextInput("X");
        if (box.Get<string>("Text") != "aXbcdef")
            throw new Exception("Left without Shift must collapse to the start of the selected range.");
        box.SetGuiProperty("SelectionStart", 2d); box.SetGuiProperty("CursorPosition", 5d);
        scene.KeyInput("Right"); scene.TextInput("Y");
        if (box.Get<string>("Text") != "aXbcYdef")
            throw new Exception("Right without Shift must collapse to the end of the selected range.");
    }
}
