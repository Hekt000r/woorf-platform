namespace Woorf.Engine.DataModel;

public static class ClassCatalog
{
    private static readonly Dictionary<string, string?> Bases = new(StringComparer.Ordinal)
    {
        ["Object"] = null, ["Instance"] = "Object", ["ServiceProvider"] = "Instance",
        ["DataModel"] = "ServiceProvider", ["PVInstance"] = "Instance", ["Model"] = "PVInstance",
        ["Workspace"] = "Model", ["Folder"] = "Instance", ["Configuration"] = "Instance",
        ["LuaSourceContainer"] = "Instance", ["BaseScript"] = "LuaSourceContainer",
        ["Script"] = "BaseScript", ["LocalScript"] = "Script", ["ModuleScript"] = "LuaSourceContainer",
        ["ReplicatedStorage"] = "Instance", ["ReplicatedFirst"] = "Instance",
        ["ServerStorage"] = "Instance", ["ServerScriptService"] = "Instance",
        ["StarterGui"] = "BasePlayerGui", ["BasePlayerGui"] = "Instance",
        ["StarterPack"] = "Instance", ["StarterPlayer"] = "Instance",
    };

    public static bool IsService(string className) => className is "Workspace" or "ReplicatedStorage"
        or "ReplicatedFirst" or "ServerStorage" or "ServerScriptService" or "StarterGui"
        or "StarterPack" or "StarterPlayer";

    public static bool IsCreatable(string className) => className is "Folder" or "Model"
        or "Configuration" or "Script" or "LocalScript" or "ModuleScript";

    public static bool IsA(string className, string target)
    {
        for (string? current = className; current is not null; current = Bases[current])
            if (current == target) return true;
        return false;
    }
}
