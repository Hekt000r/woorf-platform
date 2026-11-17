using System.Reflection;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Events;
using Woorf.Engine.Physics;
using Woorf.Engine.Characters;

static class CharacterLifecycleTests
{
    internal static int Run()
    {
        var failures = 0;
        foreach (var (name, check) in new (string, Action)[]
        {
            ("characters spawn with initialized hierarchy and lifecycle signals", CheckSpawn),
            ("characters die and respawn after their own delay", CheckRespawn),
            ("characters clean up on leave reload and world disposal", CheckCleanup),
            ("Humanoid movement input validates values and normalizes horizontal intent", CheckMovement),
            ("character auto loading is opt-in and respects respawn settings", CheckAutoLoading),
            ("character respawn deadlines reset after health is restored", CheckRestoredHealth),
            ("characters recover when their collider or Humanoid leaves the model", CheckRemovedComponents),
            ("fall-death callbacks can remove a player without respawning it", CheckFallDeathLeave),
        })
        {
            try { check(); Console.WriteLine("PASS " + name); }
            catch (Exception error) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + error.GetBaseException().Message); }
        }
        return failures;
    }

    private static void CheckSpawn()
    {
        var game = new DataModel();
        using var physics = new PhysicsWorld((Workspace)game.Workspace, 0);
        using var world = CreateWorld(game, physics);
        var player = new Player(1); var added = 0;
        ReadSignal(player, "CharacterAdded").Connect(args =>
        {
            var character = (Model)args[0]!;
            Require(character.Parent == game.Workspace && character.PrimaryPart?.Name == "HumanoidRootPart", "CharacterAdded observed incomplete root state.");
            Require(character.FindFirstChild("Humanoid") is Humanoid, "CharacterAdded observed missing Humanoid.");
            added++;
        });
        player.Parent = game.GetService("Players");
        Synchronize(world, 0);
        var model = ReadCharacter(player)!;
        Require(model.Parent == game.Workspace && added == 1, "Joining player did not spawn exactly one character.");
        Require(model.PrimaryPart!.Size == new Vector3(2, 4, 2), "Character needs a single 2x4x2 collider.");
        Synchronize(world, 1);
        Require(ReadCharacter(player) == model && added == 1, "Living character was recreated during ordinary synchronization.");
    }

    private static void CheckRespawn()
    {
        var game = new DataModel();
        using var physics = new PhysicsWorld((Workspace)game.Workspace, 0);
        using var world = CreateWorld(game, physics);
        var player = new Player(1) { Parent = game.GetService("Players") };
        Synchronize(world, 0);
        var first = ReadCharacter(player)!;
        var humanoid = (Humanoid)first.FindFirstChild("Humanoid")!;
        humanoid.TakeDamage(100);
        Synchronize(world, 1);
        Synchronize(world, 3.99);
        Require(ReadCharacter(player) == first, "Dead character respawned before its three-second delay.");
        Synchronize(world, 4);
        var second = ReadCharacter(player)!;
        Require(second != first && first.IsDestroyed && ((Humanoid)second.FindFirstChild("Humanoid")!).Health == 100,
            "Respawn did not replace the dead character with a healthy hierarchy.");
        second.PrimaryPart!.Position = new(0, -101, 0);
        Synchronize(world, 5);
        Require(((Humanoid)second.FindFirstChild("Humanoid")!).Health == 0, "Falling below the character boundary must cause death.");
    }

    private static void CheckCleanup()
    {
        var game = new DataModel();
        using var physics = new PhysicsWorld((Workspace)game.Workspace, 0);
        var world = CreateWorld(game, physics);
        var player = new Player(1) { Parent = game.GetService("Players") };
        Synchronize(world, 0);
        var first = ReadCharacter(player)!; var removing = 0;
        ReadSignal(player, "CharacterRemoving").Connect(args => { Require(args[0] == ReadCharacter(player), "Removal signal lost the outgoing reference."); removing++; });
        world.LoadCharacter(player);
        Require(first.IsDestroyed && removing == 1 && ReadCharacter(player) != first, "Explicit reload leaked the old character.");
        var second = ReadCharacter(player)!;
        player.Parent = null;
        Synchronize(world, 1);
        Require(second.IsDestroyed && ReadCharacter(player) is null && removing == 2, "Leaving player retained its character.");
        player.Parent = game.GetService("Players"); Synchronize(world, 2);
        var third = ReadCharacter(player)!;
        world.Dispose();
        Require(third.IsDestroyed && ReadCharacter(player) is null, "World disposal retained a player character.");
    }

    private static void CheckMovement()
    {
        var humanoid = new Humanoid(); var type = typeof(Humanoid);
        var move = type.GetMethod("Move") ?? throw new Exception("Humanoid movement is missing.");
        move.Invoke(humanoid, [new Vector3(3, 2, 4)]);
        var direction = (Vector3)type.GetProperty("MoveDirection")!.GetValue(humanoid)!;
        Require(direction == new Vector3(0.6f, 0, 0.8f), "Movement input must normalize horizontal intent to unit length.");
        move.Invoke(humanoid, [new Vector3(float.MaxValue, 0, float.MaxValue)]);
        direction = (Vector3)type.GetProperty("MoveDirection")!.GetValue(humanoid)!;
        Require(MathF.Abs(direction.X - 0.70710677f) < 0.00001f && direction.Y == 0,
            "Finite large movement input must normalize without overflowing its magnitude.");
        foreach (var name in new[] { "WalkSpeed", "JumpPower" })
        {
            var property = type.GetProperty(name) ?? throw new Exception("Humanoid tuning is missing.");
            var original = property.GetValue(humanoid);
            foreach (var value in new[] { -1d, double.NaN, double.PositiveInfinity })
            {
                try { property.SetValue(humanoid, value); throw new Exception("Invalid " + name + " was accepted."); }
                catch (TargetInvocationException error) when (error.InnerException is ArgumentException) { }
            }
            Require(Equals(original, property.GetValue(humanoid)), "Invalid movement tuning changed state.");
        }
    }

    private static void CheckAutoLoading()
    {
        var game = new DataModel();
        using var physics = new PhysicsWorld((Workspace)game.Workspace, 0);
        using var world = CreateWorld(game, physics, false);
        var player = new Player(1) { Parent = game.GetService("Players") };
        Synchronize(world, 0);
        Require(ReadCharacter(player) is null, "Existing headless workloads must not receive unsolicited characters.");
        world.LoadCharacter(player);
        Require(ReadCharacter(player) is not null, "Manual loading must work with automatic loading disabled.");
        var property = typeof(Players).GetProperty("RespawnTime") ?? throw new Exception("RespawnTime is missing.");
        var players = (Players)game.GetService("Players");
        foreach (var value in new[] { -1d, double.NaN, double.PositiveInfinity })
        {
            try { property.SetValue(players, value); throw new Exception("Invalid RespawnTime accepted."); }
            catch (TargetInvocationException error) when (error.InnerException is ArgumentException) { }
        }
        Require((double)property.GetValue(players)! == 3, "Rejected respawn settings changed the delay.");
    }

    private static void CheckRestoredHealth()
    {
        var game = new DataModel();
        using var physics = new PhysicsWorld((Workspace)game.Workspace, 0);
        using var world = CreateWorld(game, physics);
        var player = new Player(1) { Parent = game.GetService("Players") };
        Synchronize(world, 0);
        var first = ReadCharacter(player)!;
        var humanoid = (Humanoid)first.FindFirstChild("Humanoid")!;
        humanoid.Health = 0; Synchronize(world, 1);
        humanoid.Health = 100; Synchronize(world, 2);
        humanoid.Health = 0; Synchronize(world, 10);
        Require(ReadCharacter(player) == first, "A stale death deadline caused an immediate respawn after a later death.");
        Synchronize(world, 13);
        Require(ReadCharacter(player) != first, "The later death did not get its own respawn delay.");
    }

    private static void CheckRemovedComponents()
    {
        var game = new DataModel();
        using var physics = new PhysicsWorld((Workspace)game.Workspace, 0);
        using var world = CreateWorld(game, physics);
        var player = new Player(1) { Parent = game.GetService("Players") };
        Synchronize(world, 0);
        var first = ReadCharacter(player)!;
        first.FindFirstChild("Humanoid")!.Parent = null;
        Synchronize(world, 1); Synchronize(world, 4);
        var second = ReadCharacter(player)!;
        Require(second != first && first.IsDestroyed, "Removing Humanoid left a character permanently without movement/health.");
        second.PrimaryPart!.Parent = null;
        Synchronize(world, 5); Synchronize(world, 8);
        Require(ReadCharacter(player) != second && second.IsDestroyed, "Removing the collider left an unusable character indefinitely.");
    }

    private static void CheckFallDeathLeave()
    {
        var game = new DataModel();
        using var physics = new PhysicsWorld((Workspace)game.Workspace, 0);
        using var world = CreateWorld(game, physics);
        ((Players)game.GetService("Players")).RespawnTime = 0;
        var player = new Player(1) { Parent = game.GetService("Players") };
        Synchronize(world, 0);
        var character = player.Character!;
        var humanoid = (Humanoid)character.FindFirstChild("Humanoid")!;
        humanoid.Died.Connect(_ => player.Destroy());
        character.PrimaryPart!.Position = new(0, -101, 0);
        Synchronize(world, 1);
        Require(player.IsDestroyed && player.Character is null && character.IsDestroyed,
            "Removing a player from a death callback must clean its character without recreating it.");
    }

    private static CharacterWorld CreateWorld(DataModel game, PhysicsWorld physics, bool shouldAutoLoad = true)
    {
        var players = (Players)game.GetService("Players");
        players.CharacterAutoLoads = shouldAutoLoad;
        return new(game, physics, 0);
    }
    private static void Synchronize(CharacterWorld world, double now) => world.Synchronize(now);
    private static Model? ReadCharacter(Player player) => player.Character;
    private static ScriptSignal ReadSignal(Player player, string name) => name == "CharacterAdded" ? player.CharacterAdded : player.CharacterRemoving;
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
