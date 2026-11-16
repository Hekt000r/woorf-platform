using System.Diagnostics;

namespace Woorf.Engine.Networking;

internal sealed class InboundBudget
{
    private double requests = 120;
    private double bytes = 4 * 1024 * 1024 + 4;
    private long sampled = Stopwatch.GetTimestamp();

    private void Refill()
    {
        var now = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(sampled, now).TotalSeconds;
        sampled = now;
        requests = Math.Min(120, requests + elapsed * 120);
        bytes = Math.Min(4 * 1024 * 1024 + 4, bytes + elapsed * 1024 * 1024);
    }

    internal void Receive(int count)
    {
        Refill();
        bytes -= count;
        if (bytes < 0) throw new InvalidDataException("Inbound byte rate exceeded.");
    }

    internal void Dispatch()
    {
        Refill();
        requests -= 1;
        if (requests < 0) throw new InvalidDataException("Inbound request rate exceeded.");
    }
}
