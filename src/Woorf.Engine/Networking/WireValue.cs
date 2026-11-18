using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Scripting;

namespace Woorf.Engine.Networking;

internal sealed record WireValue(string Kind, double Number = 0, string? Text = null,
    bool Boolean = false, WireValue[]? Items = null, float[]? Components = null)
{
    internal static WireValue Encode(object? value, Func<Instance, int> identify, int depth = 0)
    {
        if (depth > 64) throw new InvalidDataException("Remote values are too deeply nested.");
        return value switch
        {
            null => new("nil"),
            bool boolean => new("bool", Boolean: boolean),
            string text => new("string", Text: text),
            double number => new("number", number),
            int number => new("number", number),
            long number => new("number", number),
            Instance instance => new("instance", identify(instance)),
            Vector3 vector => new("vector", Components: [vector.X, vector.Y, vector.Z]),
            PhysicalProperties material => new("physical", Components: material.GetComponents()),
            Color3 color => new("color", Components: [color.R, color.G, color.B]),
            Vector2 vector => new("vector2", Components: [vector.X, vector.Y]),
            UDim dimension => new("udim", Number: dimension.Offset, Components: [dimension.Scale]),
            UDim2 dimensions => new("udim2", Items: [Encode(dimensions.X, identify), Encode(dimensions.Y, identify)]),
            GuiEnum item => new("gui-enum", Number: item.Value, Text: item.EnumType),
            CFrame frame => new("frame", Components: frame.GetComponents()),
            LuaTableValue table => new("table", Items: table.Entries.SelectMany(entry => new[]
            {
                Encode(entry.Key, identify, depth + 1), Encode(entry.Value, identify, depth + 1),
            }).ToArray()),
            _ => throw new InvalidDataException("Unsupported wire value."),
        };
    }

    internal object? Decode(Func<int, Instance?> find, int depth = 0)
    {
        if (depth > 64) throw new InvalidDataException("Remote values are too deeply nested.");
        return Kind switch
        {
            "nil" => null,
            "bool" => Boolean,
            "number" => Number,
            "string" => Text ?? "",
            "instance" when Number >= 0 && Number <= int.MaxValue && Number == Math.Truncate(Number) => find((int)Number),
            "vector" when Components?.Length == 3 => new Vector3(Components[0], Components[1], Components[2]),
            "color" when Components?.Length == 3 => new Color3(Components[0], Components[1], Components[2]),
            "vector2" when Components?.Length == 2 => new Vector2(Components[0], Components[1]),
            "udim" when Components?.Length == 1 && Number >= int.MinValue && Number <= int.MaxValue && Number == Math.Truncate(Number) => new UDim(Components[0], (int)Number),
            "udim2" when Items?.Length == 2 && Items[0].Decode(find) is UDim x && Items[1].Decode(find) is UDim y => new UDim2(x, y),
            "gui-enum" when Text is not null && GuiEnums.Definitions.TryGetValue(Text, out var names) && Number >= 0 && Number <= int.MaxValue
                && Number == Math.Truncate(Number) && GuiEnums.FindIndex(Text, (int)Number) is var item && item >= 0 => GuiEnum.From(Text, names[item]),
            "physical" when Components?.Length == 5 => PhysicalProperties.FromComponents(Components),
            "frame" when Components?.Length == 12 => CFrame.FromComponents(Components),
            "table" => DecodeTable(find, depth),
            _ => throw new InvalidDataException("Invalid wire value."),
        };
    }

    private LuaTableValue DecodeTable(Func<int, Instance?> find, int depth)
    {
        if (Items is null || Items.Length % 2 != 0 || Items.Any(item => item is null)) throw new InvalidDataException("Invalid wire table.");
        var entries = new List<KeyValuePair<object, object?>>();
        for (var index = 0; index < Items.Length; index += 2)
        {
            var key = Items[index].Decode(find, depth + 1);
            if (key is not string && key is not double) throw new InvalidDataException("Invalid wire table key.");
            entries.Add(new(key, Items[index + 1].Decode(find, depth + 1)));
        }
        return new(entries);
    }
}
