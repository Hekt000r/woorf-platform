using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

internal static class GuiDefaults
{
    internal static readonly Dictionary<string, string> Classes = new()
    {
        ["GuiBase"] = "Instance", ["GuiBase2d"] = "GuiBase", ["LayerCollector"] = "GuiBase2d",
        ["ScreenGui"] = "LayerCollector", ["GuiObject"] = "GuiBase2d", ["GuiButton"] = "GuiObject",
        ["Frame"] = "GuiObject", ["CanvasGroup"] = "GuiObject", ["ScrollingFrame"] = "GuiObject",
        ["TextLabel"] = "GuiObject", ["TextBox"] = "GuiObject", ["TextButton"] = "GuiButton",
        ["ImageLabel"] = "GuiObject", ["ImageButton"] = "GuiButton", ["UIBase"] = "Instance",
        ["UIComponent"] = "UIBase", ["UIConstraint"] = "UIComponent", ["UILayout"] = "UIComponent",
        ["UIGridStyleLayout"] = "UILayout", ["UIListLayout"] = "UIGridStyleLayout", ["UIGridLayout"] = "UIGridStyleLayout",
        ["UIPadding"] = "UIComponent", ["UICorner"] = "UIComponent", ["UIStroke"] = "UIComponent", ["UIScale"] = "UIComponent",
        ["UIAspectRatioConstraint"] = "UIConstraint", ["UISizeConstraint"] = "UIConstraint", ["UITextSizeConstraint"] = "UIConstraint",
    };
    internal static bool IsCreatable(string name) => Classes.ContainsKey(name) && name is not
        ("GuiBase" or "GuiBase2d" or "GuiObject" or "GuiButton" or "LayerCollector" or "UIBase" or "UIComponent" or "UIConstraint" or "UILayout" or "UIGridStyleLayout");
    private static GuiEnum E(string type, string name) => GuiEnum.From(type, name);

    internal static Dictionary<string, object> Create(string className)
    {
        var values = new Dictionary<string, object>();
        if (className == "ScreenGui") AddScreen(values);
        if (ClassCatalog.IsA(className, "GuiObject")) AddObject(values, className);
        if (className is "TextLabel" or "TextButton" or "TextBox") AddText(values, className);
        if (className is "ImageLabel" or "ImageButton") AddImage(values);
        if (className == "ScrollingFrame") AddScrolling(values);
        if (className == "CanvasGroup") { values["GroupTransparency"] = 0d; values["GroupColor3"] = new Color3(1, 1, 1); }
        AddComponent(values, className);
        return values;
    }

    private static void AddScreen(Dictionary<string, object> p)
    {
        p["Enabled"] = true; p["ResetOnSpawn"] = true; p["DisplayOrder"] = 0d;
        p["IgnoreGuiInset"] = false; p["ZIndexBehavior"] = E("ZIndexBehavior", "Sibling");
        p["AbsolutePosition"] = default(Vector2); p["AbsoluteSize"] = default(Vector2); p["AbsoluteRotation"] = 0d;
    }

    private static void AddObject(Dictionary<string, object> p, string name)
    {
        p["Position"] = default(UDim2); p["Size"] = new UDim2(0, 100, 0, 100); p["AnchorPoint"] = default(Vector2);
        p["Visible"] = true; p["Active"] = name is "TextButton" or "ImageButton" or "TextBox" or "ScrollingFrame";
        p["Interactable"] = true; p["ZIndex"] = 1d; p["LayoutOrder"] = 0d; p["Rotation"] = 0d;
        p["ClipsDescendants"] = name == "ScrollingFrame";
        p["BackgroundColor3"] = new Color3(1, 1, 1); p["BackgroundTransparency"] = 0d;
        p["BorderColor3"] = new Color3(0.106f, 0.165f, 0.208f); p["BorderSizePixel"] = 1d;
        p["BorderMode"] = E("BorderMode", "Outline"); p["AutomaticSize"] = E("AutomaticSize", "None");
        p["SizeConstraint"] = E("SizeConstraint", "RelativeXY");
        p["AbsolutePosition"] = default(Vector2); p["AbsoluteSize"] = default(Vector2); p["AbsoluteRotation"] = 0d;
        if (name is "TextButton" or "ImageButton") { p["AutoButtonColor"] = true; p["Selected"] = false; p["Modal"] = false; }
    }

    private static void AddText(Dictionary<string, object> p, string name)
    {
        p["Text"] = name == "TextButton" ? "Button" : name == "TextLabel" ? "Label" : "";
        p["ContentText"] = p["Text"]; p["TextColor3"] = new Color3(0, 0, 0); p["TextTransparency"] = 0d;
        p["TextSize"] = 14d; p["TextScaled"] = false; p["TextWrapped"] = false;
        p["TextXAlignment"] = E("TextXAlignment", "Center"); p["TextYAlignment"] = E("TextYAlignment", "Center");
        p["Font"] = E("Font", "Legacy"); p["TextBounds"] = default(Vector2); p["TextFits"] = true;
        p["RichText"] = false; p["LineHeight"] = 1d; p["MaxVisibleGraphemes"] = -1d;
        p["TextStrokeColor3"] = new Color3(0, 0, 0); p["TextStrokeTransparency"] = 1d;
        p["TextTruncate"] = E("TextTruncate", "None");
        if (name != "TextBox") return;
        p["ClearTextOnFocus"] = true; p["MultiLine"] = false; p["TextEditable"] = true;
        p["PlaceholderText"] = ""; p["PlaceholderColor3"] = new Color3(0.7f, 0.7f, 0.7f);
        p["CursorPosition"] = -1d; p["SelectionStart"] = -1d;
    }

