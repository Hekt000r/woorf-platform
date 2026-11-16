using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Woorf.Engine.Networking;
using Woorf.Engine.Projects;

static class SocketMetricsTests
{
    private static readonly List<SocketTransportMetrics> samples = [];

    internal static async Task<int> RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-metrics-" + Guid.NewGuid().ToString("N"));
        var original = Console.Out;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Task<int>? server = null;
        try
        {
            NetworkProjectTests.Write(root, "woorf.project.json", "{\"name\":\"Metrics\",\"root\":\"src\"}");
            Directory.CreateDirectory(Path.Combine(root, "src"));
            using var output = new StartupWriter();
            Console.SetOut(output);
            samples.Clear();
            var project = ProjectLoader.Load(root);
            server = Task.Run(() => SocketTransport.Listen(project, "127.0.0.1:0", cancellation.Token, Collect));
            var address = IPEndPoint.Parse(await output.Address.Task.WaitAsync(cancellation.Token));
            using var client = new TcpClient();
            await client.ConnectAsync(address.Address, address.Port, cancellation.Token);
            var payload = Encoding.UTF8.GetBytes("{\"Kind\":\"hello\"}");
            var header = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);
            await client.GetStream().WriteAsync(header, cancellation.Token);
            await client.GetStream().WriteAsync(payload, cancellation.Token);
            await client.GetStream().ReadExactlyAsync(header, cancellation.Token);
            var length = BinaryPrimitives.ReadInt32BigEndian(header);
            await client.GetStream().ReadExactlyAsync(new byte[length], cancellation.Token);
            cancellation.Cancel();
            if (!Equals(await server, 0)) throw new Exception("Metrics server failed.");
            if (!samples.Any(sample => sample.SentBytes == length + 4
                && sample.ReplicationBytes == length + 4 && sample.QueuedBytes == 0
                && sample.ConnectedPeers == 1 && sample.TickMilliseconds >= 0))
                throw new Exception("Transport measurements did not match the frame delivered on the socket.");
            Console.SetOut(original);
            Console.WriteLine("PASS measures queued bytes replication traffic and transport tick time");
            return 0;
        }
        catch (Exception error)
        {
            Console.SetOut(original);
            Console.Error.WriteLine("FAIL measures queued bytes replication traffic and transport tick time: " + error.Message);
            return 1;
        }
        finally
        {
            cancellation.Cancel();
            if (server is not null) { try { await server; } catch { } }
            Console.SetOut(original);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void Collect(SocketTransportMetrics value) => samples.Add(value);

    private sealed class StartupWriter : StringWriter
    {
        internal TaskCompletionSource<string> Address { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override void WriteLine(string? value)
        {
            if (value?.StartsWith("Listening ") == true) Address.TrySetResult(value["Listening ".Length..]);
        }
    }
}
