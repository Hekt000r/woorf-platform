using System.Numerics;

namespace Woorf.Graphics;

public sealed class Skybox
{
    public IReadOnlyList<string> Faces { get; }
    public Vector3 Orientation { get; }
    public Skybox(IEnumerable<string> faces, Vector3 orientation)
    {
        var values = faces.ToArray();
        if (values.Length != 6 || values.Any(value => value is null)) throw new ArgumentException("A skybox requires six face images.");
        if (!float.IsFinite(orientation.X) || !float.IsFinite(orientation.Y) || !float.IsFinite(orientation.Z))
            throw new ArgumentException("Skybox orientation must be finite.");
        Faces = Array.AsReadOnly(values); Orientation = orientation;
    }
    internal Matrix4x4 GetInverseOrientation()
    {
        var radians = Orientation * (MathF.PI / 180);
        var rotation = Matrix4x4.CreateRotationY(radians.Y) * Matrix4x4.CreateRotationX(radians.X) * Matrix4x4.CreateRotationZ(radians.Z);
        return Matrix4x4.Transpose(rotation);
    }
}
