using System.Numerics;
using Woorf.Graphics;
namespace Woorf.Presentation;

internal sealed class ScenePacketEncoder
{
    private readonly SceneDrawState state = new();
    private string? lastTextures;

    internal object Encode(WorldFrame frame, PerspectiveCamera camera, float aspect)
    {
        state.Prepare(frame.Items, camera, aspect);
        var clear = SceneDrawState.ClearColor;
        return new
        {
            camera = Flatten(state.ViewProjection), eye = Components(state.Eye), clear = Components(clear),
            batches = new[] { EncodeBatch(state.Boxes), EncodeBatch(state.Spheres) },
            itemCount = frame.Items.Length, sky = EncodeSky(frame.Sky, camera, aspect),
        };
    }

    private static object EncodeBatch(InstanceBatch batch) => new
    {
        count = batch.Count,
        data = batch.Data.AsSpan(0, batch.Count * InstanceBatch.FloatsPerInstance).ToArray(),
    };

    private object? EncodeSky(Skybox? sky, PerspectiveCamera camera, float aspect)
    {
        if (sky is null) return null;
        var textures = string.Join('\n', sky.Faces);
        var images = textures == lastTextures ? null : sky.LoadImages().Select(image => new
        {
            width = image.Width, height = image.Height, pixels = image.Pixels,
        }).ToArray();
        lastTextures = textures;
        return new { images, inverseViewProjection = Flatten(camera.GetSkyInverseViewProjection(aspect)),
            inverseOrientation = Flatten(sky.GetInverseOrientation()) };
    }

    private static float[] Components(Vector3 value) => [value.X, value.Y, value.Z];
    private static float[] Flatten(Matrix4x4 m) =>
        [m.M11,m.M12,m.M13,m.M14,m.M21,m.M22,m.M23,m.M24,m.M31,m.M32,m.M33,m.M34,m.M41,m.M42,m.M43,m.M44];
}

