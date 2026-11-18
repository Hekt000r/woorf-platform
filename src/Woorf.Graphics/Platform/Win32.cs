using System.Runtime.InteropServices;

namespace Woorf.Graphics.Platform;

internal static class Win32
{
    internal delegate nint WindowProcedure(nint window, uint message, nuint wparam, nint lparam);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WindowClass
    {
        internal uint Style;
        internal nint Procedure;
        internal int ClassExtra, WindowExtra;
        internal nint Instance, Icon, Cursor, Background;
        internal string? MenuName;
        internal string ClassName;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Message
    {
        internal nint Window;
        internal uint Id;
        internal nuint WParam;
        internal nint LParam;
        internal uint Time;
        internal int X, Y;
        internal uint Private;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Rectangle { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct PixelFormat
    {
        internal ushort Size, Version;
        internal uint Flags;
        internal byte Type, ColorBits, RedBits, RedShift, GreenBits, GreenShift, BlueBits, BlueShift;
        internal byte AlphaBits, AlphaShift, AccumBits, AccumRed, AccumGreen, AccumBlue, AccumAlpha;
        internal byte DepthBits, StencilBits, AuxBuffers, LayerType, Reserved;
        internal uint LayerMask, VisibleMask, DamageMask;
    }
    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern ushort RegisterClassW(ref WindowClass value);
    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool UnregisterClassW(string name, nint instance);
    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint CreateWindowExW(uint extended, string className, string title, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);
    [DllImport("user32")] internal static extern nint DefWindowProcW(nint window, uint message, nuint wparam, nint lparam);
    [DllImport("user32")] internal static extern bool DestroyWindow(nint window);
    [DllImport("user32")] internal static extern nint GetDC(nint window);
    [DllImport("user32")] internal static extern int ReleaseDC(nint window, nint dc);
    [DllImport("user32")] internal static extern bool ShowWindow(nint window, int command);
    [DllImport("user32")] internal static extern bool PeekMessageW(out Message message, nint window, uint min, uint max, uint remove);
    [DllImport("user32")] internal static extern bool TranslateMessage(ref Message message);
    [DllImport("user32")] internal static extern nint DispatchMessageW(ref Message message);
    [DllImport("user32")] internal static extern bool GetClientRect(nint window, out Rectangle rectangle);
    [DllImport("user32")] internal static extern bool AdjustWindowRect(ref Rectangle rectangle, uint style, bool menu);
    [DllImport("user32")] internal static extern short GetAsyncKeyState(int key);
    [StructLayout(LayoutKind.Sequential)]
    internal record struct Point(int X, int Y);
    [StructLayout(LayoutKind.Sequential)]
    internal struct GamepadState
    {
        internal uint Packet;
        internal ushort Buttons;
        internal byte LeftTrigger, RightTrigger;
        internal short LeftX, LeftY, RightX, RightY;
    }
    [DllImport("xinput1_4.dll")] internal static extern uint XInputGetState(uint index, out GamepadState state);
    [DllImport("user32")] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32")] internal static extern bool ClientToScreen(nint window, ref Point point);
    [DllImport("user32")] internal static extern nint SetCursor(nint cursor);
    [DllImport("user32")] internal static extern bool SetCursorPos(int x, int y);
    [DllImport("user32")] internal static extern nint GetForegroundWindow();
    [DllImport("user32")] internal static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("kernel32", CharSet = CharSet.Unicode)] internal static extern nint GetModuleHandleW(string? name);
    [DllImport("gdi32")] internal static extern int ChoosePixelFormat(nint dc, ref PixelFormat format);
    [DllImport("gdi32")] internal static extern bool SetPixelFormat(nint dc, int index, ref PixelFormat format);
    [DllImport("gdi32")] internal static extern bool SwapBuffers(nint dc);
    [DllImport("opengl32")] internal static extern nint wglCreateContext(nint dc);
    [DllImport("opengl32")] internal static extern bool wglDeleteContext(nint context);
    [DllImport("opengl32")] internal static extern bool wglMakeCurrent(nint dc, nint context);
    [DllImport("opengl32", CharSet = CharSet.Ansi)] internal static extern nint wglGetProcAddress(string name);
}
