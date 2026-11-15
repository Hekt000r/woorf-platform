using System.Text.Json;
using Woorf.Engine.DataModel;
using GameDataModel = Woorf.Engine.DataModel.DataModel;

namespace Woorf.Engine.Projects;

public static class ProjectLoader
{
    public static LoadedProject Load(string directory)
    {
        var projectPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        RejectLink(projectPath);
        using var manifest = ReadJson(Path.Combine(projectPath, "woorf.project.json"), "Invalid project manifest");
        var rootPath = ReadString(manifest.RootElement, "root", "src");
        var sourcePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(projectPath, rootPath)));
        var relativePath = Path.GetRelativePath(projectPath, sourcePath);
        if (Path.IsPathRooted(relativePath) || relativePath == ".." ||
            relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException("Project root must remain inside the project directory.");
        if (!Directory.Exists(sourcePath))
            throw new InvalidDataException($"Project root directory does not exist: {sourcePath}");
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        for (var path = sourcePath; !path.Equals(projectPath, comparison); path = Path.GetDirectoryName(path)!)
            RejectLink(path);
        var game = new GameDataModel { Name = ReadString(manifest.RootElement, "name", "Game") };
        var scripts = new List<ProjectScript>();
        foreach (var servicePath in Directory.EnumerateDirectories(sourcePath).Order(StringComparer.Ordinal))
        {
            RejectLink(servicePath);
            var serviceName = Path.GetFileName(servicePath);
            if (!ClassCatalog.IsService(serviceName))
                throw new InvalidDataException($"Unsupported service directory: {serviceName}");
            LoadChildren(servicePath, game.GetService(serviceName), scripts);
        }
        return new LoadedProject(game, scripts.OrderBy(script => script.FilePath, StringComparer.Ordinal).ToArray());
    }

    private static void LoadChildren(string directory, Instance parent, List<ProjectScript> scripts)
    {
        foreach (var childPath in Directory.EnumerateDirectories(directory).Order(StringComparer.Ordinal))
        {
            RejectLink(childPath);
            var child = LoadFolder(childPath);
            Attach(child, parent);
            LoadChildren(childPath, child, scripts);
        }
        foreach (var filePath in Directory.EnumerateFiles(directory)
            .Where(path => path.EndsWith(".luau", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
        {
            RejectLink(filePath);
            var fileName = Path.GetFileName(filePath);
            var (className, suffix) = fileName.EndsWith(".server.luau", StringComparison.Ordinal)
                ? ("Script", ".server.luau")
                : fileName.EndsWith(".client.luau", StringComparison.Ordinal)
                    ? ("LocalScript", ".client.luau") : ("ModuleScript", ".luau");
            var script = Instance.Create(className);
            script.Name = fileName[..^suffix.Length];
            Attach(script, parent);
            script.ScriptSource = File.ReadAllText(filePath);
            scripts.Add(new ProjectScript(script, script.ScriptSource, filePath));
        }
    }

    private static Instance LoadFolder(string directory)
    {
        var metadataPath = Path.Combine(directory, "init.meta.json");
        var className = "Folder";
        if (File.Exists(metadataPath))
        {
            using var metadata = ReadJson(metadataPath, "Invalid instance metadata");
            className = ReadString(metadata.RootElement, "className", "Folder");
        }
        if (className is not ("Folder" or "Model" or "Configuration"))
            throw new InvalidDataException($"Unsupported directory class: {className}");
        var instance = Instance.Create(className);
        instance.Name = Path.GetFileName(directory);
        return instance;
    }

    private static JsonDocument ReadJson(string path, string errorMessage)
    {
        try
        {
            RejectLink(path);
            var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                document.Dispose();
                throw new InvalidDataException($"{errorMessage}: expected a JSON object in {path}");
            }
            return document;
        }
        catch (JsonException error) { throw new InvalidDataException($"{errorMessage}: {path}: {error.Message}", error); }
    }

    private static string ReadString(JsonElement element, string property, string fallback)
    {
        if (!element.TryGetProperty(property, out var value))
            return fallback;
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new InvalidDataException($"Project property '{property}' must be a nonempty string.");
        return value.GetString()!;
    }

    private static void Attach(Instance child, Instance parent)
    {
        if (parent.FindFirstChild(child.Name) is not null)
            throw new InvalidDataException($"Duplicate child name '{child.Name}' under {parent.GetFullName()}.");
        child.Parent = parent;
    }

    private static void RejectLink(string path)
    {
        if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            throw new InvalidDataException($"Linked paths are not supported in projects: {path}");
    }
}
