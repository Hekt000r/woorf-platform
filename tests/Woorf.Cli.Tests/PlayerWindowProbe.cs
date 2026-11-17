using System.ComponentModel;
using System.Runtime.InteropServices;

static class PlayerWindowProbe
{
    internal sealed record Window(bool IsVisible, int Left, int Top);
    internal static Window[] Capture(int launcherId)
    {
        var children = FindChildren(launcherId);
        var windows = new List<Window>();
        EnumWindows((handle, _) =>
        {
            GetWindowThreadProcessId(handle, out var process);
            if (!children.Contains(process)) return true;
            var title = new System.Text.StringBuilder(256);
            GetWindowTextW(handle, title, title.Capacity);
            if (!title.ToString().StartsWith("Woorf |", StringComparison.Ordinal)) return true;
            if (!GetWindowRect(handle, out var bounds)) throw new Win32Exception();
            windows.Add(new(IsWindowVisible(handle), bounds.Left, bounds.Top));
            return true;
        }, 0);
        return windows.ToArray();
    }

    private static HashSet<uint> FindChildren(int parent)
    {
        var snapshot = CreateToolhelp32Snapshot(2, 0);
        if (snapshot == -1) throw new Win32Exception();
        try
        {
            var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
            var children = new HashSet<uint>();
            if (!Process32FirstW(snapshot, ref entry)) throw new Win32Exception();
            do { if (entry.ParentProcessId == parent) children.Add(entry.ProcessId); }
            while (Process32NextW(snapshot, ref entry));
            return children;
        }
        finally { CloseHandle(snapshot); }
    }

    private delegate bool WindowVisitor(nint window, nint data);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry
    {
        internal uint Size, Usage, ProcessId;
        internal nuint Heap;
        internal uint Module, Threads, ParentProcessId;
        internal int Priority;
        internal uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] internal string Name;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Rectangle { internal int Left, Top, Right, Bottom; }
    [DllImport("kernel32", SetLastError = true)] private static extern nint CreateToolhelp32Snapshot(uint flags, uint process);
    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool Process32FirstW(nint snapshot, ref ProcessEntry entry);
    [DllImport("kernel32", CharSet = CharSet.Unicode)] private static extern bool Process32NextW(nint snapshot, ref ProcessEntry entry);
    [DllImport("kernel32")] private static extern bool CloseHandle(nint handle);
    [DllImport("user32")] private static extern bool EnumWindows(WindowVisitor visitor, nint data);
    [DllImport("user32")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32", CharSet = CharSet.Unicode)] private static extern int GetWindowTextW(nint window, System.Text.StringBuilder text, int count);
    [DllImport("user32")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32", SetLastError = true)] private static extern bool GetWindowRect(nint window, out Rectangle rectangle);
}
