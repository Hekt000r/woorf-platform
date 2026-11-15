using System.Diagnostics;

namespace Woorf.Engine.Scheduling;

internal sealed class MonotonicClock : IRuntimeClock
{
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    public double ElapsedSeconds => stopwatch.Elapsed.TotalSeconds;

    public void Sleep(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return;
        var milliseconds = Math.Clamp(Math.Ceiling(duration.TotalMilliseconds), 1, int.MaxValue);
        Thread.Sleep(TimeSpan.FromMilliseconds(milliseconds));
    }
}
