namespace Woorf.Engine.Datatypes;

public sealed record GuiEnum(string EnumType, string Name, int Value)
{
    public static GuiEnum From(string type, string name)
    {
        var names = GuiEnums.Definitions.GetValueOrDefault(type)
            ?? throw new InvalidOperationException($"Unsupported Enum.{type}.");
        var index = Array.IndexOf(names, name);
        if (index < 0) throw new InvalidOperationException($"Unsupported Enum.{type}.{name}.");
        return new(type, name, GuiEnums.NumericValue(type, index));
    }
}

internal static class GuiEnums
{
    internal static readonly Dictionary<string, string[]> Definitions = new()
    {
        ["TextXAlignment"] = ["Left", "Right", "Center"],
        ["TextYAlignment"] = ["Top", "Center", "Bottom"],
        ["ZIndexBehavior"] = ["Global", "Sibling"],
        ["FillDirection"] = ["Horizontal", "Vertical"],
        ["UIFlexAlignment"] = ["None", "Fill", "SpaceAround", "SpaceBetween", "SpaceEvenly"],
        ["ItemLineAlignment"] = ["Automatic", "Start", "Center", "End", "Stretch"],
        ["VerticalScrollBarPosition"] = ["Right", "Left"],
        ["SortOrder"] = ["Name", "Custom", "LayoutOrder"],
        ["HorizontalAlignment"] = ["Center", "Left", "Right"],
        ["VerticalAlignment"] = ["Center", "Top", "Bottom"],
        ["AutomaticSize"] = ["None", "X", "Y", "XY"],
        ["ScaleType"] = ["Stretch", "Slice", "Tile", "Fit", "Crop"],
        ["Font"] = ["Legacy", "Arial", "ArialBold", "SourceSans", "SourceSansBold", "SourceSansLight", "SourceSansItalic", "Bodoni", "Garamond", "Cartoon", "Code", "Highway", "SciFi", "Arcade", "Fantasy", "Antique", "SourceSansSemibold", "Gotham", "GothamMedium", "GothamBold", "GothamBlack", "BuilderSans", "BuilderSansMedium", "BuilderSansBold", "BuilderSansExtraBold", "Unknown"],
        ["FontWeight"] = ["Thin", "ExtraLight", "Light", "Regular", "Medium", "SemiBold", "Bold", "ExtraBold", "Heavy"],
        ["FontStyle"] = ["Normal", "Italic"],
        ["CameraType"] = ["Fixed", "Attach", "Watch", "Track", "Follow", "Custom", "Scriptable", "Orbital"],
        ["SurfaceType"] = ["Smooth", "Glue", "Weld", "Studs", "Inlet", "Universal", "Hinge", "Motor", "SteppingMotor", "SmoothNoOutlines"],
        ["ScreenInsets"] = ["None", "DeviceSafeInsets", "CoreUISafeInsets", "TopbarSafeInsets"],
        ["EasingStyle"] = ["Linear", "Sine", "Back", "Quad", "Quart", "Quint", "Bounce", "Elastic", "Exponential", "Circular", "Cubic"],
        ["EasingDirection"] = ["In", "Out", "InOut"],
        ["PlaybackState"] = ["Begin", "Delayed", "Playing", "Paused", "Completed", "Cancelled"],
        ["ApplyStrokeMode"] = ["Contextual", "Border"],
        ["LineJoinMode"] = ["Round", "Bevel", "Miter"],
        ["ScrollingDirection"] = ["X", "Y", "XY"],
        ["ScrollBarInset"] = ["None", "ScrollBar", "Always"],
        ["ElasticBehavior"] = ["WhenScrollable", "Always", "Never"],
        ["SizeConstraint"] = ["RelativeXY", "RelativeXX", "RelativeYY"],
        ["AspectType"] = ["FitWithinMaxSize", "ScaleWithParentSize"],
        ["DominantAxis"] = ["Width", "Height"],
        ["TextTruncate"] = ["None", "AtEnd", "SplitWord"],
        ["BorderMode"] = ["Outline", "Middle", "Inset"],
        ["UserInputType"] = ["MouseButton1", "MouseButton2", "MouseButton3", "MouseWheel", "MouseMovement", "Keyboard", "Focus", "Touch", "Gamepad1", "Gamepad2", "Gamepad3", "Gamepad4", "Gamepad5", "Gamepad6", "Gamepad7", "Gamepad8", "None"],
        ["UserInputState"] = ["Begin", "Change", "End", "Cancel", "None"],
        ["CameraMode"] = ["Classic", "LockFirstPerson"],
        ["CoreGuiType"] = ["PlayerList", "Health", "Backpack", "Chat", "All"],
        ["ContextActionResult"] = ["Sink", "Pass"],
        ["ContextActionPriority"] = ["Low", "Medium", "High"],
        ["KeyCode"] = ["Unknown","Space","A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z","ButtonX","ButtonY","ButtonA","ButtonB","ButtonR1","ButtonL1","ButtonR2","ButtonL2","ButtonR3","ButtonL3","ButtonStart","ButtonSelect","DPadLeft","DPadRight","DPadUp","DPadDown","Backspace","Zero","One","Two","Three","Four","Five","Six","Seven","Eight","Nine","Backquote","Escape","Quote","Semicolon","Thumbstick1"],
    };
    internal static string[] Types => Definitions.Keys.ToArray();
    private static readonly int[] InputTypeValues = [0, 1, 2, 3, 4, 8, 9, 7, 12, 13, 14, 15, 16, 17, 18, 19, 22];
    internal static int NumericValue(string type, int index) => type switch
    {
        "UserInputType" => InputTypeValues[index],
        "KeyCode" => index switch { 0 => 0, 1 => 32, < 28 => 95 + index, < 44 => 1000 + index - 28,
            44 => 8, 55 => 96, 56 => 27, 57 => 39, 58 => 59, 59 => 1017, _ => 48 + index - 45 },
        "FontWeight" => (index + 1) * 100,
        "Font" when index >= 21 => index == 25 ? 100 : 46 + index - 21,
        "ContextActionPriority" => (index + 1) * 1000,
        _ => index
    };
    internal static int FindIndex(string type, int value) => Enumerable.Range(0, Definitions[type].Length)
        .FirstOrDefault(index => NumericValue(type, index) == value, -1);
    internal static string GetInitializationSource() => "_woorf_gui_enum_definitions = {" + string.Join(',', Definitions.Select(entry =>
        "{\"" + entry.Key + "\",{" + string.Join(',', entry.Value.Select(value => "\"" + value + "\"")) + "},{"
            + string.Join(',', Enumerable.Range(0, entry.Value.Length).Select(index => NumericValue(entry.Key, index))) + "}}")) + "}";
}
