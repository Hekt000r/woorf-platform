using System.Xml.Linq;
using Woorf.Engine.Datatypes;
using static Woorf.Engine.Projects.RobloxBinaryPlace;

namespace Woorf.Engine.Projects;

internal static class RobloxBinaryProperties
{
    internal static XElement[]? Read(BinaryReader reader, byte type, int count) => type switch
    {
        1 => Each(count, _ => new("string", ReadString(reader))),
        2 => Each(count, _ => new("bool", reader.ReadByte() != 0)),
        3 => ReadColumns(reader, count).Select(value => new XElement("int", DecodeSigned(value))).ToArray(),
        4 => Floats(reader, count).Select(value => new XElement("float", value)).ToArray(),
        5 => Each(count, _ => new("double", reader.ReadDouble())),
        6 => Dimensions(reader, count, false),
        7 => Dimensions(reader, count, true),
        0x0b => ReadColumns(reader, count).Select(value => new XElement("BrickColor", value)).ToArray(),
        0x0c => Components(reader, count, "Color3", ["R", "G", "B"]),
        0x0d => Components(reader, count, "Vector2", ["X", "Y"]),
        0x0e => Components(reader, count, "Vector3", ["X", "Y", "Z"]),
        0x10 => Frames(reader, count),
        0x12 => ReadColumns(reader, count).Select(value => new XElement("token", value)).ToArray(),
        0x13 => ReadReferents(reader, count).Select(value => new XElement("Ref", value)).ToArray(),
        0x15 => Each(count, _ => ReadSequence(reader, false)),
        0x16 => Each(count, _ => ReadSequence(reader, true)),
        0x19 => Each(count, _ => Physical(reader)),
        0x1a => PackedColors(reader, count),
        0x1b => ReadColumns(reader, count, 8).Select(value => new XElement("int64", DecodeSigned(value))).ToArray(),
        0x1e => OptionalFrames(reader, count),
        _ => null
    };

    private static XElement ReadSequence(BinaryReader reader, bool isColor)
    {
        var keypoints = reader.ReadUInt32();
        if (keypoints > 20) throw new InvalidDataException("Invalid Roblox sequence size.");
        var stride = isColor ? 5 : 3;
        var values = new string[keypoints * stride];
        for (var i = 0; i < values.Length; i++)
            values[i] = System.Xml.XmlConvert.ToString(reader.ReadSingle());
        return new(isColor ? "ColorSequence" : "NumberSequence", string.Join(' ', values));
    }

    private static XElement[] Each(int count, Func<int, XElement> read)
    {
        var values = new XElement[count];
        for (var index = 0; index < count; index++) values[index] = read(index);
        return values;
    }

    private static float[] Floats(BinaryReader reader, int count) =>
        ReadColumns(reader, count).Select(bits => BitConverter.UInt32BitsToSingle((uint)(bits >> 1 | (bits & 1) << 31))).ToArray();

    private static XElement[] Components(BinaryReader reader, int count, string type, string[] names)
    {
        var columns = names.Select(_ => Floats(reader, count)).ToArray();
        return Each(count, index => new XElement(type, names.Select((name, column) => new XElement(name, columns[column][index]))));
    }

    private static XElement[] Dimensions(BinaryReader reader, int count, bool isPair)
    {
        var xScale = Floats(reader, count);
        var yScale = isPair ? Floats(reader, count) : null;
        var xOffset = ReadColumns(reader, count);
        var yOffset = isPair ? ReadColumns(reader, count) : null;
        return Each(count, index => isPair
            ? new("UDim2", new XElement("XS", xScale[index]), new XElement("XO", DecodeSigned(xOffset[index])),
                new XElement("YS", yScale![index]), new XElement("YO", DecodeSigned(yOffset![index])))
            : new("UDim", new XElement("S", xScale[index]), new XElement("O", DecodeSigned(xOffset[index]))));
    }

    private static XElement[] Frames(BinaryReader reader, int count)
    {
        var rotations = new float[count][];
        for (var index = 0; index < count; index++)
        {
            var code = reader.ReadByte();
            rotations[index] = code == 0
                ? Enumerable.Range(0, 9).Select(_ => reader.ReadSingle()).ToArray()
                : Rotation(code);
        }
        var positions = Components(reader, count, "CoordinateFrame", ["X", "Y", "Z"]);
        for (var index = 0; index < count; index++)
            for (var component = 0; component < 9; component++)
                positions[index].Add(new XElement($"R{component / 3}{component % 3}", rotations[index][component]));
        return positions;
    }

    private static readonly Vector3[] Normals = [new(1,0,0), new(0,1,0), new(0,0,1), new(-1,0,0), new(0,-1,0), new(0,0,-1)];

    private static float[] Rotation(int code)
    {
        if (code is < 1 or > 36) throw new InvalidDataException("Invalid Roblox CFrame rotation.");
        var right = Normals[(code - 1) / 6];
        var up = Normals[(code - 1) % 6];
        if (right.Dot(up) != 0) throw new InvalidDataException("Invalid Roblox CFrame axes.");
        var back = new Vector3(right.Y * up.Z - right.Z * up.Y, right.Z * up.X - right.X * up.Z, right.X * up.Y - right.Y * up.X);
        return [right.X, up.X, back.X, right.Y, up.Y, back.Y, right.Z, up.Z, back.Z];
    }

    private static XElement[] OptionalFrames(BinaryReader reader, int count)
    {
        if (reader.ReadByte() != 0x10) throw new InvalidDataException("Invalid optional Roblox CFrame.");
        var frames = Frames(reader, count);
        if (reader.ReadByte() != 2) throw new InvalidDataException("Invalid optional Roblox CFrame validity array.");
        return Each(count, index => reader.ReadByte() == 0 ? new("OptionalCoordinateFrame") :
            new("OptionalCoordinateFrame", new XElement("CFrame", frames[index].Elements())));
    }

    private static XElement Physical(BinaryReader reader)
    {
        var flags = reader.ReadByte();
        var property = new XElement("PhysicalProperties", new XElement("CustomPhysics", (flags & 1) != 0));
        if ((flags & 1) == 0) return property;
        foreach (var name in new[] { "Density", "Friction", "Elasticity", "FrictionWeight", "ElasticityWeight" })
            property.Add(new XElement(name, reader.ReadSingle()));
        if ((flags & 2) != 0) reader.ReadSingle();
        return property;
    }

    private static XElement[] PackedColors(BinaryReader reader, int count)
    {
        var bytes = Exact(reader, checked(count * 3));
        return Each(count, index => new("Color3uint8",
            bytes[index] << 16 | bytes[index + count] << 8 | bytes[index + 2 * count]));
    }
}
