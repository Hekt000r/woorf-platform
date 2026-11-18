using Woorf.Engine.DataModel;

internal static class PresentationTests
{
    internal static int Run()
    {
        try
        {
            CheckFacingWithoutSmallFrameDeltas();
            var game = new DataModel();
            var sky = (Sky)Instance.Create("Sky"); sky.Parent = game.GetService("Lighting");
            var floor = (Part)Instance.Create("Part"); floor.Anchored = true; floor.Parent = game.Workspace;
            using var view = new WorldView(game, null);
            var frame = view.Capture(0);
            if (frame.Items.Length != 1 || frame.Sky is null) throw new Exception("Shared world capture must include geometry and the scripted sky.");
            sky.Destroy();
            frame = view.Capture(1);
            if (frame.Sky is not null) throw new Exception("Removing a sky must clear both platforms' background.");
            Console.WriteLine("PASS shared presentation captures world geometry and live sky membership");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL presentation: " + error.Message); return 1; }
    }

    private static void CheckFacingWithoutSmallFrameDeltas()
    {
        var root = (Part)Instance.Create("Part");
        var humanoid = (Humanoid)Instance.Create("Humanoid");
        var view = new CharacterView(); view.Capture(root, humanoid, 0);
        humanoid.Move(new(1,0,0)); root.Position = new(8,0,0);
        var torso = view.Capture(root, humanoid, 0.5)[0].Transform;
        if (MathF.Abs(torso.M11) > 0.001f || torso.M13 < 1)
            throw new Exception("Character facing must follow movement even when a slow browser frame spans more than four studs.");
        humanoid.Move(new(0,0,1));
        torso = view.Capture(root, humanoid, 0.51)[0].Transform;
        if (torso.M11 > -1)
            throw new Exception("Movement input must turn the character between physics publications too.");
    }
}
