namespace Woorf.Engine.DataModel;

public static class ClassCatalog
{
    private static readonly Dictionary<string, string?> Bases = new(StringComparer.Ordinal)
    {
        ["Object"] = null, ["Instance"] = "Object", ["ServiceProvider"] = "Instance",
        ["DataModel"] = "ServiceProvider", ["PVInstance"] = "Instance", ["Model"] = "PVInstance",
        ["Workspace"] = "Model", ["Folder"] = "Instance", ["Configuration"] = "Instance",
        ["BasePart"] = "PVInstance", ["FormFactorPart"] = "BasePart", ["Part"] = "FormFactorPart",
        ["LuaSourceContainer"] = "Instance", ["BaseScript"] = "LuaSourceContainer",
        ["Script"] = "BaseScript", ["LocalScript"] = "Script", ["ModuleScript"] = "LuaSourceContainer",
        ["ReplicatedStorage"] = "Instance", ["ReplicatedFirst"] = "Instance",
        ["ServerStorage"] = "Instance", ["ServerScriptService"] = "Instance",
        ["StarterGui"] = "BasePlayerGui", ["BasePlayerGui"] = "Instance",
        ["StarterPack"] = "Instance", ["StarterPlayer"] = "Instance",
        ["Players"] = "Instance", ["Player"] = "Instance", ["RunService"] = "Instance",
        ["BaseRemoteEvent"] = "Instance", ["RemoteEvent"] = "BaseRemoteEvent",
        ["StarterPlayerScripts"] = "Instance", ["PlayerScripts"] = "Instance", ["PlayerGui"] = "BasePlayerGui",
    };

    public static bool IsService(string className) => className is "Workspace" or "ReplicatedStorage"
        or "ReplicatedFirst" or "ServerStorage" or "ServerScriptService" or "StarterGui"
        or "StarterPack" or "StarterPlayer" or "Players" or "RunService";

    public static bool IsCreatable(string className) => className is "Folder" or "Model"
        or "Configuration" or "Script" or "LocalScript" or "ModuleScript" or "Part" or "RemoteEvent" or "Player";

    public static bool IsA(string className, string target)
    {
        for (string? current = className; current is not null; current = Bases[current])
            if (current == target) return true;
        return false;
    }
}