    private static void AddImage(Dictionary<string, object> p)
    {
        p["Image"] = ""; p["ImageColor3"] = new Color3(1, 1, 1); p["ImageTransparency"] = 0d;
        p["ImageRectOffset"] = default(Vector2); p["ImageRectSize"] = default(Vector2);
        p["ScaleType"] = E("ScaleType", "Stretch"); p["TileSize"] = UDim2.FromScale(1, 1);
    }

    private static void AddScrolling(Dictionary<string, object> p)
    {
        p["CanvasSize"] = UDim2.FromScale(0, 2); p["CanvasPosition"] = default(Vector2);
        p["ScrollingEnabled"] = true; p["ScrollingDirection"] = E("ScrollingDirection", "XY");
        p["ScrollBarThickness"] = 12d; p["ScrollBarImageColor3"] = new Color3(0, 0, 0); p["ScrollBarImageTransparency"] = 0d;
        p["AutomaticCanvasSize"] = E("AutomaticSize", "None"); p["AbsoluteCanvasSize"] = default(Vector2); p["AbsoluteWindowSize"] = default(Vector2);
    }

    private static void AddComponent(Dictionary<string, object> p, string name)
    {
        if (name is "UIListLayout" or "UIGridLayout")
        {
            p["FillDirection"] = E("FillDirection", name == "UIGridLayout" ? "Horizontal" : "Vertical"); p["SortOrder"] = E("SortOrder", "Name");
            p["HorizontalAlignment"] = E("HorizontalAlignment", "Left"); p["VerticalAlignment"] = E("VerticalAlignment", "Top");
            p["AbsoluteContentSize"] = default(Vector2);
            if (name == "UIListLayout") p["Padding"] = default(UDim);
            else { p["CellSize"] = UDim2.FromOffset(100, 100); p["CellPadding"] = UDim2.FromOffset(5, 5); p["FillDirectionMaxCells"] = 0d; }
        }
        if (name == "UIPadding") foreach (var side in new[] { "Top", "Bottom", "Left", "Right" }) p["Padding" + side] = default(UDim);
        if (name == "UICorner") p["CornerRadius"] = new UDim(0, 8);
        if (name == "UIScale") p["Scale"] = 1d;
        if (name == "UIStroke")
        {
            p["Enabled"] = true; p["Color"] = new Color3(0, 0, 0); p["Thickness"] = 1d; p["Transparency"] = 0d;
            p["ApplyStrokeMode"] = E("ApplyStrokeMode", "Contextual"); p["LineJoinMode"] = E("LineJoinMode", "Round");
        }
        if (name == "UIAspectRatioConstraint")
        {
            p["AspectRatio"] = 1d; p["AspectType"] = E("AspectType", "FitWithinMaxSize"); p["DominantAxis"] = E("DominantAxis", "Width");
        }
        if (name == "UISizeConstraint") { p["MinSize"] = default(Vector2); p["MaxSize"] = new Vector2(float.MaxValue, float.MaxValue); }
        if (name == "UITextSizeConstraint") { p["MinTextSize"] = 1d; p["MaxTextSize"] = 100d; }
    }

    internal static object Validate(string name, object previous, object value)
    {
        if (name is "Modal" or "Selected" && value is true
            || name == "ScaleType" && value is GuiEnum { Name: "Slice" }
            || name == "LineJoinMode" && value is GuiEnum { Name: not "Round" })
            throw new NotSupportedException($"{name} mode is not supported by this GUI renderer.");
        if (previous is double)
        {
            var number = value switch { double d => d, int i => i, float f => f, _ => throw new InvalidOperationException($"{name} expects number.") };
            if (!double.IsFinite(number)) throw new InvalidOperationException($"{name} expects finite number.");
            if (name.EndsWith("Transparency", StringComparison.Ordinal)) return Math.Clamp(number, 0, 1);
            if (name is "Thickness" or "TextSize" or "Scale" or "AspectRatio" or "BorderSizePixel" or "ScrollBarThickness" or "LineHeight" or "MinTextSize" or "MaxTextSize" && number < 0)
                throw new InvalidOperationException($"{name} cannot be negative.");
            return number;
        }
        if (value is null || value.GetType() != previous.GetType()) throw new InvalidOperationException($"Invalid type for {name}.");
        if (previous is GuiEnum expected && value is GuiEnum actual && expected.EnumType != actual.EnumType)
            throw new InvalidOperationException($"{name} expects Enum.{expected.EnumType}.");
        if (value is Vector2 vector && (!float.IsFinite(vector.X) || !float.IsFinite(vector.Y))) throw new InvalidOperationException($"{name} expects finite Vector2.");
        if (value is Color3 color && (!float.IsFinite(color.R) || !float.IsFinite(color.G) || !float.IsFinite(color.B)))
            throw new InvalidOperationException($"{name} expects finite Color3.");
        if (value is UDim dim && !float.IsFinite(dim.Scale) || value is UDim2 dimensions && (!float.IsFinite(dimensions.X.Scale) || !float.IsFinite(dimensions.Y.Scale)))
            throw new InvalidOperationException($"{name} expects finite dimensions.");
        return value;
    }
}
