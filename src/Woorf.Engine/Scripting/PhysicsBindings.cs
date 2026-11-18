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
        if (BasePart.IsSurfaceProperty(property)) { codec.Push(state, part.GetSurface(property)); return true; }
        if (part is Part shaped && property == "_Shape") { codec.Push(state, (double)shaped.Shape); return true; }
        object? value = property switch
        {
            "AssemblyLinearVelocity" => part.AssemblyLinearVelocity,
            "AssemblyAngularVelocity" => part.AssemblyAngularVelocity,
            "Locked" => part.Locked,
            "Reflectance" => part.Reflectance,
            "CanCollide" => part.CanCollide, "Color" => part.Color, "BrickColor" or "brickColor" => part.BrickColor,
            "CustomPhysicalProperties" => part.CustomPhysicalProperties,
            "Elasticity" => (double)(part.CustomPhysicalProperties ?? PhysicalProperties.Default).Elasticity,
            "Friction" => (double)(part.CustomPhysicalProperties ?? PhysicalProperties.Default).Friction,
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
        if (BasePart.IsSurfaceProperty(property))
        {
            part.SetSurface(property, codec.ReadValue(state, 3) switch
            {
                GuiEnum surface => surface,
                string name => GuiEnum.From("SurfaceType", name),
                double number when GuiEnums.FindIndex("SurfaceType", (int)number) is var index && index >= 0 =>
                    GuiEnum.From("SurfaceType", GuiEnums.Definitions["SurfaceType"][index]),
                _ => throw new ArgumentException("Expected Enum.SurfaceType."),
            });
            return true;
        }
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
            case "Color": part.Color = codec.ReadValue(state, 3) is Color3 color ? color : throw new ArgumentException("Expected Color3."); break;
            case "BrickColor" or "brickColor": part.BrickColor = codec.ReadValue(state, 3) is BrickColor brick ? brick : throw new ArgumentException("Expected BrickColor."); break;
            case "Locked": part.Locked = codec.ReadValue(state, 3) is bool locked ? locked : throw new ArgumentException("Expected boolean."); break;
            case "Reflectance": part.Reflectance = codec.ReadValue(state, 3) is double reflectance ? reflectance : throw new ArgumentException("Expected number."); break;
            case "CanCollide": part.CanCollide = codec.ReadValue(state, 3) is bool value ? value : throw new ArgumentException("Expected boolean."); break;
            case "CustomPhysicalProperties":
                var material = codec.ReadValue(state, 3);
                if (material is not null and not PhysicalProperties) throw new ArgumentException("Expected PhysicalProperties or nil.");
                part.CustomPhysicalProperties = (PhysicalProperties?)material; break;
            case "Elasticity" or "Friction":
                var current = part.CustomPhysicalProperties ?? PhysicalProperties.Default;
                var legacy = codec.ReadValue(state, 3) is double amount ? (float)amount : throw new ArgumentException("Expected number.");
                part.CustomPhysicalProperties = new(current.Density, property == "Friction" ? legacy : current.Friction,
                    property == "Elasticity" ? legacy : current.Elasticity, current.FrictionWeight, current.ElasticityWeight);
                break;
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
