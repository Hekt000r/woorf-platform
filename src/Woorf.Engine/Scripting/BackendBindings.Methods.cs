using Woorf.Engine.DataModel;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    internal bool TryCall(nint state, Instance instance, string method, out int result)
    {
        result = 0;
        if(instance is UserInputService input && method=="IsKeyDown")
        { result=Push(state,input.IsKeyDown(Read(state,2) as Datatypes.GuiEnum ?? throw new InvalidOperationException("Expected Enum.KeyCode."))); return true; }
        if (instance is ContextActionService actions && TryCallContextAction(state, actions, method, out result)) return true;
        if (TryCallTools(state, instance, method)) return true;
        if (TryCallCoreGui(state, instance, method, out result)) return true;
        if (TryCallTween(state, instance, method, out result)) return true;
        if (instance is UserInputService deviceInput && method is "GetLastInputType" or "GetGamepadConnected" or "GetImageForKeyCode")
        {
            result = method switch
            {
                "GetLastInputType" => Push(state, deviceInput.LastInputType),
                "GetGamepadConnected" => Push(state, deviceInput.GamepadEnabled && Read(state, 2) is Datatypes.GuiEnum { EnumType: "UserInputType", Name: "Gamepad1" }),
                _ => Push(state, ""),
            };
            return true;
        }
        if (TryCallGui(state, instance, method, out result)) return true;
        if (!IsMethod(instance, method) || method == "AddItem") return false;
        if (instance.ClassName == "CollectionService") { result = CallCollection(state, method); return true; }
        if (instance.ClassName == "BadgeService") { result = CallBadgeStub(state, method); return true; }
        if (instance.ClassName == "HttpService") { result = CallJson(state, method); return true; }
        if (instance.ClassName is "DataStoreService" or "GlobalDataStore") { result = CallStore(state, instance, method); return true; }
        switch (method)
        {
            case "UserOwnsGamePassAsync":
                Number(state, 2);
                Number(state, 3);
                result = Push(state, false);
                break;
            case "PromptGamePassPurchase":
                var purchaser = Require(state, 2) as Player ?? throw new ArgumentException("Expected Player.");
                ((MarketplaceService)instance).PromptGamePassPurchase(purchaser, Number(state, 3));
                break;
            case "GetTouchingParts": result = List(state, ((BasePart)instance).GetTouchingParts()); break;
            case "GetPlayerFromCharacter": result = Push(state, ((Players)instance).GetPlayerFromCharacter(Require(state, 2))); break;
            case "SetAttribute": instance.SetAttribute(Text(state, 2), Read(state, 3)); break;
            case "GetAttribute": result = Push(state, instance.GetAttribute(Text(state, 2))); break;
            case "GetAttributes": result = Push(state, new LuaTableValue(instance.GetAttributes().Select(entry => new KeyValuePair<object, object?>(entry.Key, entry.Value)).ToArray())); break;
            case "GetAttributeChangedSignal": result = signals.PushSignal(state, instance.GetAttributeChangedSignal(Text(state, 2))); break;
            case "AddTag": instance.AddTag(Text(state, 2)); break;
            case "RemoveTag": instance.RemoveTag(Text(state, 2)); break;
            case "HasTag": result = Push(state, instance.HasTag(Text(state, 2))); break;
            case "GetTags": result = List(state, instance.GetTags()); break;
            case "Fire": ((BindableEvent)instance).Fire(codec.ReadArguments(state, 2)); break;
            case "TakeDamage": ((Humanoid)instance).TakeDamage(Number(state, 2)); break;
            case "Move":
            case "LoadCharacter": CallCharacter(state, instance, method); break;
            case "GetTeams": result = List(state, instance.GetChildren().Where(child => child.ClassName == "Team")); break;
            case "GetPlayers": result = List(state, ((Players)game.GetService("Players")).GetPlayers().Where(player => player.Team == instance)); break;
            case "Raycast":
            case "_Raycast": result = Push(state, HeadlessRaycast.Cast(game.Workspace, Read(state, 2), Read(state, 3), Read(state, 4))); break;
            case "Spherecast":
            case "_Spherecast": result = Push(state, HeadlessSpherecast.Cast(game.Workspace, Read(state, 2), Number(state, 3), Read(state, 4), Read(state, 5))); break;
            case "BindToClose":
                var reference = woorf_ref_shutdown(state, 2);
                if (reference <= 0) throw new InvalidOperationException("Expected shutdown function.");
                shutdown.Add(reference); break;
        }
        return true;
    }

    private int CallCollection(nint state, string method)
    {
        if (method == "GetAllTags") return List(state, tags.GetAllTags());
        if (method == "GetTagged") return List(state, tags.GetTagged(Text(state, 2)));
        if (method is "GetInstanceAddedSignal" or "GetInstanceRemovedSignal")
            return signals.PushSignal(state, tags.GetSignal(Text(state, 2), method == "GetInstanceAddedSignal"));
        var instance = Require(state, 2);
        if (method == "GetTags") return List(state, instance.GetTags());
        var tag = Text(state, 3);
        if (method == "AddTag") instance.AddTag(tag);
        else if (method == "RemoveTag") instance.RemoveTag(tag);
        else return Push(state, instance.HasTag(tag));
        return 0;
    }
}
