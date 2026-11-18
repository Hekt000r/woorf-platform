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
        shader = new(gl, SkyShaders.Vertex, SkyShaders.Fragment);
        gl.GenVertexArrays(1, out array);
        Gl.glGenTextures(1, out texture);
    }
    internal void Draw(Skybox sky, PerspectiveCamera camera, float aspect)
    {
        if (loaded is null || !loaded.SequenceEqual(sky.Faces)) Upload(sky);
        shader.Use(); shader.SetMatrix("inverseViewProjection", camera.GetSkyInverseViewProjection(aspect));
        shader.SetMatrix("inverseOrientation", sky.GetInverseOrientation());
        Gl.glDisable(0x0B71); Gl.glDisable(0x0B44);
        Gl.glBindTexture(0x8513, texture);
        gl.BindVertexArray(array); Gl.glDrawArrays(4, 0, 3);
        Gl.glEnable(0x0B71); Gl.glEnable(0x0B44);
    }
    private void Upload(Skybox sky)
    {
        var images = sky.LoadImages();
        Gl.glBindTexture(0x8513, texture); Gl.glPixelStorei(0x0CF5, 1);
        for (var index = 0; index < images.Count; index++)
        {
            var image = images[index]; var pin = GCHandle.Alloc(image.Pixels, GCHandleType.Pinned);
            try { Gl.glTexImage2D((uint)(0x8515 + index), 0, 0x8058, image.Width, image.Height, 0, 0x1908, 0x1401, pin.AddrOfPinnedObject()); }
            finally { pin.Free(); }
        }
        Gl.glTexParameteri(0x8513, 0x2801, 0x2601); Gl.glTexParameteri(0x8513, 0x2800, 0x2601);
        foreach (var axis in new uint[] { 0x2802, 0x2803, 0x8072 }) Gl.glTexParameteri(0x8513, axis, 0x812F);
        Gl.glEnable(0x884F);
        loaded = sky.Faces.ToArray();
    }
    public void Dispose()
    {
        if (texture != 0) Gl.glDeleteTextures(1, ref texture);
        if (array != 0) gl.DeleteVertexArrays(1, ref array);
        shader.Dispose(); texture = array = 0;
    }
}
