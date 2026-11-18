using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private bool TryReadTools(nint state, Instance instance, string property)
    {
        if ((instance is Humanoid && property is "EquipTool" or "UnequipTools") ||
            (instance is Tool && property is "Activate" or "Deactivate"))
        {
            woorf_push_method(state, property);
            return true;
        }

        if (instance is not Tool tool) return false;
        var signal = property switch
        {
            "Equipped" => tool.Equipped, "Unequipped" => tool.Unequipped,
            "Activated" => tool.Activated, "Deactivated" => tool.Deactivated, _ => null
        };
        if (signal is not null)
        {
            signals.PushSignal(state, signal);
            return true;
        }
        object? value = property switch
        {
            "Enabled" => tool.Enabled, "CanBeDropped" => tool.CanBeDropped,
            "RequiresHandle" => tool.RequiresHandle, "ManualActivationOnly" => tool.ManualActivationOnly,
            "ToolTip" => tool.ToolTip, "TextureId" => tool.TextureId, "Grip" => tool.Grip,
            "GripPos" => tool.Grip.Position, "GripRight" => tool.Grip.RightVector,
            "GripUp" => tool.Grip.UpVector, "GripForward" => tool.Grip.LookVector, _ => null
        };
        if (value is null) return false;
        Push(state, value);
        return true;
    }

    private bool TryWriteTool(nint state, Instance instance, string property)
    {
        if (instance is not Tool tool) return false;
        switch (property)
        {
            case "Enabled": tool.Enabled = ReadToolFlag(state); break;
            case "CanBeDropped": tool.CanBeDropped = ReadToolFlag(state); break;
            case "RequiresHandle": tool.RequiresHandle = ReadToolFlag(state); break;
            case "ManualActivationOnly": tool.ManualActivationOnly = ReadToolFlag(state); break;
            case "ToolTip": tool.ToolTip = Text(state, 3); break;
            case "TextureId": tool.TextureId = Text(state, 3); break;
            case "Grip": tool.Grip = Read(state, 3) is CFrame frame ? frame : throw new ArgumentException("Expected CFrame."); break;
            case "GripPos": tool.Grip = tool.Grip.WithPosition(ReadToolVector(state)); break;
            case "GripRight": tool.Grip = new(tool.Grip.Position, ReadToolVector(state), tool.Grip.UpVector, tool.Grip.ZVector); break;
            case "GripUp": tool.Grip = new(tool.Grip.Position, tool.Grip.RightVector, ReadToolVector(state), tool.Grip.ZVector); break;
            case "GripForward": tool.Grip = new(tool.Grip.Position, tool.Grip.RightVector, tool.Grip.UpVector, ReadToolVector(state) * -1); break;
            default: return false;
        }
        return true;
    }

    private bool ReadToolFlag(nint state) => Read(state, 3) is bool value ? value : throw new ArgumentException("Expected boolean.");
    private Vector3 ReadToolVector(nint state) => Read(state, 3) is Vector3 value ? value : throw new ArgumentException("Expected Vector3.");

    private bool TryCallTools(nint state, Instance instance, string method)
    {
        switch (instance, method)
        {
            case (Humanoid humanoid, "EquipTool"):
                humanoid.EquipTool(Require(state, 2) as Tool ?? throw new ArgumentException("Expected Tool."));
                return true;
            case (Humanoid humanoid, "UnequipTools"): humanoid.UnequipTools(); return true;
            case (Tool tool, "Activate"): tool.Activate(); return true;
            case (Tool tool, "Deactivate"): tool.Deactivate(); return true;
            default: return false;
        }
    }
}
