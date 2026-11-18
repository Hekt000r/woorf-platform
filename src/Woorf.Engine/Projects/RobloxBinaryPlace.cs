using System.Text;
using System.Xml.Linq;

namespace Woorf.Engine.Projects;

internal sealed class RobloxBinaryPlace
{
    private const int MaxInstances = 1_000_000;
    private const int MaxBytes = 128 * 1024 * 1024;
    private readonly Dictionary<int, int[]> classes = [];
    private readonly Dictionary<int, XElement> instances = [];
    private (int[] Children, int[] Parents)? relationships;
    private int decodedBytes;

    internal static XElement Read(Stream stream)
    {
        using var source = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        if (!Exact(source, 8).AsSpan().SequenceEqual("<roblox!"u8) ||
            !Exact(source, 6).AsSpan().SequenceEqual(new byte[] { 0x89, 0xff, 13, 10, 26, 10 }) ||
            source.ReadUInt16() != 0)
            throw new InvalidDataException("Unsupported Roblox binary place header.");
        var classCount = Count(source, MaxInstances);
        var instanceCount = Count(source, MaxInstances);
        Exact(source, 8);
        var decoder = new RobloxBinaryPlace();
        decoder.ReadChunks(source);
        if (decoder.classes.Count != classCount || decoder.instances.Count != instanceCount)
            throw new InvalidDataException("Roblox header counts do not match the instance chunks.");
        return decoder.BuildTree();
    }

    private void ReadChunks(BinaryReader source)
    {
        while (true)
        {
            var name = Encoding.ASCII.GetString(Exact(source, 4)).TrimEnd('\0');
            var compressed = Count(source, MaxBytes);
            var size = Count(source, MaxBytes);
            source.ReadUInt32();
            if (size > MaxBytes - decodedBytes) throw new InvalidDataException("Roblox place exceeds the decoded size limit.");
            decodedBytes += Math.Max(1, size);
            var bytes = Exact(source, compressed == 0 ? size : compressed);
            if (name is not ("INST" or "PROP" or "PRNT" or "END")) continue;
            if (compressed != 0) bytes = RobloxLz4.Decode(bytes, size);
            using var chunk = new BinaryReader(new MemoryStream(bytes, writable: false));
            switch (name)
            {
                case "INST": ReadInstances(chunk); break;
                case "PROP": ReadProperties(chunk); break;
                case "PRNT": ReadParents(chunk); break;
                case "END":
                    if (!bytes.AsSpan().SequenceEqual("</roblox>"u8)) throw new InvalidDataException("Invalid Roblox END chunk.");
                    return;
            }
        }
    }

    private void ReadInstances(BinaryReader chunk)
    {
        var classId = chunk.ReadInt32();
        var name = ReadString(chunk);
        if (chunk.ReadByte() > 1) throw new InvalidDataException("Unsupported Roblox instance format.");
        var count = Count(chunk, MaxInstances - instances.Count);
        var ids = ReadReferents(chunk, count);
        if (!classes.TryAdd(classId, ids)) throw new InvalidDataException("Duplicate Roblox class ID.");
        foreach (var id in ids)
        {
            var item = new XElement("Item", new XAttribute("class", name), new XAttribute("referent", id), new XElement("Properties"));
            if (id < 0 || !instances.TryAdd(id, item)) throw new InvalidDataException("Invalid or duplicate Roblox referent.");
        }
    }

    private void ReadProperties(BinaryReader chunk)
    {
        var classId = chunk.ReadInt32();
        if (!classes.TryGetValue(classId, out var ids)) throw new InvalidDataException("Unknown Roblox property class ID.");
        var name = ReadString(chunk);
        if (chunk.BaseStream.Position == chunk.BaseStream.Length) return;
        var type = chunk.ReadByte();
        var values = RobloxBinaryProperties.Read(chunk, type, ids.Length);
        for (var index = 0; index < ids.Length; index++)
        {
            var value = values?[index] ?? new XElement("Unsupported");
            value.SetAttributeValue("name", name);
            instances[ids[index]].Element("Properties")!.Add(value);
        }
    }

    private void ReadParents(BinaryReader chunk)
    {
        if (relationships is not null || chunk.ReadByte() != 0)
            throw new InvalidDataException("Invalid Roblox parent chunk.");
        var count = Count(chunk, MaxInstances);
        relationships = (ReadReferents(chunk, count), ReadReferents(chunk, count));
    }

    private XElement BuildTree()
    {
        if (relationships is not { } links || links.Children.Length != instances.Count)
            throw new InvalidDataException("Missing or incomplete Roblox parent chunk.");
        var childrenByParent = new Dictionary<int, List<int>>();
        var assigned = new HashSet<int>();
        foreach (var (child, parent) in links.Children.Zip(links.Parents))
        {
            if (!instances.ContainsKey(child) || !assigned.Add(child) || parent != -1 && !instances.ContainsKey(parent))
                throw new InvalidDataException("Invalid Roblox parent relationship.");
            if (!childrenByParent.TryGetValue(parent, out var children)) childrenByParent[parent] = children = [];
            children.Add(child);
        }
        var visited = 0;
        void AddChildren(XElement parent, int id, int depth)
        {
            if (depth > 121) throw new InvalidDataException("Roblox hierarchy exceeds the importer depth limit.");
            if (!childrenByParent.TryGetValue(id, out var children)) return;
            foreach (var child in children)
            {
                visited++;
                parent.Add(instances[child]);
                AddChildren(instances[child], child, depth + 1);
            }
        }
        var root = new XElement("roblox");
        AddChildren(root, -1, 0);
        if (visited != instances.Count) throw new InvalidDataException("Roblox hierarchy contains a parent cycle.");
        return root;
    }

    internal static int Count(BinaryReader reader, int maximum)
    {
        var count = reader.ReadInt32();
        return count >= 0 && count <= maximum ? count : throw new InvalidDataException("Roblox count exceeds the supported range.");
    }

    internal static byte[] Exact(BinaryReader reader, int count)
    {
        if (count > reader.BaseStream.Length - reader.BaseStream.Position)
            throw new EndOfStreamException("Truncated Roblox binary data.");
        var bytes = reader.ReadBytes(count);
        return bytes.Length == count ? bytes : throw new EndOfStreamException("Truncated Roblox binary data.");
    }

    internal static string ReadString(BinaryReader reader) => Encoding.UTF8.GetString(Exact(reader, Count(reader, MaxBytes)));

    internal static ulong[] ReadColumns(BinaryReader reader, int count, int stride = 4)
    {
        var bytes = Exact(reader, checked(count * stride));
        var values = new ulong[count];
        for (var column = 0; column < stride; column++)
            for (var index = 0; index < count; index++)
                values[index] = values[index] << 8 | bytes[column * count + index];
        return values;
    }

    internal static long DecodeSigned(ulong value) => (long)(value >> 1) ^ -(long)(value & 1);

    internal static int[] ReadReferents(BinaryReader reader, int count)
    {
        var value = 0;
        return ReadColumns(reader, count).Select(field => value = checked(value + (int)DecodeSigned(field))).ToArray();
    }
}
