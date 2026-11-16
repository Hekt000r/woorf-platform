namespace Woorf.Engine.DataModel;

public sealed class DataModel : Instance
{
    public DataModel() : base("DataModel")
    {
        Name = "Game";
        Workspace = GetService("Workspace");
    }

    internal string DataDirectory { get; set; } = Path.Combine(Environment.CurrentDirectory, ".woorf", "data");

    public Instance Workspace { get; }

    public Instance? FindService(string className) =>
        GetChildren().FirstOrDefault(child => child.ClassName == className && ClassCatalog.IsService(className));

    public Instance GetService(string className)
    {
        if (!ClassCatalog.IsService(className))
            throw new InvalidOperationException($"Unsupported service '{className}'.");
        var service = FindService(className);
        if (service is not null) return service;
        Instance created = className == "Workspace" ? new Workspace() : Instance.CreateRuntime(className);
        created.Parent = this;
        return created;
    }
}
