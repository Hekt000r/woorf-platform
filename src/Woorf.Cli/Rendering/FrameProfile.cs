using System.Diagnostics;

internal sealed class FrameProfile(string path) : IDisposable
{
    private readonly List<string> samples = ["frame,simulation_ms,scene_ms,render_ms,present_ms,frame_ms,allocated_bytes,parts,draw_calls"];

    internal void Record(long start, long simulated, long collected, long rendered, long presented, long allocated, int parts, int draws)
    {
        double Ms(long from, long to) => Stopwatch.GetElapsedTime(from, to).TotalMilliseconds;
        samples.Add(FormattableString.Invariant($"{samples.Count},{Ms(start, simulated):F3},{Ms(simulated, collected):F3},{Ms(collected, rendered):F3},{Ms(rendered, presented):F3},{Ms(start, Stopwatch.GetTimestamp()):F3},{GC.GetAllocatedBytesForCurrentThread() - allocated},{parts},{draws}"));
    }

    public void Dispose() => File.WriteAllLines(path, samples);
}
