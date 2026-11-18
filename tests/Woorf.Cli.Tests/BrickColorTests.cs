using System.Text.Json;
using Woorf.Engine;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Networking;

internal static class BrickColorTests
{
    internal static int Run()
    {
        var result = LuauRuntime.Execute("""
            local red = BrickColor.new("Bright red")
            assert(typeof(red) == "BrickColor" and red.Number == 21 and tostring(red) == "Bright red")
            assert(BrickColor.new(21) == red and BrickColor.palette(68) == red)
            assert(BrickColor.Red() == red and BrickColor.new(red.Color) == red)
            assert(BrickColor.new(red.r, red.g, red.b) == red)
            assert(BrickColor.new("missing") == BrickColor.Gray() and BrickColor.new(-1) == BrickColor.Gray())
            assert(not pcall(BrickColor.palette, 128))
            assert(typeof(BrickColor.random()) == "BrickColor" and typeof(BrickColor.Random()) == "BrickColor")
            local stored = Instance.new("BrickColorValue")
            local changed
            stored.Changed:Connect(function(value) changed = value end)
            stored.Value = red
            assert(changed == red and stored:Clone().Value == red)
            local part = Instance.new("Part")
            part:SetAttribute("Paint", red)
            assert(part:GetAttribute("Paint") == red)
            part.BrickColor = stored.Value
            assert(part.BrickColor == red and part.Color == red.Color)
            part.Color = BrickColor.Blue().Color
            assert(part.BrickColor == BrickColor.Blue())
            local team = Instance.new("Team", game:GetService("Teams"))
            team.TeamColor = red
            local player = Instance.new("Player", game:GetService("Players"))
            player.TeamColor = red
            assert(player.Team == team and player.TeamColor == red)
            """, "brickcolor.luau");
        if (result != 0) return result;

        var server = new DataModel();
        var red = new BrickColor(21);
        var part = (Part)Instance.Create("Part");
        part.Parent = server.Workspace;
        part.BrickColor = red;
        var value = (ValueObject)Instance.Create("BrickColorValue");
        value.Value = red;
        value.Parent = part;
        using var authority = new WireGraph(server, true);
        using var client = new WireGraph(new DataModel(), false);
        var wire = JsonSerializer.Serialize(authority.Capture(), SocketPeer.Options);
        client.Apply(JsonSerializer.Deserialize<WireNode[]>(wire, SocketPeer.Options)!);
        if (client.Find(authority.Identify(part)) is not Part copy || copy.BrickColor != red ||
            client.Find(authority.Identify(value)) is not ValueObject stored || !Equals(stored.Value, red))
            throw new Exception("Snapshot lost a BrickColor property or value.");
        Console.WriteLine("PASS BrickColor constructors, typed values, part and team properties, and wire replication");
        return 0;
    }
}
