namespace Woorf.Engine.DataModel;

public sealed class DataModel : Instance
{
    public DataModel() : base("DataModel")
    {
        Name = "Game";
        Workspace = GetService("Workspace");
    }

    public Instance Workspace { get; }

    public Instance? FindService(string className) =>
        GetChildren().FirstOrDefault(child => child.ClassName == className && ClassCatalog.IsService(className));

    public Instance GetService(string className)
    {
        if (!ClassCatalog.IsService(className))
            throw new InvalidOperationException($"Unsupported service '{className}'.");
        var service = FindService(className);
        if (service is not null) return service;
        return new Instance(className) { Parent = this };
    }
}
