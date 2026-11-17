using Woorf.Engine.DataModel;
using Woorf.Engine.Networking;

static class CharacterReplicationTests
{
    internal static int Run()
    {
        try
        {
            var server = new DataModel();
            var player = (Player)Instance.Create("Player"); player.Parent = server.GetService("Players");
            var character = (Model)Instance.Create("Model"); character.Name = "Character"; character.Parent = server.Workspace;
            var root = (Part)Instance.Create("Part"); root.Name = "HumanoidRootPart"; root.Anchored = true; root.Parent = character;
            var humanoid = (Humanoid)Instance.Create("Humanoid"); humanoid.Parent = character; humanoid.WalkSpeed = 9;
            humanoid.Move(new(1, 0, 0)); character.PrimaryPart = root; player.Character = character;
            var local = new DataModel(); var replicas = new ReplicaMap(server, local); replicas.Synchronize();
            var copy = (Player)replicas.ToClient(player)!;
            if (copy.Character != replicas.ToClient(character) || ((Humanoid)replicas.ToClient(humanoid)!).WalkSpeed != 9)
                throw new Exception("Local snapshots omitted the character reference or movement tuning.");
            var remote = new DataModel(); using var source = new WireGraph(server, true); using var receiver = new WireGraph(remote, false);
            source.Capture();
            var playerId = source.Identify(player);
            receiver.Apply(source.Capture().OrderBy(node => node.ClassName == "Player" ? 0 : 1).ToArray());
            var wirePlayer = (Player)receiver.Find(playerId)!;
            if (wirePlayer.Character?.PrimaryPart?.Name != "HumanoidRootPart")
                throw new Exception("Wire snapshot omitted a fully initialized character graph.");
            var added = 0;
            wirePlayer.CharacterAdded.Connect(arguments =>
            {
                if (((Model)arguments[0]!).PrimaryPart is null) return;
                added++;
            });
            character.Parent = server.GetService("ServerStorage");
            replicas.Synchronize(); receiver.Apply(source.Capture());
            if (copy.Character is not null || wirePlayer.Character is not null) throw new Exception("Private character leaked across replication.");
            character.Parent = server.Workspace;
            replicas.Synchronize(); receiver.Apply(source.Capture().OrderBy(node => node.ClassName == "Player" ? 0 : 1).ToArray());
            if (copy.Character is null || wirePlayer.Character?.PrimaryPart is null || added != 1)
                throw new Exception("Character reference did not recover after visibility returned.");
            Console.WriteLine("PASS characters and movement replicate through local and wire visibility transitions");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL character replication: " + error.Message); return 1; }
    }
}
