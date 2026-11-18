using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Networking;
using System.Text.Json;

static class GuiReplicationTests
{
    internal static int Run()
    {
        try { VerifyLocal(); VerifyWire(); VerifyRemoteValues(); Console.WriteLine("PASS GUI local and wire snapshots preserve edits and client property isolation"); return 0; }
        catch (Exception error) { Console.Error.WriteLine("FAIL GUI replication: " + error.Message); return 1; }
    }

    private static (DataModel Game, GuiInstance Label) Create()
    {
        var game = new DataModel();
        var screen = (GuiInstance)Instance.Create("ScreenGui"); screen.Parent = game.GetService("ReplicatedStorage");
        var label = (GuiInstance)Instance.Create("TextLabel"); label.Parent = screen;
        label.SetGuiProperty("Text", "Stage 2"); label.SetGuiProperty("Size", new UDim2(0.5f, -20, 0, 50));
        label.SetGuiProperty("AnchorPoint", new Vector2(0.5f, 0));
        label.SetGuiProperty("TextXAlignment", GuiEnum.From("TextXAlignment", "Left"));
        return (game, label);
    }

    private static void VerifyLocal()
    {
        var (server, label) = Create(); var client = new DataModel();
        var replicas = new ReplicaMap(server, client); replicas.Synchronize();
        var copy = (GuiInstance)replicas.ToClient(label)!;
        Require(copy.Get<string>("Text") == "Stage 2", "Local GUI snapshot lost text.");
        Require(copy.Get<UDim2>("Size") == new UDim2(0.5f, -20, 0, 50), "Local GUI snapshot lost dimensions.");
        copy.SetGuiProperty("Text", "Local"); label.SetGuiProperty("Visible", false); replicas.Synchronize();
        Require(copy.Get<string>("Text") == "Local" && !copy.Get<bool>("Visible"), "Unrelated server edits replaced local GUI properties.");
        label.SetGuiProperty("Text", "Stage 3"); replicas.Synchronize();
        Require(copy.Get<string>("Text") == "Stage 3", "Authoritative GUI change failed to replace same local property.");
    }

    private static void VerifyWire()
    {
        var (server, label) = Create(); using var source = new WireGraph(server, true);
        var client = new DataModel(); using var target = new WireGraph(client, false);
        var initial = source.Capture(); var baseline = new ReplicationBaseline(); baseline.Initialize(initial);
        var updates = new ReplicaUpdates(target);
        updates.Initialize(JsonSerializer.Deserialize<WireNode[]>(JsonSerializer.Serialize(initial, SocketPeer.Options), SocketPeer.Options)!);
        var copy = (GuiInstance)target.Find(source.Identify(label))!;
        Require(copy.Get<string>("Text") == "Stage 2" && copy.Get<Vector2>("AnchorPoint") == new Vector2(0.5f, 0), "Wire GUI snapshot lost properties.");
        copy.SetGuiProperty("Text", "Local"); label.SetGuiProperty("Visible", false);
        var delta = baseline.BuildDelta(source.Capture(), []) ?? throw new Exception("GUI edit produced no delta.");
        updates.Apply(JsonSerializer.Deserialize<WirePacket>(JsonSerializer.Serialize(delta, SocketPeer.Options), SocketPeer.Options)!);
        Require(copy.Get<string>("Text") == "Local" && !copy.Get<bool>("Visible"), "Wire delta overwrote unrelated local text.");
        label.Destroy(); target.Apply(source.Capture());
        Require(copy.IsDestroyed, "Wire removed GUI remained alive.");
    }

    private static void VerifyRemoteValues()
    {
        foreach (var value in new object[] { new Vector2(12, 30), new UDim(0.5f, -2), new UDim2(1, -10, 0, 40) })
            Require(Equals(WireValue.Encode(value, _ => 0).Decode(_ => null), value), "Remote GUI datatype lost value.");
    }

    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
