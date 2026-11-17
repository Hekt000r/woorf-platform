using System.Numerics;
using Woorf.Engine.DataModel;
using Woorf.Engine.Networking;

static class MultiplayerPlayTests
{
    internal static int Run()
    {
        try
        {
            var game = new DataModel();
            var players = game.GetService("Players");
            var local = CreatePlayer(players, 1);
            var other = CreatePlayer(players, 2);
            var composer = new CharacterFrameComposer(local);
            var frame = composer.Capture([], 0);
            if (frame.Length != 16) throw new Exception("Both connected characters must contribute their eight visual parts.");
            other.Character!.PrimaryPart!.Position += new Woorf.Engine.Datatypes.Vector3(1, 0, 0);
            var walking = composer.Capture([], 0.05);
            var leg = walking[12].Transform;
            if (MathF.Abs(leg.M21) + MathF.Abs(leg.M23) < 0.1f)
                throw new Exception("Composing a moving peer must preserve its walking pose.");
            composer.Capture([], 0.06);
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 100; index++) composer.Capture([], 0.07);
            if (GC.GetAllocatedBytesForCurrentThread() != allocated)
                throw new Exception("Steady multiplayer composition must reuse peer membership and visual buffers.");
            other.Destroy();
            if (composer.Capture([], 0.1).Length != 8) throw new Exception("A departed player must disappear from the frame.");
            CheckNetworkViewer(local);
            Console.WriteLine("PASS multiplayer composition includes peers and removes departed characters");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL multiplayer play: " + error.Message); return 1; }
    }

    private static void CheckNetworkViewer(Player local)
    {
        CharacterInput sent = default;
        var viewer = new CharacterViewer(local, input => sent = input);
        var camera = new Woorf.Graphics.PerspectiveCamera();
        viewer.ApplyInput(camera, 1, 0, true, true, 0, 0, 0.016f);
        var humanoid = (Humanoid)local.Character!.FindFirstChild("Humanoid")!;
        if (!sent.ShouldJump || !sent.ShouldReset || sent.MoveDirection.Magnitude < 0.99f)
            throw new Exception("Network viewer must send movement and edge-triggered actions.");
        if (humanoid.Jump || humanoid.Health != 100 || humanoid.MoveDirection.Magnitude != 0)
            throw new Exception("Network viewer must leave replicated movement and health authoritative.");
        viewer.ApplyInput(camera, 0, 0, false, false, 0, 0, 0.016f);
        if (sent.MoveDirection.Magnitude != 0 || sent.ShouldJump || sent.ShouldReset)
            throw new Exception("Losing keyboard focus must send stopped movement and no actions.");
    }

    private static Player CreatePlayer(Instance players, int id)
    {
        var player = new Player(id) { Parent = players };
        var model = (Model)Instance.Create("Model");
        var root = (Part)Instance.Create("Part"); root.Position = new(id * 4, 2, 0); root.Parent = model;
        var humanoid = (Humanoid)Instance.Create("Humanoid"); humanoid.Parent = model;
        model.PrimaryPart = root; player.Character = model;
        return player;
    }
}
