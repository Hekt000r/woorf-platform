using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Gui;

static class GuiSizingTests
{
    internal static int Run()
    {
        var failures = 0;
        foreach (var (name, check) in new (string, Action)[]
        {
            ("GUI automatic sizing grows text and descendants within minimum bounds", CheckAutomaticSize),
            ("GUI scale multiplies parent and descendant dimensions", CheckScale),
            ("GUI vertical grids honor fill count and alignment", CheckGrid),
            ("GUI automatic canvas follows content and clamps scrolling", CheckCanvas),
            ("GUI global ZIndex orders descendants and their pointer targets", CheckGlobalOrder),
        })
        {
            try { check(); Console.WriteLine("PASS " + name); }
            catch (Exception error) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + error.Message); }
        }
        return failures;
    }

    private static GuiInstance Create(string name, Instance parent)
    {
        var item = (GuiInstance)Instance.Create(name); item.Parent = parent;
        return item;
    }

    private static (Instance Root, GuiInstance Screen) CreateScreen()
    {
        var root = Instance.Create("Folder"); return (root, Create("ScreenGui", root));
    }

    private static void CheckAutomaticSize()
    {
        var (root, screen) = CreateScreen();
        var label = Create("TextLabel", screen); label.SetGuiProperty("Size", UDim2.FromOffset(10, 10));
        label.SetGuiProperty("Text", "HELLO"); label.SetGuiProperty("TextSize", 20d);
        label.SetGuiProperty("AutomaticSize", GuiEnum.From("AutomaticSize", "XY"));
        var frame = Create("Frame", screen); frame.SetGuiProperty("Size", UDim2.FromOffset(10, 10));
        frame.SetGuiProperty("AutomaticSize", GuiEnum.From("AutomaticSize", "XY"));
        var child = Create("Frame", frame); child.SetGuiProperty("Position", UDim2.FromOffset(10, 15));
        child.SetGuiProperty("Size", UDim2.FromOffset(40, 20));
        new GuiScene().Capture(root, 640, 480);
        var textSize = label.Get<Vector2>("AbsoluteSize");
        if (Math.Abs(textSize.X - 60) > 0.001 || Math.Abs(textSize.Y - 20) > 0.001)
            throw new Exception("Text must grow to its natural content size: " + textSize);
        if (frame.Get<Vector2>("AbsoluteSize") != new Vector2(50, 35)) throw new Exception("Frame must enclose its positioned child.");
        label.SetGuiProperty("Text", "A"); new GuiScene().Capture(root, 640, 480);
        if (label.Get<Vector2>("AbsoluteSize").X >= 60) throw new Exception("Automatic size must shrink when content changes.");
    }

    private static void CheckScale()
    {
        var (root, screen) = CreateScreen(); var frame = Create("Frame", screen);
        frame.SetGuiProperty("Size", UDim2.FromOffset(100, 50));
        Create("UIScale", frame).SetGuiProperty("Scale", 2d);
        var child = Create("Frame", frame); child.SetGuiProperty("Size", new UDim2(0.5f, 0, 0, 10));
        new GuiScene().Capture(root, 640, 480);
        if (frame.Get<Vector2>("AbsoluteSize") != new Vector2(200, 100) || child.Get<Vector2>("AbsoluteSize") != new Vector2(100, 20))
            throw new Exception("UIScale must scale its parent and offsets without doubling descendant scale components.");
    }

    private static void CheckGrid()
    {
        var (root, screen) = CreateScreen(); var frame = Create("Frame", screen);
        frame.SetGuiProperty("Size", UDim2.FromOffset(100, 100));
        var grid = Create("UIGridLayout", frame); grid.SetGuiProperty("CellSize", UDim2.FromOffset(20, 20));
        grid.SetGuiProperty("CellPadding", UDim2.FromOffset(5, 5)); grid.SetGuiProperty("FillDirectionMaxCells", 2d);
        grid.SetGuiProperty("FillDirection", GuiEnum.From("FillDirection", "Vertical"));
        grid.SetGuiProperty("HorizontalAlignment", GuiEnum.From("HorizontalAlignment", "Right"));
        grid.SetGuiProperty("VerticalAlignment", GuiEnum.From("VerticalAlignment", "Bottom"));
        var a = Create("Frame", frame); a.Name = "A"; var b = Create("Frame", frame); b.Name = "B"; var c = Create("Frame", frame); c.Name = "C";
        new GuiScene().Capture(root, 640, 480);
        if (a.Get<Vector2>("AbsolutePosition") != new Vector2(55, 55) || b.Get<Vector2>("AbsolutePosition") != new Vector2(55, 80)
            || c.Get<Vector2>("AbsolutePosition") != new Vector2(80, 55)) throw new Exception("Vertical fill and bottom/right alignment must determine cell positions.");
    }

    private static void CheckCanvas()
    {
        var (root, screen) = CreateScreen(); var frame = Create("ScrollingFrame", screen);
        frame.SetGuiProperty("Size", UDim2.FromOffset(100, 50)); frame.SetGuiProperty("CanvasSize", UDim2.FromOffset(0, 0));
        frame.SetGuiProperty("AutomaticCanvasSize", GuiEnum.From("AutomaticSize", "Y"));
        var child = Create("Frame", frame); child.SetGuiProperty("Position", UDim2.FromOffset(0, 150)); child.SetGuiProperty("Size", UDim2.FromOffset(40, 30));
        var scene = new GuiScene(); scene.Capture(root, 640, 480);
        if (frame.Get<Vector2>("AbsoluteCanvasSize").Y != 180) throw new Exception("Automatic canvas must include the bottom of its child.");
        scene.Pointer(10, 10, false, -1);
        if (frame.Get<Vector2>("CanvasPosition").Y != 40) throw new Exception("Wheel input must scroll automatically measured content.");
    }

    private static void CheckGlobalOrder()
    {
        var (root, screen) = CreateScreen(); screen.SetGuiProperty("ZIndexBehavior", GuiEnum.From("ZIndexBehavior", "Global"));
        var a = Create("TextButton", screen); a.SetGuiProperty("ZIndex", 5d);
        var b = Create("TextButton", screen); b.SetGuiProperty("ZIndex", 2d);
        var nested = Create("TextButton", b); nested.SetGuiProperty("ZIndex", 10d);
        var clicked = ""; a.GetGuiSignal("Activated").Connect(_ => clicked = "A"); nested.GetGuiSignal("Activated").Connect(_ => clicked = "Nested");
        var scene = new GuiScene(); scene.Capture(root, 640, 480); scene.Pointer(10, 10, true); scene.Pointer(10, 10, false);
        if (clicked != "Nested") throw new Exception("Global ordering must allow a nested higher ZIndex above its uncle.");
    }
}
