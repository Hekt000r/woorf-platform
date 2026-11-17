using System.Runtime.InteropServices;
using Woorf.Graphics.OpenGL;
using Woorf.Graphics.Platform;

namespace Woorf.Graphics;

public sealed class OpenGlRenderer : IDisposable
{
    private readonly OpenGlWindow window;
    private readonly Gl gl;
    private ShaderProgram? shader;
    private GpuMesh? box, sphere;
    private SkyboxRenderer? skyRenderer;
    private bool isDisposed;
    private readonly InstanceBatch boxes = new(), spheres = new();
    public int LastFrameDrawCalls { get; private set; }
    public string DriverDescription { get; }

    public OpenGlRenderer(OpenGlWindow window)
    {
        this.window = window;
        window.MakeCurrent();
        gl = new Gl(window.GetProcedureAddress);
        DriverDescription = Marshal.PtrToStringAnsi(Gl.glGetString(0x1F02)) + " / " + Marshal.PtrToStringAnsi(Gl.glGetString(0x1F01));
        try
        {
            shader = new(gl, SceneShaders.Vertex, SceneShaders.Fragment);
            box = new(gl, MeshData.CreateBox()); sphere = new(gl, MeshData.CreateSphere());
            skyRenderer = new(gl);
            Gl.glEnable(0x0B71); Gl.glEnable(0x0B44);
            CheckErrors();
        }
        catch { Dispose(); throw; }
    }

    public void Draw(IReadOnlyList<RenderItem> items, PerspectiveCamera camera, Skybox? sky = null)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        LastFrameDrawCalls = 0;
        boxes.Begin(); spheres.Begin();
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            (item.Shape == MeshShape.Sphere ? spheres : boxes).Append(item);
        }
        window.MakeCurrent();
        var (width, height) = window.GetSize();
        if (width == 0 || height == 0) return;
        Gl.glViewport(0, 0, width, height);
        Gl.glClearColor(0.055f, 0.075f, 0.11f, 1);
        Gl.glClear(0x4000 | 0x0100);
        if (sky is not null) { skyRenderer!.Draw(sky, camera, (float)width / height); LastFrameDrawCalls++; }
        shader!.Use(); shader.SetMatrix("viewProjection", camera.GetViewProjection((float)width / height));
        shader.SetVector("eye", camera.Position);
        if (boxes.Count > 0) { box!.Draw(boxes); LastFrameDrawCalls++; }
        if (spheres.Count > 0) { sphere!.Draw(spheres); LastFrameDrawCalls++; }
        gl.BindVertexArray(0);
        CheckErrors();
    }

    public void Capture(string path)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        window.MakeCurrent();
        var (width, height) = window.GetSize();
        if (width == 0 || height == 0) throw new InvalidOperationException("Cannot capture a minimized framebuffer.");
        var pixels = new byte[checked(width * height * 3)];
        Gl.glReadBuffer(0x0405); Gl.glPixelStorei(0x0D05, 1);
        Gl.glReadPixels(0, 0, width, height, 0x80E0, 0x1401, pixels);
        CheckErrors();
        FrameCapture.WriteBitmap(path, width, height, pixels);
    }

    private static void CheckErrors()
    {
        var error = Gl.glGetError();
        if (error != 0) throw new InvalidOperationException($"OpenGL error 0x{error:X}.");
    }
    public void Dispose()
    {
        if (isDisposed) return;
        window.MakeCurrent();
        skyRenderer?.Dispose(); sphere?.Dispose(); box?.Dispose(); shader?.Dispose();
        isDisposed = true;
    }
}
