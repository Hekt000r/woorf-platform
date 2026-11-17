using Woorf.Engine;
using Woorf.Engine.DataModel;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Scripting;

static class ObbyTests
{
    internal static int Run()
    {
        try
        {
            CheckMultiplayerSpawn();
            if (LuauRuntime.Execute(ReadCourse() + Checks, "obby-tests.luau") != 0) throw new Exception("Course acceptance failed.");
            Console.WriteLine("PASS obby spawn hazards spinning checkpoints respawn and finish");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL obby: " + error.Message); return 1; }
    }

    private static void CheckMultiplayerSpawn()
    {
        var game = new DataModel();
        var player = new Player(4) { Parent = game.GetService("Players") };
        var clock = new MonotonicClock();
        using var session = new LuauSession(game, clock);
        var source = ReadCourse() + """
            local course = Course.build()
            local player = game:GetService("Players"):GetPlayers()[1]
            course:addPlayer(player)
            player:LoadCharacter()
            local root = player.Character.PrimaryPart
            assert(math.abs(root.Position.X) < 7, "Every multiplayer guest must spawn safely inside the start pad")
            """;
        if (session.Execute(source, "obby-multiplayer-spawn.luau") != 0 || session.HasFailed)
            throw new Exception("Multiplayer spawn acceptance failed.");
    }

    private static string ReadCourse()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "examples/obby/src/ServerScriptService/Course.luau");
        var module = File.Exists(path) ? File.ReadAllText(path) : "return nil";
        return "local Course = (function()\n" + module + "\nend)()\n";
    }

    private const string Checks = """
                assert(Course, "Obby course is missing")
                local course = Course.build()
                local players = game:GetService("Players")
                local player = Instance.new("Player", players)
                course:addPlayer(player)
                player:LoadCharacter()
                local root = player.Character.PrimaryPart
                local humanoid = player.Character.Humanoid
                root.Position = Vector3.new(0, 3, 8)
                course:update(0)
                assert(humanoid.Health == 100, "Spawn must be safe")
                local spinner = workspace.Obby.Spinner1
                local before = spinner.CFrame
                course:update(0.5)
                assert((spinner.CFrame.RightVector - before.RightVector).Magnitude > 0.1, "Spinner must rotate")
                task.wait(0.25)
                course:update(1 / 60)
                humanoid.Jump = true
                task.wait(1 / 60)
                assert(root.AssemblyLinearVelocity.Y > 25, "Player must jump while course spinners animate")
                root.Position = workspace.Obby.KillStrip1.Position + Vector3.new(0, 4, 0)
                course:update(0)
                assert(humanoid.Health == 100, "Jumping clear of a strip must be safe")
                root.Position = workspace.Obby.KillStrip1.Position + Vector3.new(0, 5, 0)
                root.AssemblyLinearVelocity = Vector3.zero
                task.wait(0.4)
                assert(humanoid.Health == 0, "Kill strip must kill from engine Touched without course overlap checks")
                player:LoadCharacter()
                root = player.Character.PrimaryPart
                root.Position = spinner.Position + spinner.CFrame.RightVector * 4 + Vector3.new(0, 5, 0)
                root.AssemblyLinearVelocity = Vector3.zero
                task.wait(0.45)
                assert(player.Character.Humanoid.Health == 0, "Rotated spinner must kill")
                player:LoadCharacter()
                root = player.Character.PrimaryPart
                root.Position = workspace.Obby.Checkpoint.Position + Vector3.new(0, 3, 0)
                root.AssemblyLinearVelocity = Vector3.zero
                task.wait(0.2)
                assert(player:GetAttribute("ObbyStage") == 2, "Checkpoint must save progress")
                player:LoadCharacter()
                assert(player.Character.PrimaryPart.Position.Z == -52, "Respawn must use checkpoint")
                root = player.Character.PrimaryPart
                root.Position = workspace.Obby.Finish.Position + Vector3.new(0, 3, 0)
                root.AssemblyLinearVelocity = Vector3.zero
                task.wait(0.2)
                assert(player:GetAttribute("ObbyFinished"), "Finish must record completion")
                root.Position = Vector3.new(0, -15, 0)
                course:update(0)
                assert(player.Character.Humanoid.Health == 0, "Falling must kill promptly")
                player:Destroy()
                course:update(0.1)
                """;
}
