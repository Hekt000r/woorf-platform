using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

internal sealed class PivotMovement
{
    private sealed record PartOffset(BasePart Part, CFrame Relative, CFrame Expected);
    private sealed record ModelOffset(Model Model, CFrame Relative, CFrame Expected);
    private PartOffset[] partOffsets = [];
    private ModelOffset[] modelOffsets = [];
    private CFrame expectedPivot;
    private bool hasCache;
    private int movementVersion;

    internal void MoveCached(Instance root, CFrame from, CFrame target)
    {
        var version = ++movementVersion;
        var subtree = root.GetDescendants().Prepend(root).ToArray();
        var parts = subtree.OfType<BasePart>().ToArray();
        var models = subtree.OfType<Model>().ToArray();
        if (!CanReuseOffsets(parts, models, from)) CacheOffsets(parts, models, from);
        var partTargets = partOffsets.Select(offset => target * offset.Relative).ToArray();
        var modelTargets = modelOffsets.Select(offset => target * offset.Relative).ToArray();
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            if (part.IsDestroyed || part != root && !part.IsDescendantOf(root)) continue;
            part.MoveToPivotTransform(partTargets[index]);
            if (version != movementVersion) return;
            partOffsets[index] = partOffsets[index] with { Expected = partTargets[index].Orthonormalize() };
        }
        for (var index = 0; index < models.Length; index++)
        {
            var model = models[index];
            if (model.IsDestroyed || model != root && !model.IsDescendantOf(root)) continue;
            var modelTarget = model == root && model.PrimaryPart is null ? target : modelTargets[index];
            model.WorldPivot = modelTarget;
            if (version != movementVersion) return;
            modelOffsets[index] = modelOffsets[index] with { Expected = modelTarget };
        }
        expectedPivot = root switch { Model model => model.GetPivot(), BasePart part => part.GetPivot(), _ => target };
    }

    private bool CanReuseOffsets(BasePart[] parts, Model[] models, CFrame pivot)
    {
        if (!hasCache || pivot != expectedPivot || parts.Length != partOffsets.Length || models.Length != modelOffsets.Length)
            return false;
        for (var index = 0; index < parts.Length; index++)
            if (parts[index] != partOffsets[index].Part || parts[index].CFrame != partOffsets[index].Expected) return false;
        for (var index = 0; index < models.Length; index++)
            if (models[index] != modelOffsets[index].Model || models[index].WorldPivot != modelOffsets[index].Expected) return false;
        return true;
    }

    private void CacheOffsets(BasePart[] parts, Model[] models, CFrame from)
    {
        var inverse = from.Inverse();
        partOffsets = parts.Select(part => new PartOffset(part, inverse * part.CFrame, part.CFrame)).ToArray();
        modelOffsets = models.Select(model => new ModelOffset(model, inverse * model.WorldPivot, model.WorldPivot)).ToArray();
        hasCache = true;
    }
}
