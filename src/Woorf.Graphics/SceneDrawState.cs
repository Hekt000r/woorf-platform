using System.Numerics;

namespace Woorf.Graphics;

public sealed class SceneDrawState
{
    public static Vector3 ClearColor => new(0.055f, 0.075f, 0.11f);
    public InstanceBatch Boxes { get; } = new();
    public InstanceBatch Spheres { get; } = new();
    public Matrix4x4 ViewProjection { get; private set; }
    public Vector3 Eye { get; private set; }

    public void Prepare(IReadOnlyList<RenderItem> items, PerspectiveCamera camera, float aspect)
    {
        ViewProjection = camera.GetViewProjection(aspect); Eye = camera.Position;
        Boxes.Begin(); Spheres.Begin();
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            (item.Shape == MeshShape.Sphere ? Spheres : Boxes).Append(item);
        }
    }
}
