using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Networking;

internal sealed record ReplicaState(string Name, Instance? Parent, bool Archivable, CFrame Frame,
    CFrame PivotOffset, Vector3 Size, bool Anchored, CFrame WorldPivot, BasePart? PrimaryPart, int UserId, string Source, ReplicatedBackendState? Backend = null, bool Enabled = true, int RunContext = 0, ReplicatedPhysicsState? Physics = null)
{
    internal static ReplicaState Capture(Instance instance) => new(instance.Name, instance.Parent, instance.Archivable,
        instance is BasePart part ? part.CFrame : default,
        instance is BasePart pivotPart ? pivotPart.PivotOffset : default,
        instance is BasePart sizedPart ? sizedPart.Size : default,
        instance is BasePart anchoredPart && anchoredPart.Anchored,
        instance is Model model ? model.WorldPivot : default,
        instance is Model primaryModel ? primaryModel.PrimaryPart : null,
        instance is Player player ? player.UserId : 0,
        instance.ClassName is "ModuleScript" or "LocalScript" || instance is ScriptInstance { RunContext: 2 } ? instance.ScriptSource : "",
        ReplicatedBackendState.Capture(instance), instance is not ScriptInstance script || script.Enabled,
        instance is ScriptInstance contextScript ? contextScript.RunContext : 0, ReplicatedPhysicsState.Capture(instance));

    internal void ApplyValues(Instance replica, ReplicaState? previous, Func<Instance, Instance?> translate)
    {
        if (replica is ScriptInstance script)
        {
            if (previous is null || previous.Enabled != Enabled) script.Enabled = Enabled;
            if (previous is null || previous.RunContext != RunContext) script.RunContext = RunContext;
        }
        Physics?.Apply(replica, previous?.Physics);
        Backend?.Apply(replica, previous?.Backend, translate);
        if (replica is BasePart part)
        {
            if (previous is null || previous.Frame != Frame) part.CFrame = Frame;
            if (previous is null || previous.PivotOffset != PivotOffset) part.PivotOffset = PivotOffset;
            if (previous is null || previous.Size != Size) part.Size = Size;
            if (previous is null || previous.Anchored != Anchored) part.Anchored = Anchored;
        }
        if (replica is Model model)
        {
            if (previous is null || previous.WorldPivot != WorldPivot) model.WorldPivot = WorldPivot;
            if (previous is null || previous.PrimaryPart != PrimaryPart)
                model.PrimaryPart = PrimaryPart is null ? null : translate(PrimaryPart) as BasePart;
        }
        if (replica is Player player && (previous is null || previous.UserId != UserId)) player.UserId = UserId;
        if (replica.ClassName is "ModuleScript" or "LocalScript" or "Script" && (previous is null || previous.Source != Source)) replica.ScriptSource = Source;
    }
}
