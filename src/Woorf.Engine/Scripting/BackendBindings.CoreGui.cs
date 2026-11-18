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

        if (instance is not GuiService gui) return false;
        var core = game.CoreOverlay;
        switch (property)
        {
            case "IsTenFootInterface": woorf_push_method(state, property); return true;
            case "GetGuiInset": woorf_push_method(state, property); return true;
            case "PreferredTransparency": Push(state, gui.PreferredTransparency); return true;
            case "TopbarInset": Push(state, gui.TopbarInset); return true;
            case "SelectedObject": Push(state, gui.SelectedObject); return true;
            case "MenuIsOpen": Push(state, core.MenuIsOpen); return true;
            case "MenuOpened": signals.PushSignal(state, core.MenuOpened); return true;
            case "MenuClosed": signals.PushSignal(state, core.MenuClosed); return true;
            default: return false;
        }
    }

    private bool TryCallCoreGui(nint state, Instance instance, string method, out int result)
    {
        result = 0;
        if (instance is GuiService && method == "GetGuiInset")
        {
            Push(state, default(Vector2));
            Push(state, default(Vector2));
            result = 2;
            return true;
        }
        if (instance is GuiService && method == "IsTenFootInterface")
        {
            result = Push(state, false);
            return true;
        }
        if (instance.ClassName != "StarterGui" || method is not ("SetCoreGuiEnabled" or "GetCoreGuiEnabled" or "SetCore" or "GetCore"))
            return false;
        if (!((RunService)game.GetService("RunService")).IsClient())
            throw new InvalidOperationException("Core GUI controls are client-only.");

        var core = game.CoreOverlay;
        if (method is "SetCore" or "GetCore")
        {
            var setting = Text(state, 2);
            switch (setting)
            {
                case "DevConsoleVisible":
                    if (method == "SetCore") core.IsConsoleVisible = ReadCoreGuiBoolean(state);
                    else result = Push(state, core.IsConsoleVisible);
                    break;
                case "TopbarEnabled":
                    if (method == "SetCore") core.IsTopbarEnabled = ReadCoreGuiBoolean(state);
                    else result = Push(state, core.IsTopbarEnabled);
                    break;
                case "ChatActive":
                    if (method == "SetCore") ReadCoreGuiBoolean(state);
                    else result = Push(state, false);
                    break;
                default: throw new InvalidOperationException($"Unsupported Core setting '{setting}'.");
            }
            return true;
        }

        var kind = Read(state, 2);
        if (kind is not GuiEnum { EnumType: "CoreGuiType", Name: "PlayerList" or "Backpack" or "All" }
            && kind is not ("PlayerList" or "Backpack" or "All"))
            throw new InvalidOperationException("Expected Enum.CoreGuiType.PlayerList, Backpack or All.");
        var name = kind is GuiEnum item ? item.Name : (string)kind!;
        if (method == "SetCoreGuiEnabled")
        {
            var enabled = ReadCoreGuiBoolean(state);
            if (name is "PlayerList" or "All") core.IsPlayerListEnabled = enabled;
            if (name is "Backpack" or "All") core.IsBackpackEnabled = enabled;
        }
        else result = Push(state, name switch
        {
            "Backpack" => core.IsBackpackEnabled,
            "All" => core.IsPlayerListEnabled && core.IsBackpackEnabled,
            _ => core.IsPlayerListEnabled
        });
        return true;
    }

    private bool ReadCoreGuiBoolean(nint state) => Read(state, 3) is bool value
        ? value : throw new InvalidOperationException("Expected boolean.");
}
