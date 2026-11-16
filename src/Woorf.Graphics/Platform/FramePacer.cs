using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Woorf.Graphics.Platform;

public sealed class FramePacer : IDisposable
{
    private readonly SafeWaitHandle timer;

    public FramePacer()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("The current frame timer backend requires Windows.");
        timer = CreateWaitableTimerExW(0, null, 2, 0x100002);
        if (timer.IsInvalid) { timer.Dispose(); throw new Win32Exception(); }
    }

    public void WaitUntil(long deadline)
    {
        ObjectDisposedException.ThrowIf(timer.IsClosed, this);
        var remaining = Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp(), deadline).Ticks;
        if (remaining <= 0) return;
        var dueTime = -remaining;
        if (!SetWaitableTimer(timer, ref dueTime, 0, 0, 0, false)) throw new Win32Exception();
        if (WaitForSingleObject(timer, uint.MaxValue) != 0) throw new Win32Exception();
    }

    public void Dispose() => timer.Dispose();

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeWaitHandle CreateWaitableTimerExW(nint attributes, string? name, uint flags, uint access);
    [DllImport("kernel32", SetLastError = true)]
    private static extern bool SetWaitableTimer(SafeWaitHandle timer, ref long dueTime, int period, nint callback, nint argument, bool resume);
    [DllImport("kernel32", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeWaitHandle handle, uint milliseconds);
}
