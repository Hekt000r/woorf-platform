using System.Numerics;
using Woorf.Engine.DataModel;
using Woorf.Graphics;

internal sealed class WorkspaceScene : IDisposable
{
    private readonly Instance workspace;
    private readonly Dictionary<BasePart, int> slots = [];
    private readonly HashSet<BasePart> dirty = [];
    private RenderItem[] items = [];
    private bool isHierarchyDirty = true;
    internal long FullScans { get; private set; }
    internal long CapturedParts { get; private set; }

    internal WorkspaceScene(Instance workspace)
    {
        this.workspace = workspace;
        workspace.DescendantMutated += OnMutation;
    }

    private void OnMutation(Instance instance, string property)
    {
        if (property == "Parent") isHierarchyDirty = true;
        else if (instance is BasePart part && property is "CFrame" or "Size" or "Anchored" or "Shape") dirty.Add(part);
    }

    internal RenderItem[] Capture()
    {
        if (isHierarchyDirty) return Rebuild();
        foreach (var part in dirty)
        {
            var next = CapturePart(part);
            var index = slots[part];
            if (next is null || index < 0) return Rebuild();
            items[index] = next.Value;
        }
        dirty.Clear();
        return items;
    }

    private RenderItem[] Rebuild()
    {
        FullScans++;
        var next = new List<RenderItem>();
        slots.Clear();
        foreach (var part in workspace.GetDescendants().OfType<BasePart>())
        {
            var item = CapturePart(part);
            slots.Add(part, item is null ? -1 : next.Count);
            if (item is not null) next.Add(item.Value);
        }
        items = next.ToArray();
        dirty.Clear(); isHierarchyDirty = false;
        return items;
    }

    private RenderItem? CapturePart(BasePart part)
    {
        CapturedParts++;
        var frame = part.CFrame;
        var position = frame.Position; var right = frame.RightVector; var up = frame.UpVector; var back = frame.ZVector;
        var transform = new Matrix4x4(right.X, right.Y, right.Z, 0, up.X, up.Y, up.Z, 0,
            back.X, back.Y, back.Z, 0, position.X, position.Y, position.Z, 1);
        var size = part.Size;
        var isSphere = part is Part { Shape: PartType.Ball };
        var scale = isSphere ? new Vector3(MathF.Min(size.X, MathF.Min(size.Y, size.Z)) * 0.5f) : new Vector3(size.X, size.Y, size.Z);
        transform = Matrix4x4.CreateScale(scale) * transform;
        if (!IsFinite(transform) || scale.X <= 0 || scale.Y <= 0 || scale.Z <= 0) return null;
        var color = part.Anchored ? new Vector3(0.25f, 0.32f, 0.42f)
            : isSphere ? new Vector3(0.95f, 0.36f, 0.09f) : new Vector3(0.05f, 0.58f, 0.65f);
        return new(isSphere ? MeshShape.Sphere : MeshShape.Box, transform, color);
    }

    private static bool IsFinite(Matrix4x4 m) =>
        float.IsFinite(m.M11) && float.IsFinite(m.M12) && float.IsFinite(m.M13)
        && float.IsFinite(m.M21) && float.IsFinite(m.M22) && float.IsFinite(m.M23)
        && float.IsFinite(m.M31) && float.IsFinite(m.M32) && float.IsFinite(m.M33)
        && float.IsFinite(m.M41) && float.IsFinite(m.M42) && float.IsFinite(m.M43);

    public void Dispose() => workspace.DescendantMutated -= OnMutation;
}
