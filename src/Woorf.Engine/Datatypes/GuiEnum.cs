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
        ["SortOrder"] = ["Name", "Custom", "LayoutOrder"],
        ["HorizontalAlignment"] = ["Center", "Left", "Right"],
        ["VerticalAlignment"] = ["Center", "Top", "Bottom"],
        ["AutomaticSize"] = ["None", "X", "Y", "XY"],
        ["ScaleType"] = ["Stretch", "Slice", "Tile", "Fit", "Crop"],
        ["Font"] = ["Legacy", "Arial", "ArialBold", "SourceSans", "SourceSansBold", "SourceSansLight", "SourceSansItalic", "Bodoni", "Garamond", "Cartoon", "Code", "Highway", "SciFi", "Arcade", "Fantasy", "Antique", "SourceSansSemibold", "Gotham", "GothamMedium", "GothamBold", "GothamBlack"],
        ["ApplyStrokeMode"] = ["Contextual", "Border"],
        ["LineJoinMode"] = ["Round", "Bevel", "Miter"],
        ["ScrollingDirection"] = ["X", "Y", "XY"],
        ["SizeConstraint"] = ["RelativeXY", "RelativeXX", "RelativeYY"],
        ["AspectType"] = ["FitWithinMaxSize", "ScaleWithParentSize"],
        ["DominantAxis"] = ["Width", "Height"],
        ["TextTruncate"] = ["None", "AtEnd", "SplitWord"],
        ["BorderMode"] = ["Outline", "Middle", "Inset"],
        ["UserInputType"] = ["MouseButton1", "MouseButton2", "MouseButton3", "MouseWheel", "MouseMovement", "Keyboard", "Focus", "Touch"],
        ["UserInputState"] = ["Begin", "Change", "End", "Cancel", "None"],
        ["KeyCode"] = ["Unknown","Space","A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"],
    };
    internal static string[] Types => Definitions.Keys.ToArray();
    internal static int NumericValue(string type, int index) => type == "UserInputType"
        ? new[] { 0, 1, 2, 3, 4, 8, 9, 7 }[index] : type=="KeyCode" ? index==0?0:index==1?32:95+index : index;
    internal static int FindIndex(string type, int value) => Enumerable.Range(0, Definitions[type].Length)
        .FirstOrDefault(index => NumericValue(type, index) == value, -1);
    internal static string GetInitializationSource() => "_woorf_gui_enum_definitions = {" + string.Join(',', Definitions.Select(entry =>
        "{\"" + entry.Key + "\",{" + string.Join(',', entry.Value.Select(value => "\"" + value + "\"")) + "},{"
            + string.Join(',', Enumerable.Range(0, entry.Value.Length).Select(index => NumericValue(entry.Key, index))) + "}}")) + "}";
}
