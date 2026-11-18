using System.Numerics;

namespace Woorf.Graphics;

public sealed class Skybox
{
    public IReadOnlyList<string> Faces { get; }
    public Vector3 Orientation { get; }
    private SkyImage[]? images;

    public IReadOnlyList<SkyImage> LoadImages()
    {
        if (images is not null) return images;
        var loaded = Faces.Select(SkyImage.Load).ToArray();
        if (loaded.Any(image => image.Width != loaded[0].Width || image.Height != image.Width))
            throw new InvalidDataException("Skybox images must be square and have equal dimensions.");
        return images = loaded;
    }
    public Skybox(IEnumerable<string> faces, Vector3 orientation)
    {
        var values = faces.ToArray();
        if (values.Length != 6 || values.Any(value => value is null)) throw new ArgumentException("A skybox requires six face images.");
        if (!float.IsFinite(orientation.X) || !float.IsFinite(orientation.Y) || !float.IsFinite(orientation.Z))
            throw new ArgumentException("Skybox orientation must be finite.");
        Faces = Array.AsReadOnly(values); Orientation = orientation;
    }
    public Matrix4x4 GetInverseOrientation()
    {
        var radians = Orientation * (MathF.PI / 180);
        var rotation = Matrix4x4.CreateRotationY(radians.Y) * Matrix4x4.CreateRotationX(radians.X) * Matrix4x4.CreateRotationZ(radians.Z);
        return Matrix4x4.Transpose(rotation);
    }
}
