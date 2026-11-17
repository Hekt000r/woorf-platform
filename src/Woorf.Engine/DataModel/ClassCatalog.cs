namespace Woorf.Engine.DataModel;

public static class ClassCatalog
{
    private static readonly Dictionary<string, string?> Bases = new(StringComparer.Ordinal)
    {
        ["ValueBase"] = "Instance", ["IntValue"] = "ValueBase", ["NumberValue"] = "ValueBase",
        ["BoolValue"] = "ValueBase", ["StringValue"] = "ValueBase", ["ObjectValue"] = "ValueBase",
        ["Vector3Value"] = "ValueBase", ["CFrameValue"] = "ValueBase", ["Color3Value"] = "ValueBase",
        ["BindableEvent"] = "Instance", ["Humanoid"] = "Instance", ["Team"] = "Instance",
        ["Lighting"] = "Instance", ["Sky"] = "Instance",
        ["CollectionService"] = "Instance", ["HttpService"] = "Instance", ["Debris"] = "Instance",
        ["DataStoreService"] = "Instance", ["GlobalDataStore"] = "Instance", ["Teams"] = "Instance",
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
        ["RemoteFunction"] = "Instance", ["BaseRemoteEvent"] = "Instance", ["RemoteEvent"] = "BaseRemoteEvent",
        ["StarterPlayerScripts"] = "Instance", ["PlayerScripts"] = "Instance", ["PlayerGui"] = "BasePlayerGui",
    };

    public static bool IsService(string className) => className is "Workspace" or "ReplicatedStorage"
        or "ReplicatedFirst" or "ServerStorage" or "ServerScriptService" or "StarterGui"
        or "StarterPack" or "StarterPlayer" or "Players" or "RunService"
        or "CollectionService" or "HttpService" or "Debris" or "DataStoreService" or "Teams" or "Lighting";

    public static bool IsCreatable(string className) => className is "Folder" or "Model"
        or "Configuration" or "Script" or "LocalScript" or "ModuleScript" or "Part" or "RemoteEvent" or "RemoteFunction" or "Player"
        or "BindableEvent" or "Humanoid" or "Team" or "Sky"
        or "IntValue" or "NumberValue" or "BoolValue" or "StringValue" or "ObjectValue"
        or "Vector3Value" or "CFrameValue" or "Color3Value";

    public static bool IsA(string className, string target)
    {
        for (string? current = className; current is not null; current = Bases[current])
            if (current == target) return true;
        return false;
    }
}
