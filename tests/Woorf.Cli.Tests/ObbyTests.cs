using Woorf.Engine;

static class ObbyTests
{
    internal static int Run()
    {
        try
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "examples/obby/src/ServerScriptService/Course.luau");
            var module = File.Exists(path) ? File.ReadAllText(path) : "return nil";
            var source = "local Course = (function()\n" + module + "\nend)()\n" + """
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
                root.Position = workspace.Obby.KillStrip1.Position
                course:update(0)
                assert(humanoid.Health == 0, "Kill strip must kill")
                player:LoadCharacter()
                root = player.Character.PrimaryPart
                root.Position = spinner.Position
                course:update(0)
                assert(player.Character.Humanoid.Health == 0, "Rotated spinner must kill")
                player:LoadCharacter()
                root = player.Character.PrimaryPart
                root.Position = workspace.Obby.Checkpoint.Position + Vector3.new(0, 2.5, 0)
                task.wait(0.15)
                course:update(0)
                assert(player:GetAttribute("ObbyStage") == 2, "Checkpoint must save progress")
                player:LoadCharacter()
                assert(player.Character.PrimaryPart.Position.Z == -52, "Respawn must use checkpoint")
                root = player.Character.PrimaryPart
                root.Position = workspace.Obby.Finish.Position + Vector3.new(0, 2.5, 0)
                course:update(0)
                assert(player:GetAttribute("ObbyFinished"), "Finish must record completion")
                root.Position = Vector3.new(0, -15, 0)
                course:update(0)
                assert(player.Character.Humanoid.Health == 0, "Falling must kill promptly")
                player:Destroy()
                course:update(0.1)
                """;
            if (LuauRuntime.Execute(source, "obby-tests.luau") != 0) throw new Exception("Course acceptance failed.");
            Console.WriteLine("PASS obby spawn hazards spinning checkpoints respawn and finish");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL obby: " + error.Message); return 1; }
    }
}
