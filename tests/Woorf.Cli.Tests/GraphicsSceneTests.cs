using Woorf.Engine.DataModel;

static class GraphicsSceneTests
{
    internal static int Run()
    {
        try
        {
            CheckHiddenCharacterRoots();
            var workspace = new DataModel().Workspace;
            var part = (Part)Instance.Create("Part"); part.Parent = workspace; part.Anchored = true;
            using var scene = new WorkspaceScene(workspace);
            var first = scene.Capture();
            var second = scene.Capture();
            if (!ReferenceEquals(first, second) || scene.FullScans != 1 || scene.CapturedParts != 1)
                throw new Exception("Unchanged scenes must reuse captured geometry without rescanning Workspace.");
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 100; index++) scene.Capture();
            if (GC.GetAllocatedBytesForCurrentThread() != allocated) throw new Exception("Static scene captures must not allocate.");
            part.Position = new(2, 0, 0);
            var moved = scene.Capture();
            if (scene.FullScans != 1 || scene.CapturedParts != 2 || moved[0].Transform.M41 != 2)
                throw new Exception("A transform update must recapture only the changed Part.");
            part.SetAttribute("RenderColor", new Woorf.Engine.Datatypes.Color3(1, 0.1f, 0.2f));
            var colored = scene.Capture();
            if (colored[0].Color != new System.Numerics.Vector3(1, 0.1f, 0.2f) || scene.FullScans != 1)
                throw new Exception("Scripted colors must update cached geometry without a hierarchy scan.");
            part.SetAttribute("RenderColor", null);
            if (scene.Capture()[0].Color != new System.Numerics.Vector3(0.25f, 0.32f, 0.42f))
                throw new Exception("Removing a color must restore the default palette.");
            var storage = Instance.Create("Folder"); part.Parent = storage;
            if (scene.Capture().Length != 0) throw new Exception("Reparented geometry remained in the renderer cache.");
            part.Parent = workspace;
            if (scene.Capture().Length != 1) throw new Exception("Reinserted geometry was not captured.");
            part.Destroy();
            if (scene.Capture().Length != 0) throw new Exception("Scene cache retained destroyed geometry.");
            Console.WriteLine("PASS renderer scene cache reuses static geometry and updates dirty Parts");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL renderer scene cache: " + error.Message); return 1; }
    }

    private static void CheckHiddenCharacterRoots()
    {
        var workspace = new DataModel().Workspace;
        var character = (Model)Instance.Create("Model");
        character.SetAttribute("WoorfCharacter", true);
        character.Parent = workspace;
        var root = (Part)Instance.Create("Part");
        root.Name = "HumanoidRootPart"; root.Parent = character;
        using var scene = new WorkspaceScene(workspace);
        if (scene.Capture().Length != 0) throw new Exception("Character colliders must stay hidden.");
        root.Position = new(4, 3, 0);
        if (scene.Capture().Length != 0) throw new Exception("Moving a peer must not expose its collider.");
    }
}
