namespace Woorf.Engine.DataModel;

public static class ClassCatalog
{
    static ClassCatalog()
    {
        foreach (var (name, ancestor) in GuiDefaults.Classes) Bases[name] = ancestor;
    }
    private static readonly Dictionary<string, string?> Bases = new(StringComparer.Ordinal)
    {
        ["ValueBase"] = "Instance", ["IntValue"] = "ValueBase", ["NumberValue"] = "ValueBase",
        ["BoolValue"] = "ValueBase", ["StringValue"] = "ValueBase", ["ObjectValue"] = "ValueBase",
        ["Vector3Value"] = "ValueBase", ["CFrameValue"] = "ValueBase", ["Color3Value"] = "ValueBase", ["BrickColorValue"] = "ValueBase",
        ["BindableEvent"] = "Instance", ["BindableFunction"] = "Instance", ["Humanoid"] = "Instance", ["Team"] = "Instance",
        ["Lighting"] = "Instance", ["Sky"] = "Instance",
        ["GroupService"] = "Instance", ["GamepadService"] = "Instance", ["TextService"] = "Instance", ["LocalizationService"] = "Instance", ["VRService"] = "Instance", ["UserInputService"] = "Instance", ["ContextActionService"] = "Instance",
        ["CollectionService"] = "Instance", ["HttpService"] = "Instance", ["Debris"] = "Instance",
        ["TextChatService"] = "Instance", ["ChatInputBarConfiguration"] = "Instance",
        ["BadgeService"] = "Instance", ["MarketplaceService"] = "Instance", ["DataStoreService"] = "Instance", ["GlobalDataStore"] = "Instance", ["Teams"] = "Instance",
        ["Object"] = null, ["Instance"] = "Object", ["ServiceProvider"] = "Instance",
        ["DataModel"] = "ServiceProvider", ["PVInstance"] = "Instance", ["Model"] = "PVInstance",
        ["Tool"] = "BackpackItem", ["BackpackItem"] = "Model", ["Backpack"] = "Instance", ["StarterGear"] = "Instance",
        ["Workspace"] = "Model", ["Folder"] = "Instance", ["Configuration"] = "Instance",
        ["Camera"] = "PVInstance",
        ["JointInstance"] = "Instance", ["Weld"] = "JointInstance", ["Snap"] = "JointInstance",
        ["TweenService"] = "Instance", ["TweenBase"] = "Instance", ["Tween"] = "TweenBase",
        ["BasePart"] = "PVInstance", ["FormFactorPart"] = "BasePart", ["Part"] = "FormFactorPart",
        ["LuaSourceContainer"] = "Instance", ["BaseScript"] = "LuaSourceContainer",
        ["Script"] = "BaseScript", ["LocalScript"] = "Script", ["ModuleScript"] = "LuaSourceContainer",
        ["ReplicatedStorage"] = "Instance", ["ReplicatedFirst"] = "Instance",
        ["ServerStorage"] = "Instance", ["ServerScriptService"] = "Instance",
        ["StarterGui"] = "BasePlayerGui", ["BasePlayerGui"] = "Instance",
        ["StarterPack"] = "Instance", ["StarterPlayer"] = "Instance",
        ["CoreGui"] = "BasePlayerGui", ["GuiService"] = "Instance", ["Players"] = "Instance", ["Player"] = "Instance", ["RunService"] = "Instance",
        ["RemoteFunction"] = "Instance", ["BaseRemoteEvent"] = "Instance", ["RemoteEvent"] = "BaseRemoteEvent",
        ["StarterPlayerScripts"] = "Instance", ["PlayerScripts"] = "Instance", ["PlayerGui"] = "BasePlayerGui",
    };

    public static bool IsService(string className) => className is "Workspace" or "ReplicatedStorage"
        or "ReplicatedFirst" or "ServerStorage" or "ServerScriptService" or "StarterGui"
        or "CoreGui" or "GuiService" or "StarterPack" or "StarterPlayer" or "Players" or "RunService" or "TweenService"
        or "CollectionService" or "HttpService" or "Debris" or "BadgeService" or "MarketplaceService" or "TextChatService" or "DataStoreService" or "Teams" or "Lighting" or "GroupService" or "GamepadService" or "TextService" or "LocalizationService" or "VRService" or "UserInputService" or "ContextActionService";

    public static bool IsCreatable(string className) => GuiDefaults.IsCreatable(className) || className is "Folder" or "Model"
        or "Configuration" or "Script" or "LocalScript" or "ModuleScript" or "Part" or "RemoteEvent" or "RemoteFunction" or "Player"
        or "Tool" or "BindableEvent" or "BindableFunction" or "Humanoid" or "Team" or "Sky" or "Camera" or "Weld" or "Snap"
        or "IntValue" or "NumberValue" or "BoolValue" or "StringValue" or "ObjectValue"
        or "Vector3Value" or "CFrameValue" or "Color3Value" or "BrickColorValue";

    public static bool IsA(string className, string target)
    {
        for (string? current = className; current is not null; current = Bases[current])
            if (current == target) return true;
        return false;
    }
}
