using System.Reflection;
using System.Text.Json;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Networking;
using GameDataModel = Woorf.Engine.DataModel.DataModel;

namespace Woorf.Engine.Projects;

internal sealed record ProjectNode(string ClassName, string Name, int Id,
    Dictionary<string, WireValue> Properties, ProjectNode[] Children, string? Source = null);

internal static class ProjectInstances
{
    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        MaxDepth = 512,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
    };

    internal static void Load(string path, GameDataModel game, List<ProjectScript> scripts)
    {
        using var input = File.OpenRead(path);
        var roots = JsonSerializer.Deserialize<ProjectNode[]>(input, Json)
            ?? throw new InvalidDataException("Expected an array of project instances.");
        var instances = new Dictionary<int, Instance>();
        var pending = new List<(ProjectNode Node, Instance Instance)>();
        foreach (var root in roots)
        {
            if (!ClassCatalog.IsService(root.ClassName)) throw new InvalidDataException($"Unsupported service {root.ClassName}.");
            Attach(root, game.GetService(root.ClassName), pending, instances);
        }
        foreach (var (node, instance) in pending)
        {
            foreach (var (name, value) in node.Properties)
                SetProperty(instance, name, value.Decode(id => instances.GetValueOrDefault(id)));
            if (node.Source is not null && instance.IsA("LuaSourceContainer"))
            {
                instance.ScriptSource = node.Source;
                scripts.Add(new(instance, node.Source, $"{path}#{node.Id:D10}"));
            }
        }
    }

    private static void Attach(ProjectNode node, Instance instance, List<(ProjectNode, Instance)> pending,
        Dictionary<int, Instance> instances)
    {
        if (node.Id <= 0 || !instances.TryAdd(node.Id, instance))
            throw new InvalidDataException($"Invalid or duplicate project instance ID {node.Id}.");
        if (!ClassCatalog.IsService(node.ClassName)) instance.Name = node.Name;
        pending.Add((node, instance));
        foreach (var child in node.Children)
        {
            var created = (instance.ClassName, child.ClassName) switch
            {
                ("StarterPlayer", "StarterPlayerScripts") => Instance.CreateRuntime(child.ClassName),
                ("TextChatService", "ChatInputBarConfiguration") => instance.GetChildren().Single(item => item.ClassName == child.ClassName),
                _ => Instance.Create(child.ClassName)
            };
            created.Parent = instance;
            Attach(child, created, pending, instances);
        }
    }

    internal static PropertyInfo? FindProperty(Instance instance, string name)
    {
        var isSupported = instance switch
        {
            Part => name is "Size" or "CFrame" or "PivotOffset" or "Anchored" or "CanCollide" or "CanTouch"
                or "Shape" or "CustomPhysicalProperties" or "Color" or "BrickColor",
            Workspace => name is "Gravity" or "WorldPivot" or "PrimaryPart",
            RigidJoint => name is "C0" or "C1" or "Enabled" or "Part0" or "Part1",
            Tool => name is "Enabled" or "RequiresHandle" or "CanBeDropped" or "ManualActivationOnly" or "ToolTip" or "TextureId" or "Grip",
            Model => name is "PrimaryPart" or "WorldPivot",
            ScriptInstance => name is "Enabled" or "RunContext",
            Humanoid => name is "Health" or "MaxHealth" or "WalkSpeed" or "JumpPower" or "AutoRotate",
            Team => name == "TeamColor",
            Players => name is "CharacterAutoLoads" or "RespawnTime",
            ValueObject => name == "Value",
            Sky => name is "SkyboxBk" or "SkyboxDn" or "SkyboxFt" or "SkyboxLf" or "SkyboxRt" or "SkyboxUp",
            _ => false
        };
        return isSupported ? instance.GetType().GetProperty(name) : null;
    }

    internal static void SetProperty(Instance instance, string name, object? value)
    {
        if (name is "@RenderColor" or "@CharacterSpawn")
        {
            instance.SetAttribute(name[1..], value);
            return;
        }
        if (instance is GuiInstance gui && gui.HasGuiProperty(name) && value is not null)
        {
            gui.SetGuiProperty(name, value);
            return;
        }
        var property = FindProperty(instance, name) ?? throw new InvalidDataException($"Unsupported {instance.ClassName}.{name}.");
        if (value is double number)
        {
            if (property.PropertyType == typeof(int)) value = checked((int)number);
            else if (property.PropertyType.IsEnum) value = Enum.ToObject(property.PropertyType, checked((int)number));
        }
        try { property.SetValue(instance, value); }
        catch (TargetInvocationException error) { throw new InvalidDataException($"Invalid {instance.ClassName}.{name}.", error.InnerException); }
    }
}
