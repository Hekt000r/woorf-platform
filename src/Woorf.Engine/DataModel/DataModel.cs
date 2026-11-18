namespace Woorf.Engine.DataModel;

public sealed class DataModel : Instance
{
    public DataModel() : base("DataModel")
    {
        Name = "Game";
        Workspace = GetService("Workspace");
    }

    internal string DataDirectory { get; set; } = Path.Combine(Environment.CurrentDirectory, ".woorf", "data");

    public Gui.CoreGuiOverlay CoreOverlay { get; } = new();
    public IReadOnlyCollection<string> ScriptOutput => scriptOutput;
    private readonly Queue<string> scriptOutput = new();

    internal void RecordScriptOutput(string text)
    {
        foreach (var line in text.TrimEnd().Split('\n'))
        {
            if (scriptOutput.Count == 120) scriptOutput.Dequeue();
            scriptOutput.Enqueue(line.Length > 1024 ? line[..1024] : line);
        }
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
        Instance created = className == "Workspace" ? new Workspace() : Instance.CreateRuntime(className);
        if (className == "TextChatService")
            Instance.CreateRuntime("ChatInputBarConfiguration").Parent = created;
        created.Parent = this;
        return created;
    }
}
