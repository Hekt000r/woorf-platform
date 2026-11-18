using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Networking;

namespace Woorf.Engine.Projects;

public sealed record RobloxImportReport(int Instances, int Scripts, IReadOnlyList<string> Warnings);

public static class RobloxPlaceImporter
{
    public static RobloxImportReport Import(string input, string output)
    {
        using var stream = File.OpenRead(input);
        Span<byte> magic = stackalloc byte[8];
        var isBinary = stream.ReadAtLeast(magic, magic.Length, throwOnEndOfStream: false) == magic.Length
            && magic.SequenceEqual("<roblox!"u8);
        if (!isBinary) return ImportXml(input, output);
        stream.Position = 0;
        return ConvertPlace(RobloxBinaryPlace.Read(stream), output, Path.GetFileNameWithoutExtension(input));
    }

    public static RobloxImportReport ImportXml(string input, string output, string? projectName = null)
    {
        using var reader = XmlReader.Create(input, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 128 * 1024 * 1024
        });
        var root = XDocument.Load(reader).Root;
        if (root?.Name != "roblox") throw new InvalidDataException("Expected a Roblox XML place.");
        return ConvertPlace(root, output, projectName ?? Path.GetFileNameWithoutExtension(input));
    }

    private static RobloxImportReport ConvertPlace(XElement root, string output, string projectName)
    {
        var conversion = new Conversion(root);
        var nodes = conversion.ConvertServices();
        var report = conversion.Report();
        WriteProject(output, projectName, nodes, report);
        return report;
    }

    private static void WriteProject(string output, string name, ProjectNode[] nodes, RobloxImportReport report)
    {
        output = Path.TrimEndingDirectorySeparator(Path.GetFullPath(output));
        if (Path.Exists(output)) throw new IOException($"Import output already exists: {output}");
        var parent = Path.GetDirectoryName(output) ?? throw new IOException("Expected an output directory below a filesystem root.");
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, ".woorf-import-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(staging, "src"));
            WriteJson(Path.Combine(staging, "woorf.project.json"), new { name, root = "src" });
            WriteJson(Path.Combine(staging, "src", "instances.json"), nodes);
            WriteJson(Path.Combine(staging, "import-report.json"), report);
            ProjectLoader.Load(staging);
            Directory.Move(staging, output);
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }

    private static void WriteJson<T>(string path, T value)
    {
        using var file = File.Create(path);
        JsonSerializer.Serialize(file, value, ProjectInstances.Json);
    }

    private sealed class Conversion
    {
        private readonly XElement root;
        private readonly Dictionary<XElement, int> ids = [];
        private readonly Dictionary<string, int> references = new(StringComparer.Ordinal);
        private readonly SortedDictionary<string, int> warnings = new(StringComparer.Ordinal);
        private Vector3? spawn;
        private int instanceCount;
        private int scriptCount;

        internal Conversion(XElement root)
        {
            this.root = root;
            foreach (var item in root.Descendants("Item"))
            {
                var id = ids.Count + 1;
                ids.Add(item, id);
                if ((string?)item.Attribute("referent") is { Length: > 0 } reference && !references.TryAdd(reference, id))
                    throw new InvalidDataException($"Duplicate Roblox referent '{reference}'.");
            }
        }

        internal ProjectNode[] ConvertServices()
        {
            var services = new List<ProjectNode>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in root.Elements("Item"))
            {
                var name = ReadClass(item);
                if (!ClassCatalog.IsService(name)) { Warn($"Skipped unsupported top-level service {name}"); continue; }
                if (!names.Add(name)) throw new InvalidDataException($"Duplicate Roblox service '{name}'.");
                services.Add(ConvertItem(item, 0, name));
            }
            return services.ToArray();
        }

        internal RobloxImportReport Report() => new(instanceCount, scriptCount,
            warnings.Select(entry => $"{entry.Key} ({entry.Value})").ToArray());

        private ProjectNode ConvertItem(XElement item, int depth, string? service = null)
        {
            if (depth > 120) throw new InvalidDataException("Roblox hierarchy exceeds the importer depth limit.");
            var original = ReadClass(item);
            var targetClass = service ?? (original == "ChatInputBarConfiguration" && (string?)item.Parent?.Attribute("class") == "TextChatService"
                ? original : MapClass(original));
            var probe = targetClass == "Workspace" ? new Workspace() : Instance.CreateRuntime(targetClass);
            var properties = item.Element("Properties")?.Elements().ToArray() ?? [];
            var name = Find(properties, "Name")?.Value ?? original;
            var sourceProperty = Find(properties, "Source");
            var hasScriptSource = probe.IsA("LuaSourceContainer");
            var source = hasScriptSource && sourceProperty?.Name.LocalName is "string" or "ProtectedString"
                ? sourceProperty.Value : null;
            if (hasScriptSource && source is null) Warn($"Missing readable source for {original}");
            var converted = ConvertProperties(probe, original, properties);
            if (original == "SpawnLocation" && probe is Part part && spawn is null &&
                Find(properties, "Enabled")?.Value != "false")
                spawn = part.Position + new Vector3(0, part.Size.Y / 2 + 3, 0);
            var children = item.Elements("Item").Select(child => ConvertItem(child, depth + 1)).ToArray();
            if (service == "Workspace" && spawn is { } position)
                converted["@CharacterSpawn"] = WireValue.Encode(position, _ => 0);
            instanceCount++;
            if (source is not null) scriptCount++;
            return new(targetClass, name, ids[item], converted, children, source);
        }

        private Dictionary<string, WireValue> ConvertProperties(Instance probe, string original, XElement[] properties)
        {
            var converted = new Dictionary<string, WireValue>(StringComparer.Ordinal);
            foreach (var property in properties)
            {
                var originalName = (string?)property.Attribute("name") ?? "";
                if (originalName is "Name" or "Source") continue;
                var name = originalName switch
                {
                    "size" => "Size",
                    "shape" => "Shape",
                    "Color3uint8" when probe is Part => "Color",
                    "Disabled" when probe is ScriptInstance => "Enabled",
                    _ => originalName
                };
                try
                {
                    var value = originalName == "Disabled" && probe is ScriptInstance
                        ? new WireValue("bool", Boolean: !bool.Parse(property.Value))
                        : ConvertProperty(probe, property, name);
                    if (value is not null) { converted[name] = value; continue; }
                }
                catch (Exception error) when (error is FormatException or ArgumentException or InvalidOperationException or OverflowException or InvalidDataException)
                {
                    // Keep the runtime default when a place value falls outside the supported subset.
                }
                Warn($"Skipped property {original}.{originalName}");
            }
            return converted;
        }

        private WireValue? ConvertProperty(Instance probe, XElement property, string name)
        {
            if (name != "@RenderColor" && !(probe is GuiInstance gui && gui.HasGuiProperty(name)) &&
                ProjectInstances.FindProperty(probe, name) is null) return null;
            if (property.Name == "Ref" && name is not ("PrimaryPart" or "Value" or "Part0" or "Part1")) return null;
            return RobloxXmlProperties.Convert(property, name, probe, references);
        }

        private string MapClass(string name)
        {
            if (name == "StarterPlayerScripts" || (ClassCatalog.IsCreatable(name) && name != "Player")) return name;
            var replacement = name is "SpawnLocation" or "WedgePart" or "CornerWedgePart" or "TrussPart" or "MeshPart" or "UnionOperation"
                ? "Part" : "Folder";
            Warn($"Converted unsupported {name} to {replacement}");
            return replacement;
        }

        private void Warn(string message) => warnings[message] = warnings.GetValueOrDefault(message) + 1;
        private static string ReadClass(XElement item) => (string?)item.Attribute("class")
            ?? throw new InvalidDataException("Roblox Item has no class.");
        private static XElement? Find(XElement[] properties, string name) => properties.LastOrDefault(property => (string?)property.Attribute("name") == name);
    }
}
