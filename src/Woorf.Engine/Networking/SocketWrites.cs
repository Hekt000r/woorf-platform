using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;

namespace Woorf.Engine.Networking;

internal sealed class SocketWrites(Socket socket)
{
    internal const int MaxQueuedBytes = 8 * 1024 * 1024;
    private const int TickBudget = 256 * 1024;
    private readonly Queue<Frame> ready = [];
    private readonly Queue<Frame> deferred = [];
    private int offset;
    private long lastProgress = Stopwatch.GetTimestamp();
    internal long PeakQueuedBytes { get; private set; }
    internal long QueuedBytes { get; private set; }
    internal long SentBytes { get; private set; }
    internal long ReplicationBytes { get; private set; }
    internal bool IsOverloaded { get; private set; }

    internal void Enqueue(WirePacket packet, bool shouldDefer)
    {
        if (IsOverloaded) return;
        var payload = JsonSerializer.SerializeToUtf8Bytes(packet, SocketPeer.Options);
        if (payload.Length > 4 * 1024 * 1024) throw new InvalidDataException("Socket frame is too large.");
        if (QueuedBytes + payload.Length + 4 > MaxQueuedBytes)
        {
            IsOverloaded = true;
            throw new IOException("Socket outgoing queue exceeded its byte budget.");
        }
        var bytes = new byte[payload.Length + 4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, payload.Length);
        payload.CopyTo(bytes, 4);
        var frame = new Frame(bytes, packet.Kind is "welcome" or "delta");
        (shouldDefer ? deferred : ready).Enqueue(frame);
        if (QueuedBytes == 0) lastProgress = Stopwatch.GetTimestamp();
        QueuedBytes += bytes.Length;
        PeakQueuedBytes = Math.Max(PeakQueuedBytes, QueuedBytes);
    }

    internal void ReleaseDeferred()
    {
        while (deferred.TryDequeue(out var frame)) ready.Enqueue(frame);
    }

    internal void Pump()
    {
        if (QueuedBytes > 0 && Stopwatch.GetElapsedTime(lastProgress).TotalSeconds > 5)
        {
            IsOverloaded = true;
            throw new IOException("Socket writes made no progress for five seconds.");
        }
        var budget = TickBudget;
        while (budget > 0 && ready.TryPeek(out var frame))
        {
            int sent;
            try { sent = socket.Send(frame.Bytes, offset, Math.Min(frame.Bytes.Length - offset, budget), SocketFlags.None); }
            catch (SocketException error) when (error.SocketErrorCode is SocketError.WouldBlock
                or SocketError.IOPending or SocketError.NoBufferSpaceAvailable) { return; }
            if (sent == 0) throw new IOException("Socket closed during write.");
            lastProgress = Stopwatch.GetTimestamp();
            offset += sent;
            budget -= sent;
            QueuedBytes -= sent;
            SentBytes += sent;
            if (frame.IsReplication) ReplicationBytes += sent;
            if (offset == frame.Bytes.Length) { ready.Dequeue(); offset = 0; }
        }
    }

    internal void Clear()
    {
        ready.Clear();
        deferred.Clear();
        offset = 0;
        QueuedBytes = 0;
    }

    private sealed record Frame(byte[] Bytes, bool IsReplication);
}
