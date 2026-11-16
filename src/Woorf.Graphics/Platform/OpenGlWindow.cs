using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Woorf.Graphics.Platform;

public sealed class OpenGlWindow : IDisposable
{
    private readonly string className = "Woorf-" + Guid.NewGuid().ToString("N");
    private readonly Win32.WindowProcedure procedure;
    private readonly int threadId = Environment.CurrentManagedThreadId;
    private nint window, dc, context;
    private bool isRegistered, isDisposed, shouldClose;
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint CreateContext(nint dc, nint shared, int[] attributes);

    public OpenGlWindow(string title, int width = 960, int height = 640, bool isVisible = true)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("The initial OpenGL window backend requires Windows.");
        if (width < 1 || height < 1 || width > 8192 || height > 8192) throw new ArgumentOutOfRangeException(nameof(width));
        procedure = ProcessMessage;
        try { CreateWindow(title, width, height); CreateOpenGlContext(); if (isVisible) Win32.ShowWindow(window, 5); }
        catch { Dispose(); throw; }
    }

    private void CreateWindow(string title, int width, int height)
    {
        var instance = Win32.GetModuleHandleW(null);
        var definition = new Win32.WindowClass { Style = 0x20, Procedure = Marshal.GetFunctionPointerForDelegate(procedure),
            Instance = instance, ClassName = className };
        if (Win32.RegisterClassW(ref definition) == 0) throw new Win32Exception();
        isRegistered = true;
        const uint style = 0x00CF0000 | 0x02000000 | 0x04000000;
        var bounds = new Win32.Rectangle { Right = width, Bottom = height };
        if (!Win32.AdjustWindowRect(ref bounds, style, false)) throw new Win32Exception();
        window = Win32.CreateWindowExW(0, className, title, style, 100, 100,
            bounds.Right - bounds.Left, bounds.Bottom - bounds.Top, 0, 0, instance, 0);
        if (window == 0) throw new Win32Exception();
        dc = Win32.GetDC(window);
        if (dc == 0) throw new Win32Exception();
    }

    private void CreateOpenGlContext()
    {
        var format = new Win32.PixelFormat { Size = (ushort)Marshal.SizeOf<Win32.PixelFormat>(), Version = 1,
            Flags = 0x4 | 0x20 | 0x1, ColorBits = 32, DepthBits = 24, StencilBits = 8 };
        var index = Win32.ChoosePixelFormat(dc, ref format);
        if (index == 0 || !Win32.SetPixelFormat(dc, index, ref format)) throw new Win32Exception();
        context = Win32.wglCreateContext(dc);
        if (context == 0 || !Win32.wglMakeCurrent(dc, context)) throw new InvalidOperationException("Could not create the bootstrap OpenGL context.");
        var create = Marshal.GetDelegateForFunctionPointer<CreateContext>(GetProcedureAddress("wglCreateContextAttribsARB"));
        var modern = create(dc, 0, [0x2091, 3, 0x2092, 3, 0x9126, 1, 0]);
        if (modern == 0) throw new NotSupportedException("An OpenGL 3.3 core driver is required.");
        Win32.wglMakeCurrent(0, 0);
        Win32.wglDeleteContext(context);
        context = modern;
        MakeCurrent();
    }

    public nint GetProcedureAddress(string name)
    {
        AssertThread();
        var address = Win32.wglGetProcAddress(name);
        if (address is 0 or 1 or 2 or 3 or -1) throw new NotSupportedException("OpenGL entry point unavailable: " + name);
        return address;
    }
    public (int Width, int Height) GetSize()
    {
        AssertThread();
        if (!Win32.GetClientRect(window, out var rectangle)) throw new Win32Exception();
        return (rectangle.Right, rectangle.Bottom);
    }
    public bool PollEvents()
    {
        AssertThread();
        while (Win32.PeekMessageW(out var message, window, 0, 0, 1))
        {
            Win32.TranslateMessage(ref message); Win32.DispatchMessageW(ref message);
        }
        if (IsKeyDown(0x1B)) shouldClose = true;
        return !shouldClose;
    }
    public bool IsKeyDown(int key) => Win32.GetForegroundWindow() == window && (Win32.GetAsyncKeyState(key) & 0x8000) != 0;
    public void MakeCurrent()
    {
        AssertThread();
        if (!Win32.wglMakeCurrent(dc, context)) throw new InvalidOperationException("Could not activate OpenGL context.");
    }
    public void Present() { AssertThread(); if (!Win32.SwapBuffers(dc)) throw new Win32Exception(); }
    private nint ProcessMessage(nint handle, uint message, nuint wparam, nint lparam)
    {
        if (message == 0x10) { shouldClose = true; return 0; }
        return Win32.DefWindowProcW(handle, message, wparam, lparam);
    }
    private void AssertThread()
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        if (Environment.CurrentManagedThreadId != threadId) throw new InvalidOperationException("OpenGL window access requires its creating thread.");
    }
    public void Dispose()
    {
        if (isDisposed) return;
        AssertThread();
        if (context != 0) { Win32.wglMakeCurrent(0, 0); Win32.wglDeleteContext(context); }
        if (dc != 0) Win32.ReleaseDC(window, dc);
        if (window != 0) Win32.DestroyWindow(window);
        if (isRegistered) Win32.UnregisterClassW(className, Win32.GetModuleHandleW(null));
        isDisposed = true;
        GC.KeepAlive(procedure);
    }
}
