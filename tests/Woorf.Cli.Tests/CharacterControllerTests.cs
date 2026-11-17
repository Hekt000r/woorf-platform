using Woorf.Engine.Characters;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Physics;

static class CharacterControllerTests
{
    internal static int Run()
    {
        var failures = 0;
        foreach (var (name, check) in new (string, Action)[]
        {
            ("character walks at capped speed and stops without input", CheckWalking),
            ("default jump stays below six studs in playground gravity", CheckDefaultJumpHeight),
            ("moving unrelated hazards preserves grounded jump input", CheckJumpWithMovingHazard),
            ("character slides against walls and remains upright", CheckWalls),
            ("character jumps hits ceiling and restores grounded state on landing", CheckJump),
            ("character rejects air jumps and does not buffer them until landing", CheckAirJump),
            ("character distinguishes a walkable ramp from steep support", CheckSlopes),
            ("dead character stops driving its collider", CheckDeath),
            ("disposing character controller removes its movement hook", CheckDisposal),
            ("characters stop at a wall without pushing each other through it", CheckWallPressure),
        })
        {
            try { check(); Console.WriteLine("PASS " + name); }
            catch (Exception error) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + error.Message); }
        }
        return failures;
    }

    private static void CheckWalking()
    {
        using var scene = new Scene();
        scene.Move(new(3, 0, 4)); scene.Advance(120);
        Require(scene.Collider.Position.X is > 8.9f and < 9.8f && scene.Collider.Position.Z is > 12 and < 13,
            "Diagonal input exceeded configured walking speed or did not move.");
        Require(scene.IsGrounded, "Walking lost floor support.");
        var stopped = scene.Collider.Position; scene.Move(default); scene.Advance(60);
        Require((scene.Collider.Position - stopped).Magnitude < 0.03f, "No-input character kept sliding.");
    }

    private static void CheckDefaultJumpHeight()
    {
        using var scene = new Scene();
        scene.Workspace.Gravity = 80;
        scene.Advance(2);
        var start = scene.Collider.Position.Y;
        scene.Jump();
        var peak = start;
        for (var index = 0; index < 150; index++)
        {
            scene.Advance(1);
            peak = MathF.Max(peak, scene.Collider.Position.Y);
        }
        Require(peak - start is > 5 and < 6, $"Expected a compact jump, got {peak - start} studs.");
        Require(scene.IsGrounded, "Jump did not land.");
    }

    private static void CheckJumpWithMovingHazard()
    {
        using var scene = new Scene();
        var hazard = scene.Add(new(30, 3, 0), new(13, 1, 1), true);
        hazard.CanCollide = false;
        scene.Advance(3);
        Require(scene.IsGrounded, "Character must start on the floor.");
        hazard.CFrame *= CFrame.FromAxisAngle(new(0, 1, 0), 0.1f);
        scene.Jump();
        scene.Advance(2);
        Require(scene.Collider.AssemblyLinearVelocity.Y > 25 && scene.Collider.Position.Y > 2.2f,
            "Moving an unrelated spinner discarded the grounded jump request.");
    }

    private static void CheckWalls()
    {
        using var scene = new Scene();
        scene.Add(new(5, 4, 0), new(1, 10, 80), true);
        scene.Move(new(1, 0, 1)); scene.Advance(240);
        Require(scene.Collider.Position.X is > 3.3f and < 3.6f && scene.Collider.Position.Z > 15,
            "Collider crossed the wall or failed to slide along it.");
        Require(scene.Collider.CFrame.UpVector.Y > 0.9999f && scene.Collider.AssemblyAngularVelocity.Magnitude < 0.0001f,
            "Character tipped under sustained off-center contact.");
    }

    private static void CheckJump()
    {
        using var scene = new Scene();
        scene.Add(new(0, 7, 0), new(80, 2, 80), true);
        scene.Advance(2); Require(scene.IsGrounded, "Character did not start grounded.");
        scene.Jump(); scene.Advance(1);
        Require(!scene.IsGrounded && scene.Collider.AssemblyLinearVelocity.Y > 20, "Grounded jump did not leave support.");
        var hitCeiling = false;
        for (var index = 0; index < 200; index++)
        {
            scene.Advance(1);
            hitCeiling |= scene.World.GetContacts(scene.Collider).Any(contact => contact.Normal.Y < -0.9f);
            Require(scene.Collider.Position.Y is >= 1.97f and <= 4.03f, "Character crossed its floor or ceiling.");
        }
        Require(hitCeiling && scene.IsGrounded && MathF.Abs(scene.Collider.Position.Y - 2) < 0.03f,
            "Character did not hit ceiling and land stably.");
    }

    private static void CheckAirJump()
    {
        using var scene = new Scene();
        scene.Advance(2); scene.Jump(); scene.Advance(20);
        var velocity = scene.Collider.AssemblyLinearVelocity.Y;
        scene.Jump(); scene.Advance(1);
        Require(scene.Collider.AssemblyLinearVelocity.Y < velocity, "Air jump reset vertical velocity.");
        scene.Advance(220);
        Require(scene.IsGrounded && MathF.Abs(scene.Collider.Position.Y - 2) < 0.03f,
            "Rejected air jump was replayed upon landing.");
    }

    private static void CheckSlopes()
    {
        foreach (var (angle, shouldGround) in new[] { (MathF.PI / 6, true), (MathF.PI / 3, false) })
        {
            using var scene = new Scene(false);
            var ramp = scene.Add(default, new(80, 1, 80), true);
            ramp.CFrame = CFrame.FromAxisAngle(new(0, 0, 1), angle);
            scene.Collider.Position = new(0, 3, 0);
            var foundGround = false;
            for (var index = 0; index < 90; index++) { scene.Advance(1); foundGround |= scene.IsGrounded; }
            Require(foundGround == shouldGround, "Slope grounding did not respect the walkable normal threshold.");
        }
    }

    private static void CheckDeath()
    {
        using var scene = new Scene();
        scene.Move(new(1, 0, 0)); scene.Advance(12); scene.Humanoid.Health = 0; scene.Advance(1);
        Require(MathF.Abs(scene.Collider.AssemblyLinearVelocity.X) < 0.01f, "Dead character kept applying move input.");
        scene.Jump(); scene.Advance(1);
        Require(scene.Collider.AssemblyLinearVelocity.Y < 1, "Dead character jumped.");
    }

    private static void CheckDisposal()
    {
        using var scene = new Scene();
        scene.Move(new(1, 0, 0)); scene.Advance(1); scene.Controller.Dispose();
        scene.Collider.AssemblyLinearVelocity = default; scene.Advance(12);
        Require(MathF.Abs(scene.Collider.Position.X - 16f / 120) < 0.01f, "Disposed controller still drove the collider.");
    }

    private static void CheckWallPressure()
    {
        using var scene = new Scene();
        scene.Collider.Position = new(4, 2, 0); scene.Humanoid.WalkSpeed = 8;
        scene.Add(new(10, 4, 0), new(1, 10, 80), true);
        var trailing = scene.Add(new(1, 2, 0), new(2, 4, 2));
        var humanoid = (Humanoid)Instance.Create("Humanoid"); humanoid.WalkSpeed = 8;
        using var controller = new CharacterController(scene.World, trailing, humanoid);
        controller.SetMoveDirection(new(1, 0, 0)); scene.Move(new(1, 0, 0));
        scene.Advance(360);
        Require(scene.Collider.Position.X <= 8.53f && trailing.Position.X <= 6.56f,
            $"Character pressure penetrated the wall: lead={scene.Collider.Position.X}, trailing={trailing.Position.X}.");
        Require(scene.Collider.Position.X >= 8.3f && trailing.Position.X >= 6.25f,
            "Characters stopped before reaching the wall and each other.");
    }

    private sealed class Scene : IDisposable
    {
        private int step;
        private readonly CharacterController controller;
        internal readonly Workspace Workspace = (Workspace)new DataModel().Workspace;
        internal readonly PhysicsWorld World;
        internal readonly Part Collider;
        internal readonly Humanoid Humanoid = (Humanoid)Instance.Create("Humanoid");
        internal IDisposable Controller => controller;
        internal bool IsGrounded => controller.IsGrounded;
        internal Scene(bool hasFloor = true)
        {
            Workspace.Gravity = 50;
            if (hasFloor) Add(new(0, -1, 0), new(200, 2, 200), true);
            Collider = Add(new(0, 2, 0), new(2, 4, 2));
            World = new PhysicsWorld(Workspace, 0);
            controller = new CharacterController(World, Collider, Humanoid);
        }
        internal Part Add(Vector3 position, Vector3 size, bool anchored = false)
        {
            var part = (Part)Instance.Create("Part"); part.Size = size; part.Position = position;
            part.Anchored = anchored; part.CustomPhysicalProperties = new(1, 0, 0); part.Parent = Workspace;
            return part;
        }
        internal void Move(Vector3 value) => controller.SetMoveDirection(value);
        internal void Jump() => controller.RequestJump();
        internal void Advance(int count) { for (var index = 0; index < count; index++) World.Advance(++step / 120d); }
        public void Dispose() { Controller.Dispose(); World.Dispose(); }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
