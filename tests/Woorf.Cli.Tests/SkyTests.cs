using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Networking;

static class SkyTests
{
    internal static int Run()
    {
        try
        {
            var server = new DataModel(); var client = new DataModel();
            var sky = (Sky)Instance.Create("Sky"); sky.Parent = server.GetService("Lighting");
            sky.SetTexture("SkyboxFt", "custom-front.png"); sky.SkyboxOrientation = new(10, 20, 30);
            var replicas = new ReplicaMap(server, client); replicas.Synchronize();
            var copy = (Sky?)client.FindService("Lighting")?.FindFirstChild("Sky");
            Require(copy?.GetTexture("SkyboxFt") == "custom-front.png" && copy.SkyboxOrientation == new Vector3(10, 20, 30),
                "Local clients must receive Lighting and sky face/orientation values.");
            using var source = new WireGraph(server, true);
            var remote = new DataModel(); using var target = new WireGraph(remote, false);
            var initial = source.Capture();
            var baseline = new ReplicationBaseline(); baseline.Initialize(initial);
            var updates = new ReplicaUpdates(target); updates.Initialize(initial);
            var wireCopy = (Sky?)remote.FindService("Lighting")?.FindFirstChild("Sky");
            Require(wireCopy?.GetTexture("SkyboxFt") == "custom-front.png", "TCP snapshots must include sky faces.");
            sky.SetTexture("SkyboxFt", "replacement.png"); sky.SkyboxOrientation = new(0, 90, 0);
            var changed = baseline.BuildDelta(source.Capture(), []) ?? throw new Exception("Sky edit produced no wire delta.");
            updates.Apply(changed); replicas.Synchronize();
            Require(wireCopy!.GetTexture("SkyboxFt") == "replacement.png" && wireCopy.SkyboxOrientation == new Vector3(0, 90, 0)
                && copy!.GetTexture("SkyboxFt") == "replacement.png", "Live sky edits must replicate.");
            sky.Destroy(); target.Apply(source.Capture()); replicas.Synchronize();
            Require(wireCopy.IsDestroyed && copy!.IsDestroyed, "Removed skies must disappear from clients.");
            Console.WriteLine("PASS Lighting skies replicate faces orientation updates and removal locally and over wire snapshots");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL sky: " + error.Message); return 1; }
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
