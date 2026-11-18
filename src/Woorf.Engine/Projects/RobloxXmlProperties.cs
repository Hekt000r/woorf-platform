using System.Globalization;
using System.Xml.Linq;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Networking;

namespace Woorf.Engine.Projects;

internal static class RobloxXmlProperties
{
    internal static WireValue? Convert(XElement element, string name, Instance instance,
        IReadOnlyDictionary<string, int> references)
    {
        if (element.Name.LocalName == "Ref")
            return new("instance", references.GetValueOrDefault(element.Value.Trim()));
        var guiDefault = instance is GuiInstance gui && gui.HasGuiProperty(name) ? gui.GetGuiProperty(name) : null;
        object? value = element.Name.LocalName switch
        {
            "bool" => bool.Parse(element.Value),
            "int" or "BrickColor" when name is "BrickColor" or "TeamColor" || instance.ClassName == "BrickColorValue" =>
                new BrickColor(int.Parse(element.Value, CultureInfo.InvariantCulture)),
            "int" or "int64" or "float" or "double" => Number(element.Value),
            "string" or "ProtectedString" => element.Value,
            "Content" or "ContentId" => element.Element("url")?.Value ?? "",
            "Vector3" => new Vector3(Field(element, "X"), Field(element, "Y"), Field(element, "Z")),
            "Vector2" => new Vector2(Field(element, "X"), Field(element, "Y")),
            "CoordinateFrame" => ReadFrame(element),
            "OptionalCoordinateFrame" => element.Element("CFrame") is { } frame ? ReadFrame(frame) : null,
            "Color3" => new Color3(Field(element, "R"), Field(element, "G"), Field(element, "B")),
            "Color3uint8" => ReadColor(element.Value),
            "ColorSequence" => ReadSequence(element.Value, true),
            "NumberSequence" => ReadSequence(element.Value, false),
            "UDim" => new UDim(Field(element, "S"), Offset(element, "O")),
            "UDim2" => new UDim2(Field(element, "XS"), Offset(element, "XO"), Field(element, "YS"), Offset(element, "YO")),
            "PhysicalProperties" => ReadPhysicalProperties(element),
            "token" => ReadToken(element.Value, guiDefault as GuiEnum, name),
            _ => null
        };
        if (value is null) return null;
        ProjectInstances.SetProperty(instance, name, value);
        return WireValue.Encode(value, _ => 0);
    }

    private static object? ReadToken(string text, GuiEnum? guiEnum, string property)
    {
        var token = int.Parse(text, CultureInfo.InvariantCulture);
        if (property == "Shape" && token is not (0 or 1)) return null;
        if (guiEnum is null) return (double)token;
        var index = GuiEnums.FindIndex(guiEnum.EnumType, token);
        return index < 0 ? null : GuiEnum.From(guiEnum.EnumType, GuiEnums.Definitions[guiEnum.EnumType][index]);
    }

    private static PhysicalProperties? ReadPhysicalProperties(XElement element) =>
        element.Element("CustomPhysics")?.Value == "true"
            ? new PhysicalProperties(Field(element, "Density"), Field(element, "Friction"), Field(element, "Elasticity"),
                Field(element, "FrictionWeight"), Field(element, "ElasticityWeight"))
            : null;

    private static CFrame ReadFrame(XElement value) => new(
        new(Field(value, "X"), Field(value, "Y"), Field(value, "Z")),
        new(Field(value, "R00"), Field(value, "R10"), Field(value, "R20")),
        new(Field(value, "R01"), Field(value, "R11"), Field(value, "R21")),
        new(Field(value, "R02"), Field(value, "R12"), Field(value, "R22")));

    private static Color3 ReadColor(string text)
    {
        var color = uint.Parse(text, CultureInfo.InvariantCulture);
        return new((color >> 16 & 255) / 255f, (color >> 8 & 255) / 255f, (color & 255) / 255f);
    }

    private static SequenceValue ReadSequence(string text, bool isColor)
    {
        var values = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var stride = isColor ? 5 : 3;
        if (values.Length % stride != 0) throw new FormatException("Invalid sequence keypoints.");
        var components = values.Where((_, index) => !isColor || index % stride != 4)
            .Select(value => (float)Number(value)).ToArray();
        var kind = isColor ? 2 : 3;
        return SequenceValue.HasValidComponents(kind, components) ? new(kind, components)
            : throw new FormatException("Invalid sequence keypoints.");
    }

    private static double Number(string text)
    {
        var number = double.Parse(text, CultureInfo.InvariantCulture);
        if (!double.IsFinite(number)) throw new FormatException("Expected finite number.");
        return number;
    }

    private static float Field(XElement element, string name) => float.Parse(
        element.Element(name)?.Value ?? throw new FormatException($"Missing {name}."),
        CultureInfo.InvariantCulture) is var value && float.IsFinite(value) ? value : throw new FormatException($"Invalid {name}.");

    private static int Offset(XElement element, string name) => checked((int)Field(element, name));
}
