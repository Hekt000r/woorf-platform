using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Gui;

static class GuiTextTests
{
    internal static int Run()
    {
        try
        {
            CheckRichText();
            CheckTruncation();
            CheckGraphemes();
            Console.WriteLine("PASS GUI text formats rich spans truncates overflow and preserves graphemes");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL GUI text: " + error.Message); return 1; }
    }

    private static (Instance Root, GuiInstance Label) CreateLabel()
    {
        var root = Instance.Create("Folder");
        var screen = Instance.Create("ScreenGui"); screen.Parent = root;
        var label = (GuiInstance)Instance.Create("TextLabel"); label.Parent = screen;
        label.SetGuiProperty("Size", UDim2.FromOffset(300, 100));
        label.SetGuiProperty("TextSize", 20d);
        return (root, label);
    }

    private static void CheckRichText()
    {
        var (root, label) = CreateLabel();
        label.SetGuiProperty("RichText", true);
        label.SetGuiProperty("Text", "Hi <b>bold</b><br/><font color=\"#ff0000\">red</font>");
        var commands = new GuiScene().Capture(root, 640, 480).Where(item => item.Kind == "Text").ToArray();
        if (label.Get<string>("ContentText") != "Hi bold\nred") throw new Exception("Rich markup must not appear in visible content.");
        if (!commands.Any(item => item.Content == "red" && item.Color == new Color3(1, 0, 0)))
            throw new Exception("Rich font colors must affect their text span.");
        if (!commands.Any(item => item.Content.Contains("bold") && item.GetType().GetProperty("Bold")?.GetValue(item) is true))
            throw new Exception("Rich bold spans must reach the platform text renderer.");
    }

    private static void CheckTruncation()
    {
        var (root, label) = CreateLabel();
        label.SetGuiProperty("Text", "1234567890");
        label.SetGuiProperty("Size", UDim2.FromOffset(48, 20));
        label.SetGuiProperty("TextTruncate", GuiEnum.From("TextTruncate", "AtEnd"));
        var commands = new GuiScene().Capture(root, 640, 480).Where(item => item.Kind == "Text").ToArray();
        if (label.Get<bool>("TextFits") || label.Get<Vector2>("TextBounds").X <= 48)
            throw new Exception("Text bounds must measure overflowing source content and TextFits must report overflow.");
        if (!commands.Any(item => item.Content.EndsWith('…')) || commands.Any(item => item.Content == "1234567890"))
            throw new Exception("AtEnd truncation must emit a visible ellipsis within the label.");
    }

    private static void CheckGraphemes()
    {
        var (root, label) = CreateLabel();
        label.SetGuiProperty("Text", "A😀B");
        label.SetGuiProperty("MaxVisibleGraphemes", 2d);
        var text = string.Concat(new GuiScene().Capture(root, 640, 480).Where(item => item.Kind == "Text").Select(item => item.Content));
        if (text != "A😀") throw new Exception("MaxVisibleGraphemes must hide whole text elements without splitting emoji.");
        if (label.Get<string>("ContentText") != "A😀B") throw new Exception("Grapheme visibility must not rewrite ContentText.");
    }
}
