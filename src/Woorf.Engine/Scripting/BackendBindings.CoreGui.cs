using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private bool TryReadCoreGui(nint state, Instance instance, string property, out int result)
    {
        result = 1;
        if (instance.ClassName == "StarterGui" && property is "SetCoreGuiEnabled" or "GetCoreGuiEnabled" or "SetCore" or "GetCore")
        {
            woorf_push_method(state, property);
            return true;
        }

        if (instance.ClassName != "GuiService") return false;
        var core = game.CoreOverlay;
        switch (property)
        {
            case "MenuIsOpen": Push(state, core.MenuIsOpen); return true;
            case "MenuOpened": signals.PushSignal(state, core.MenuOpened); return true;
            case "MenuClosed": signals.PushSignal(state, core.MenuClosed); return true;
            default: return false;
        }
    }

    private bool TryCallCoreGui(nint state, Instance instance, string method, out int result)
    {
        result = 0;
        if (instance.ClassName != "StarterGui" || method is not ("SetCoreGuiEnabled" or "GetCoreGuiEnabled" or "SetCore" or "GetCore"))
            return false;
        if (!((RunService)game.GetService("RunService")).IsClient())
            throw new InvalidOperationException("Core GUI controls are client-only.");

        var core = game.CoreOverlay;
        if (method is "SetCore" or "GetCore")
        {
            if (Text(state, 2) != "DevConsoleVisible") throw new InvalidOperationException("Unsupported Core setting.");
            if (method == "SetCore") core.IsConsoleVisible = ReadCoreGuiBoolean(state);
            else result = Push(state, core.IsConsoleVisible);
            return true;
        }

        var kind = Read(state, 2);
        if (kind is not GuiEnum { EnumType: "CoreGuiType", Name: "PlayerList" or "All" }
            && kind is not ("PlayerList" or "All"))
            throw new InvalidOperationException("Expected Enum.CoreGuiType.PlayerList or All.");
        if (method == "SetCoreGuiEnabled") core.IsPlayerListEnabled = ReadCoreGuiBoolean(state);
        else result = Push(state, core.IsPlayerListEnabled);
        return true;
    }

    private bool ReadCoreGuiBoolean(nint state) => Read(state, 3) is bool value
        ? value : throw new InvalidOperationException("Expected boolean.");
}
