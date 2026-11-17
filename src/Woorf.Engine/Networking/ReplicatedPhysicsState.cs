using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Networking;

internal sealed record ReplicatedPhysicsState(Vector3 Linear, Vector3 Angular, bool CanCollide, PhysicalProperties? Material, double Gravity, PartType Shape = PartType.Block, bool CanTouch = true)
{
    internal static ReplicatedPhysicsState? Capture(Instance instance) => instance switch
    {
        BasePart part => new(part.AssemblyLinearVelocity, part.AssemblyAngularVelocity, part.CanCollide, part.CustomPhysicalProperties, 0, part is Part shaped ? shaped.Shape : PartType.Block, part.CanTouch),
        Workspace workspace => new(default, default, true, null, workspace.Gravity),
        _ => null,
    };
    internal void Apply(Instance instance, ReplicatedPhysicsState? previous)
    {
        if (instance is Workspace workspace && (previous is null || Gravity != previous.Gravity)) workspace.Gravity = Gravity;
        if (instance is not BasePart part) return;
        if (part is Part shaped && (previous is null || Shape != previous.Shape)) shaped.Shape = Shape;
        if (previous is null || Linear != previous.Linear) part.AssemblyLinearVelocity = Linear;
        if (previous is null || Angular != previous.Angular) part.AssemblyAngularVelocity = Angular;
        if (previous is null || CanCollide != previous.CanCollide) part.CanCollide = CanCollide;
        if (previous is null || CanTouch != previous.CanTouch) part.CanTouch = CanTouch;
        if (previous is null || Material != previous.Material) part.CustomPhysicalProperties = Material;
    }
    internal WirePhysicsState Encode() => new([Linear.X, Linear.Y, Linear.Z], [Angular.X, Angular.Y, Angular.Z], CanCollide, Material?.GetComponents(), Gravity, Shape, CanTouch);
}

internal sealed record WirePhysicsState(float[] Linear, float[] Angular, bool CanCollide, float[]? Material, double Gravity, PartType Shape = PartType.Block, bool CanTouch = true)
{
    internal ReplicatedPhysicsState Decode()
    {
        if (Linear.Length != 3 || Angular.Length != 3) throw new InvalidDataException("Invalid physics velocity.");
        return new(new(Linear[0], Linear[1], Linear[2]), new(Angular[0], Angular[1], Angular[2]), CanCollide,
            Material is null ? null : PhysicalProperties.FromComponents(Material), Gravity, Shape, CanTouch);
    }
}
