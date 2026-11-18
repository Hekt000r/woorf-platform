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
    private readonly SceneDrawState state = new();
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
        window.MakeCurrent();
        var (width, height) = window.GetSize();
        if (width == 0 || height == 0) return;
        Gl.glViewport(0, 0, width, height);
        var clear = SceneDrawState.ClearColor;
        Gl.glClearColor(clear.X, clear.Y, clear.Z, 1);
        Gl.glClear(0x4000 | 0x0100);
        if (sky is not null) { skyRenderer!.Draw(sky, camera, (float)width / height); LastFrameDrawCalls++; }
        state.Prepare(items, camera, (float)width / height);
        shader!.Use(); shader.SetMatrix("viewProjection", state.ViewProjection);
        shader.SetVector("eye", state.Eye);
        if (state.Boxes.Count > 0) { box!.Draw(state.Boxes); LastFrameDrawCalls++; }
        if (state.Spheres.Count > 0) { sphere!.Draw(state.Spheres); LastFrameDrawCalls++; }
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
