using System.Text.Json;
using Woorf.Engine.DataModel;
using Woorf.Engine.Networking;

static class PhysicsTouchReplicationTests
{
    internal static int Run()
    {
        var failures = 0;
        foreach (var isWire in new[] { false, true })
        {
            try { CheckReplication(isWire); Console.WriteLine("PASS touch CanTouch replicates and disconnects stale " + (isWire ? "wire" : "local") + " handlers"); }
            catch (Exception error) { failures++; Console.Error.WriteLine("FAIL touch replication: " + error.Message); }
        }
        return failures;
    }

    private static void CheckReplication(bool isWire)
    {
        var server = new DataModel(); var client = new DataModel();
        var part = (Part)Instance.Create("Part"); part.Parent = server.Workspace;
        var local = new ReplicaMap(server, client);
        using var source = new WireGraph(server, true);
        using var receiver = new WireGraph(client, false);
        Part Synchronize()
        {
            if (!isWire) { local.Synchronize(); return (Part)local.ToClient(part)!; }
            var serialized = JsonSerializer.Serialize(source.Capture());
            receiver.Apply(JsonSerializer.Deserialize<WireNode[]>(serialized)!);
            return (Part)receiver.Find(source.Identify(part))!;
        }
        var replica = Synchronize();
        var connection = replica.Touched.Connect(_ => { });
        part.CanTouch = false;
        Synchronize();
        if (replica.CanTouch || connection.Connected) throw new Exception("Disabling touch did not reach replica or release its connection.");
        part.CanTouch = true;
        Synchronize();
        if (!replica.CanTouch || connection.Connected) throw new Exception("Re-enabling touch revived an obsolete subscription or failed to replicate.");
        var fresh = replica.Touched.Connect(_ => { });
        if (!fresh.Connected) throw new Exception("Replica rejected a fresh subscription after touch was enabled.");
    }
}
