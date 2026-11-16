using System.Reflection;
using Woorf.Engine;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

static class DirtyReplicationTests
{
    internal static int Run()
    {
        try
        {
            var game = new Woorf.Engine.DataModel.DataModel();
            var workspace = game.Workspace;
            var marker = Instance.Create("Part"); marker.Parent = workspace;
            for (var index = 0; index < 100; index++) Instance.Create("Folder").Parent = workspace;
            var type = typeof(LuauRuntime).Assembly.GetType("Woorf.Engine.Networking.WireGraph")!;
            var graph = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [game, true], null)!;
            using var lifetime = (IDisposable)graph;
            var capture = type.GetMethod("Capture", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var counter = type.GetProperty("CapturedNodes", BindingFlags.Instance | BindingFlags.NonPublic);
            if (counter is null) throw new Exception("Replication does not expose node capture measurements.");
            long Count() => (long)counter.GetValue(graph)!;
            object Snapshot() => capture.Invoke(graph, null)!;
            Snapshot();
            var initial = Count();
            for (var index = 0; index < 20; index++) Snapshot();
            var scans = type.GetProperty("FullScans", BindingFlags.Instance | BindingFlags.NonPublic)!;
            if ((long)scans.GetValue(graph)! != 1) throw new Exception("Idle replication performs repeated hierarchy scans.");
            if (Count() != initial) throw new Exception("Idle replication still captures the shared world.");
            marker.Name = "one"; marker.Name = "two"; marker.Name = "final";
            Snapshot();
            if (Count() - initial != 1) throw new Exception("Repeated property changes were not coalesced to one instance capture.");
            CheckSpatialCaptures(workspace, (BasePart)marker, Snapshot, Count);
            var hidden = Instance.Create("Folder"); hidden.Parent = game.GetService("ServerStorage");
            var nested = Instance.Create("Part"); nested.Name = "Nested"; nested.Parent = hidden;
            hidden.Parent = workspace;
            var visible = (Array)Snapshot();
            if (!visible.Cast<object>().Any(node => (string)node.GetType().GetProperty("Name")!.GetValue(node)! == "Nested"))
                throw new Exception("Entering shared ancestry omitted descendants.");
            hidden.Parent = game.GetService("ServerStorage");
            var privateSnapshot = (Array)Snapshot();
            if (privateSnapshot.Cast<object>().Any(node => (string)node.GetType().GetProperty("Name")!.GetValue(node)! == "Nested"))
                throw new Exception("Leaving shared ancestry retained private descendants.");
            hidden.Parent = workspace;
            Snapshot();
            hidden.Destroy();
            var destroyed = (Array)Snapshot();
            if (destroyed.Cast<object>().Any(node => (string)node.GetType().GetProperty("Name")!.GetValue(node)! == "Nested"))
                throw new Exception("Destroyed descendants remained replicated.");
            var linked = (Model)Instance.Create("Model");
            linked.Name = "ExternalPrimary"; linked.Parent = workspace;
            linked.PrimaryPart = (BasePart)marker;
            Snapshot();
            marker.Parent = game.GetService("ServerStorage");
            var privateReference = ((Array)Snapshot()).Cast<object>()
                .Single(node => (string)node.GetType().GetProperty("Name")!.GetValue(node)! == "ExternalPrimary");
            if ((int)privateReference.GetType().GetProperty("PrimaryPart")!.GetValue(privateReference)! != 0)
                throw new Exception("An external PrimaryPart reference still exposes a private instance.");
            marker.Parent = workspace;
            var restoredReference = ((Array)Snapshot()).Cast<object>()
                .Single(node => (string)node.GetType().GetProperty("Name")!.GetValue(node)! == "ExternalPrimary");
            if ((int)restoredReference.GetType().GetProperty("PrimaryPart")!.GetValue(restoredReference)! == 0)
                throw new Exception("An external PrimaryPart reference was not restored on shared entry.");
            Console.WriteLine("PASS captures only dirty instances while tracking shared subtree visibility");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL captures only dirty instances while tracking shared subtree visibility: " + error.Message);
            return 1;
        }
    }
    private static void CheckSpatialCaptures(Instance workspace, BasePart marker, Func<object> snapshot, Func<long> count)
    {
        var before = count();
        marker.Position = new Vector3(1, 0, 0);
        marker.Position = new Vector3(2, 0, 0);
        marker.Position = new Vector3(3, 0, 0);
        snapshot();
        if (count() - before != 2) throw new Exception("Spatial edits did not coalesce the part and automatic model pivot.");
        ((Model)workspace).WorldPivot = CFrame.Identity;
        snapshot();
        before = count();
        marker.Position = new Vector3(4, 0, 0);
        snapshot();
        if (count() - before != 1) throw new Exception("A fixed model pivot was recaptured for an unrelated part edit.");
    }

}
