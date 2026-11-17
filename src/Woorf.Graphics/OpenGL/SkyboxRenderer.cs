using System.Numerics;
using System.Runtime.InteropServices;

namespace Woorf.Graphics.OpenGL;

internal sealed class SkyboxRenderer : IDisposable
{
    private readonly Gl gl;
    private readonly ShaderProgram shader;
    private uint array, texture;
    private string[]? loaded;
    internal SkyboxRenderer(Gl gl)
    {
        this.gl = gl;
        shader = new(gl, Vertex, Fragment);
        gl.GenVertexArrays(1, out array);
        Gl.glGenTextures(1, out texture);
    }
    internal void Draw(Skybox sky, PerspectiveCamera camera, float aspect)
    {
        if (loaded is null || !loaded.SequenceEqual(sky.Faces)) Upload(sky.Faces);
        var centered = new PerspectiveCamera { Position = Vector3.Zero, Target = camera.Target - camera.Position,
            FieldOfView = camera.FieldOfView, NearPlane = camera.NearPlane, FarPlane = camera.FarPlane };
        Matrix4x4.Invert(centered.GetViewProjection(aspect), out var inverse);
        shader.Use(); shader.SetMatrix("inverseViewProjection", inverse);
        shader.SetMatrix("inverseOrientation", sky.GetInverseOrientation());
        Gl.glDisable(0x0B71); Gl.glDisable(0x0B44);
        Gl.glBindTexture(0x8513, texture);
        gl.BindVertexArray(array); Gl.glDrawArrays(4, 0, 3);
        Gl.glEnable(0x0B71); Gl.glEnable(0x0B44);
    }
    private void Upload(IReadOnlyList<string> faces)
    {
        var images = faces.Select(SkyImage.Load).ToArray();
        if (images.Any(image => image.Width != images[0].Width || image.Height != image.Width))
            throw new InvalidDataException("Skybox images must be square and have equal dimensions.");
        Gl.glBindTexture(0x8513, texture); Gl.glPixelStorei(0x0CF5, 1);
        for (var index = 0; index < images.Length; index++)
        {
            var image = images[index]; var pin = GCHandle.Alloc(image.Pixels, GCHandleType.Pinned);
            try { Gl.glTexImage2D((uint)(0x8515 + index), 0, 0x8058, image.Width, image.Height, 0, 0x1908, 0x1401, pin.AddrOfPinnedObject()); }
            finally { pin.Free(); }
        }
        Gl.glTexParameteri(0x8513, 0x2801, 0x2601); Gl.glTexParameteri(0x8513, 0x2800, 0x2601);
        foreach (var axis in new uint[] { 0x2802, 0x2803, 0x8072 }) Gl.glTexParameteri(0x8513, axis, 0x812F);
        Gl.glEnable(0x884F);
        loaded = faces.ToArray();
    }
    public void Dispose()
    {
        if (texture != 0) Gl.glDeleteTextures(1, ref texture);
        if (array != 0) gl.DeleteVertexArrays(1, ref array);
        shader.Dispose(); texture = array = 0;
    }
    private const string Vertex = """
        #version 330 core
        out vec2 clipPosition;
        void main() {
            clipPosition = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2) * 2.0 - 1.0;
            gl_Position = vec4(clipPosition, 0.0, 1.0);
        }
        """;
    private const string Fragment = """
        #version 330 core
        in vec2 clipPosition;
        uniform mat4 inverseViewProjection;
        uniform mat4 inverseOrientation;
        uniform samplerCube skyTexture;
        out vec4 color;
        void main() {
            vec4 point = inverseViewProjection * vec4(clipPosition, 1.0, 1.0);
            vec3 direction = mat3(inverseOrientation) * point.xyz;
            color = vec4(texture(skyTexture, direction).rgb, 1.0);
        }
        """;
}
