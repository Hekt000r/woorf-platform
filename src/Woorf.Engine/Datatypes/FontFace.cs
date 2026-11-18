namespace Woorf.Engine.Datatypes;

public sealed record FontFace(string Family, int Weight = 400, int Style = 0)
{
    public bool Bold => Weight >= 600;
    internal static string FamilyPath(string name) => $"rbxasset://fonts/families/{name}.json";

    internal static FontFace FromEnum(GuiEnum font)
    {
        if (font.EnumType != "Font" || font.Name == "Unknown") throw new ArgumentException("Expected a known Enum.Font.");
        var name = font.Name;
        var family = name switch
        {
            "Legacy" => "LegacyArial", "ArialBold" => "Arial",
            "SourceSansBold" or "SourceSansLight" or "SourceSansItalic" or "SourceSansSemibold" => "SourceSansPro",
            "SourceSans" => "SourceSansPro",
            "Gotham" or "GothamMedium" or "GothamBold" or "GothamBlack" => "GothamSSm",
            "Antique" => "RomanAntique", "Code" => "Inconsolata", _ => name
        };
        var weight = name switch
        {
            "GothamBlack" => 900, "GothamMedium" => 500, "SourceSansLight" => 300,
            "SourceSansSemibold" or "BuilderSansMedium" => name == "BuilderSansMedium" ? 500 : 600,
            "BuilderSansExtraBold" => 800,
            _ when name.EndsWith("Bold", StringComparison.Ordinal) => 700, _ => 400
        };
        if (name.StartsWith("BuilderSans", StringComparison.Ordinal)) family = "BuilderSans";
        return new(FamilyPath(family), weight, name.EndsWith("Italic", StringComparison.Ordinal) ? 1 : 0);
    }

    internal GuiEnum ToEnum()
    {
        foreach (var name in GuiEnums.Definitions["Font"])
        {
            if (name == "Unknown") continue;
            var item = GuiEnum.From("Font", name);
            if (FromEnum(item) == this) return item;
        }
        return GuiEnum.From("Font", "Unknown");
    }
}
