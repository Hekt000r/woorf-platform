using Woorf.Engine.DataModel;
using Woorf.Engine.Physics;

static class PhysicsIsolationTests
{
    internal static int Run()
    {
        try
        {
            var game = new DataModel(); var workspace = (Workspace)game.Workspace;
            workspace.Gravity = 60;
            var local = (Part)Instance.Create("Part"); local.Size = new(2, 2, 2); local.Position = new(0, 10, 0); local.Parent = workspace;
            var distant = (Part)Instance.Create("Part"); distant.Size = new(2, 2, 2);
            distant.Position = new(1000, -1000, 0); distant.AssemblyLinearVelocity = new(0, -6000, 0); distant.Parent = workspace;
            using var world = new PhysicsWorld(workspace, 0);
            world.Advance(1d / 120);
            if (MathF.Abs(local.Position.Y - 9.995833f) > 0.00001f)
                throw new Exception("A distant falling body changed local fixed-step integration by imposing its collision subdivisions.");
            if (distant.Position.Y > -1049) throw new Exception("Isolated bodies must still simulate gravity and velocity.");
            CheckGravityReversal();
            Console.WriteLine("PASS distant fast bodies do not impose collision subdivisions on unrelated physics");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL physics isolation: " + error.Message); return 1; }
    }

    private static void CheckGravityReversal()
    {
        var body = new RigidBody(1) { Size = new(2, 2, 2), Frame = Woorf.Engine.Datatypes.CFrame.Identity,
            LinearVelocity = new(0, 500, 0), Material = new(1, 0, 0), CanCollide = true };
        var ceiling = new RigidBody(2) { Size = new(10, 0.1f, 10), Frame = new(new(0, 1.35f, 0)),
            Material = new(1, 0, 0), CanCollide = true, IsAnchored = true };
        PhysicsSolver.Step([body, ceiling], 120000, 1f / 120);
        if (body.LinearVelocity.Y >= -600)
            throw new Exception("Swept collision candidates missed a ceiling reached before gravity reverses the motion.");
    }
}
