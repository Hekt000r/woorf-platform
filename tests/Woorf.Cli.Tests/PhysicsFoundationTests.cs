using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Physics;
using Woorf.Engine.Networking;

static class PhysicsFoundationTests
{
    internal static int Run()
    {
        var failures = 0;
        foreach (var (name, check) in new (string, Action)[]
        {
            ("sphere mass and inertia use spherical geometry", CheckSphereMass),
            ("fixed-step hooks control every substep and publish once", CheckFixedSteps),
            ("locked rotation withstands off-center impacts", CheckLockedRotation),
            ("support contacts distinguish ground walls and ceilings", CheckContacts),
            ("contact state clears on separation removal and collision changes", CheckContactCleanup),
            ("fast boxes and spheres cannot cross thin walls at qualified speeds", CheckFastCollision),
            ("upright collider jumps clears ground and lands without ceiling penetration", CheckJump),
            ("upright collider remains blocked in a corner under sustained drive", CheckCorner),
            ("oriented support normals distinguish a slope from a floor", CheckSlope),
            ("callback teleports invalidate support before another physics step", CheckCallbackSupport),
            ("sphere shape survives local and wire late-join snapshots", CheckShapeSnapshots),
            ("fixed-step removal does not leave a ghost collider", CheckHookRemoval),
        })
        {
            try { check(); Console.WriteLine("PASS " + name); }
            catch (Exception error) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + error.Message); }
        }
        return failures;
    }
    private static void CheckSphereMass()
    {
        var sphere = new RigidBody(1) { Shape = PartType.Ball, Size = new(2,2,2), Material = new(3,0,0), Frame = CFrame.Identity };
        Require(MathF.Abs(sphere.InverseMass - 0.07957747f) < 0.000001f, "Expected mass 4*pi for unit sphere of density 3.");
        Require(MathF.Abs(sphere.InverseInertia(new(1,0,0)).X - 0.19894368f) < 0.000001f, "Expected solid sphere inertia 2*m*r*r/5.");
    }
    private static void CheckFixedSteps()
    {
        var workspace = CreateWorkspace(); workspace.Gravity = 0;
        var part = AddPart(workspace, new(0,4,0), new(2,2,2));
        using var world = new PhysicsWorld(workspace, 0);
        var steps = 0; var publications = 0;
        part.GetPropertyChangedSignal("CFrame").Connect(_ => publications++);
        world.BeforeStep += delta =>
        {
            Require(MathF.Abs(delta - 1f/120) < 1e-8f, "Hook received host-frame time.");
            steps++;
            world.GetBody(part)!.LinearVelocity = new(12,0,0);
        };
        world.Advance(0.1);
        Require(steps == 12 && publications == 1, "Hook or publication count: " + steps + "/" + publications);
        Require(MathF.Abs(part.Position.X - 1.2f) < 0.001f, "Controller did not affect every substep.");
        world.Advance(0.1);
        Require(steps == 12, "Zero elapsed time ran another substep.");
    }
    private static void CheckLockedRotation()
    {
        var workspace = CreateWorkspace(); workspace.Gravity = 0;
        AddPart(workspace, new(3,1,0), new(1,1,10), true);
        var part = AddPart(workspace, new(0,0,0), new(2,4,2));
        using var world = new PhysicsWorld(workspace, 0);
        world.SetRotationLocked(part, true);
        world.BeforeStep += _ => world.GetBody(part)!.LinearVelocity = new(8,0,2);
        for (var index = 1; index <= 120; index++) world.Advance(index / 120d);
        Require(part.CFrame.UpVector.Y > 0.9999f && part.AssemblyAngularVelocity.Magnitude < 0.0001f, "Character collider tipped.");
        Require(part.Position.X < 1.55f && part.Position.Z > 1, "Sustained wall contact failed to slide.");
    }
    private static void CheckContacts()
    {
        foreach (var (position, size, normal) in new[]
        {
            (new Vector3(0,-1,0), new Vector3(20,2,20), new Vector3(0,1,0)),
            (new Vector3(2,1,0), new Vector3(2,20,20), new Vector3(-1,0,0)),
            (new Vector3(0,3,0), new Vector3(20,2,20), new Vector3(0,-1,0)),
        })
        {
            var workspace = CreateWorkspace(); workspace.Gravity = 0;
            var surface = AddPart(workspace, position, size, true);
            var part = AddPart(workspace, new(0,1,0), new(2,2,2));
            using var world = new PhysicsWorld(workspace, 0);
            world.Advance(1d/120);
            var contacts = world.GetContacts(part);
            Require(contacts.Any(contact => contact.Other == surface && contact.Normal.Dot(normal) > 0.99f), "Incorrect support normal.");
        }
    }
    private static void CheckContactCleanup()
    {
        var workspace = CreateWorkspace(); workspace.Gravity = 0;
        var floor = AddPart(workspace, new(0,-1,0), new(20,2,20), true);
        var part = AddPart(workspace, new(0,1,0), new(2,2,2));
        using var world = new PhysicsWorld(workspace, 0);
        world.Advance(1d/120);
        Require(world.GetContacts(part).Count > 0, "Missing initial ground contact.");
        part.Position = new(0,5,0); world.Advance(2d/120);
        Require(world.GetContacts(part).Count == 0, "Stale ground after teleport.");
        part.Position = new(0,1,0); world.Advance(3d/120);
        floor.CanCollide = false; world.Advance(4d/120);
        Require(world.GetContacts(part).Count == 0, "Noncollidable ground remained support.");
        floor.CanCollide = true; world.Advance(5d/120);
        floor.Destroy(); world.Advance(6d/120);
        Require(world.GetContacts(part).Count == 0 && world.GetBody(floor) is null, "Removed support retained.");
    }
    private static void CheckFastCollision()
    {
        foreach (var shape in new[] { PartType.Block, PartType.Ball })
        {
            var workspace = CreateWorkspace(); workspace.Gravity = 0;
            AddPart(workspace, new(0,0,0), new(0.05f,20,20), true);
            var part = AddPart(workspace, new(-4,0,0), new(2,2,2)); part.Shape = shape;
            part.AssemblyLinearVelocity = new(1200,0,0);
            using var world = new PhysicsWorld(workspace, 0);
            world.Advance(1d/120);
            Require(part.Position.X < -0.99f && MathF.Abs(part.AssemblyLinearVelocity.X) < 0.1f, shape + " tunnelled through thin wall.");
        }
    }
    private static void CheckJump()
    {
        var workspace = CreateWorkspace(); workspace.Gravity = 50;
        AddPart(workspace, new(0,-1,0), new(40,2,40), true);
        AddPart(workspace, new(0,7,0), new(40,2,40), true);
        var part = AddPart(workspace, new(0,2,0), new(2,4,2));
        using var world = new PhysicsWorld(workspace, 0);
        world.SetRotationLocked(part, true);
        world.Advance(1d/120);
        Require(world.GetContacts(part).Any(contact => contact.Normal.Y > 0.99f), "Collider not grounded before jump.");
        var steps = 0; var leftGround = false; var hitCeiling = false;
        world.BeforeStep += _ =>
        {
            if (steps++ == 0) world.GetBody(part)!.LinearVelocity = new(0,30,0);
        };
        for (var index = 2; index <= 240; index++)
        {
            world.Advance(index / 120d);
            var contacts = world.GetContacts(part);
            if (part.Position.Y > 2.1f && !contacts.Any(contact => contact.Normal.Y > 0.7f)) leftGround = true;
            if (contacts.Any(contact => contact.Normal.Y < -0.9f)) hitCeiling = true;
            Require(part.Position.Y <= 4.03f && part.Position.Y >= 1.97f, "Collider crossed floor or ceiling.");
        }
        Require(leftGround && hitCeiling, "Jump did not leave ground or reach ceiling.");
        Require(MathF.Abs(part.Position.Y - 2) < 0.03f && MathF.Abs(part.AssemblyLinearVelocity.Y) < 0.05f, "Collider did not land stably.");
        Require(world.GetContacts(part).Any(contact => contact.Normal.Y > 0.99f), "Landing did not restore support.");
    }
    private static void CheckCorner()
    {
        var workspace = CreateWorkspace(); workspace.Gravity = 50;
        AddPart(workspace, new(0,-1,0), new(40,2,40), true);
        AddPart(workspace, new(5,5,0), new(1,12,40), true);
        AddPart(workspace, new(0,5,5), new(40,12,1), true);
        var part = AddPart(workspace, new(0,2,0), new(2,4,2));
        using var world = new PhysicsWorld(workspace, 0); world.SetRotationLocked(part, true);
        world.BeforeStep += _ =>
        {
            var body = world.GetBody(part)!; body.LinearVelocity = new(16,body.LinearVelocity.Y,16);
        };
        for (var index = 1; index <= 240; index++) world.Advance(index / 120d);
        Require(part.Position.X is > 3.4f and < 3.55f && part.Position.Z is > 3.4f and < 3.55f, "Collider crossed or escaped corner.");
        Require(MathF.Abs(part.Position.Y - 2) < 0.03f && part.CFrame.UpVector.Y > 0.9999f, "Corner drive lost upright ground support.");
    }
    private static void CheckSlope()
    {
        var workspace = CreateWorkspace(); workspace.Gravity = 50;
        var ramp = AddPart(workspace, default, new(40,1,20), true);
        ramp.CFrame = CFrame.FromAxisAngle(new(0,0,1), MathF.PI / 6);
        var part = AddPart(workspace, new(0,3,0), new(2,4,2));
        using var world = new PhysicsWorld(workspace, 0); world.SetRotationLocked(part, true);
        var foundSlope = false;
        for (var index = 1; index <= 60; index++)
        {
            world.Advance(index / 120d);
            foundSlope |= world.GetContacts(part).Any(contact => contact.Normal.Y is > 0.85f and < 0.88f && contact.Normal.X < -0.49f);
        }
        Require(foundSlope && part.CFrame.UpVector.Y > 0.9999f, "Slope normal missing or collider tipped.");
    }
    private static void CheckCallbackSupport()
    {
        var workspace = CreateWorkspace(); workspace.Gravity = 50;
        AddPart(workspace, new(0,-1,0), new(40,2,40), true);
        var falling = AddPart(workspace, new(-5,10,0), new(2,2,2));
        var grounded = AddPart(workspace, new(5,1,0), new(2,2,2));
        using var world = new PhysicsWorld(workspace, 0);
        falling.GetPropertyChangedSignal("CFrame").Connect(_ => grounded.Position = new(5,20,0));
        world.Advance(1d/120);
        Require(grounded.Position.Y == 20, "Physics overwrote callback teleport.");
        Require(world.GetContacts(grounded).Count == 0, "Teleport left stale support visible.");
    }
    private static void CheckShapeSnapshots()
    {
        var server = new DataModel(); var client = new DataModel();
        var part = AddPart((Workspace)server.Workspace, default, new(2,2,2), true);
        var replicas = new ReplicaMap(server, client); replicas.Synchronize();
        part.Shape = PartType.Ball; replicas.Synchronize();
        Require(((Part)replicas.ToClient(part)!).Shape == PartType.Ball, "Local shape update missing.");
        var late = new ReplicaMap(server, new DataModel()); late.Synchronize();
        Require(((Part)late.ToClient(part)!).Shape == PartType.Ball, "Local late join lost shape.");
        using var authority = new WireGraph(server, true);
        var snapshot = authority.Capture();
        using var receiver = new WireGraph(new DataModel(), false); receiver.Apply(snapshot);
        Require(((Part)receiver.Find(authority.Identify(part))!).Shape == PartType.Ball, "Wire late join lost shape.");
        part.Shape = PartType.Block; receiver.Apply(authority.Capture());
        Require(((Part)receiver.Find(authority.Identify(part))!).Shape == PartType.Block, "Wire shape update missing.");
    }
    private static void CheckHookRemoval()
    {
        var workspace = CreateWorkspace(); workspace.Gravity = 0;
        var wall = AddPart(workspace, new(0,0,0), new(1,20,20), true);
        var part = AddPart(workspace, new(-2,0,0), new(2,2,2));
        part.AssemblyLinearVelocity = new(240,0,0);
        using var world = new PhysicsWorld(workspace, 0);
        world.BeforeStep += _ => wall.Destroy();
        world.Advance(1d/120);
        Require(MathF.Abs(part.Position.X) < 0.001f, "Collider removed by controller still blocked motion.");
        Require(world.GetContacts(part).Count == 0, "Destroyed collider remained support.");
    }
    private static Workspace CreateWorkspace() => (Workspace)new DataModel().Workspace;
    private static Part AddPart(Workspace workspace, Vector3 position, Vector3 size, bool anchored = false)
    {
        var part = (Part)Instance.Create("Part"); part.Size = size; part.Position = position;
        part.Anchored = anchored; part.CustomPhysicalProperties = new(1,0,0); part.Parent = workspace;
        return part;
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
