using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal static class PhysicsBindings
{
    internal static bool TryRead(nint state, Instance instance, string property, LuaValueCodec codec)
    {
        if (instance is Workspace workspace && property == "Gravity") { codec.Push(state, workspace.Gravity); return true; }
        if (instance is not BasePart part) return false;
        if (part is Part shaped && property == "_Shape") { codec.Push(state, (double)shaped.Shape); return true; }
        object? value = property switch
        {
            "AssemblyLinearVelocity" => part.AssemblyLinearVelocity,
            "AssemblyAngularVelocity" => part.AssemblyAngularVelocity,
            "CanCollide" => part.CanCollide,
            "CustomPhysicalProperties" => part.CustomPhysicalProperties,
            _ => null,
        };
        if (value is null && property != "CustomPhysicalProperties") return false;
        codec.Push(state, value); return true;
    }
    internal static bool TryWrite(nint state, Instance instance, string property, LuaValueCodec codec)
    {
        if (instance is Workspace workspace && property == "Gravity")
        {
            workspace.Gravity = codec.ReadValue(state, 3) is double number ? number : throw new ArgumentException("Expected gravity number.");
            return true;
        }
        if (instance is not BasePart part) return false;
        if (part is Part shaped && property == "_Shape")
        {
            var value = codec.ReadValue(state, 3);
            if (value is not double number || number is not (0 or 1)) throw new ArgumentException("Unsupported Part shape.");
            shaped.Shape = (PartType)(int)number; return true;
        }
        switch (property)
        {
            case "AssemblyLinearVelocity": part.AssemblyLinearVelocity = Vector3Bindings.Read(state, 3); break;
            case "AssemblyAngularVelocity": part.AssemblyAngularVelocity = Vector3Bindings.Read(state, 3); break;
            case "CanCollide": part.CanCollide = codec.ReadValue(state, 3) is bool value ? value : throw new ArgumentException("Expected boolean."); break;
            case "CustomPhysicalProperties":
                var material = codec.ReadValue(state, 3);
                if (material is not null and not PhysicalProperties) throw new ArgumentException("Expected PhysicalProperties or nil.");
                part.CustomPhysicalProperties = (PhysicalProperties?)material; break;
            default: return false;
        }
        return true;
    }
    internal static PhysicalProperties ReadMaterial(nint state, int index)
    {
        var components = new float[5];
        if (woorf_get_physical_properties(state, index, components) == 0) throw new ArgumentException("Expected PhysicalProperties.");
        return PhysicalProperties.FromComponents(components);
    }
}
