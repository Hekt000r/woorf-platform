using System.Diagnostics;

namespace Woorf.Engine.Networking;

public sealed record SocketTransportMetrics(long QueuedBytes, long SentBytes, long ReplicationBytes,
    double TickMilliseconds, int ConnectedPeers, long SlowDisconnects, long PeakPeerQueuedBytes,
    long ReplicationNodesCaptured, long ReplicationFullScans);

internal sealed partial class SocketRuntime
{
    private readonly Action<SocketTransportMetrics>? observe;
    private long sentBeforeDisconnect;
    private long replicationBeforeDisconnect;
    private long slowDisconnects;
    private long peakPeerQueuedBytes;

    private void RecordDisconnectedPeer(SocketPeer peer)
    {
        sentBeforeDisconnect += peer.Writes.SentBytes;
        replicationBeforeDisconnect += peer.Writes.ReplicationBytes;
        peakPeerQueuedBytes = Math.Max(peakPeerQueuedBytes, peer.Writes.PeakQueuedBytes);
        if (peer.Writes.IsOverloaded) slowDisconnects++;
    }

    private void ReportMetrics(long started)
    {
        if (observe is null) return;
        var peak = peers.Select(peer => peer.Writes.PeakQueuedBytes).DefaultIfEmpty(0).Max();
        peakPeerQueuedBytes = Math.Max(peakPeerQueuedBytes, peak);
        observe(new(
            peers.Sum(peer => peer.Writes.QueuedBytes),
            sentBeforeDisconnect + peers.Sum(peer => peer.Writes.SentBytes),
            replicationBeforeDisconnect + peers.Sum(peer => peer.Writes.ReplicationBytes),
            Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            peers.Count(peer => peer.Player is not null),
            slowDisconnects,
            peakPeerQueuedBytes, graph.CapturedNodes, graph.FullScans));
    }
}
