using Woorf.Engine.DataModel;
using Woorf.Engine.Networking;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;

internal static class ContextActionTests
{
    internal static int Run()
    {
        var game = new DataModel();
        var starter = Instance.CreateRuntime("StarterPlayerScripts");
        starter.Parent = game.GetService("StarterPlayer");
        var script = Instance.Create("LocalScript");
        script.Parent = starter;
        script.ScriptSource = """
            local cas = game:GetService("ContextActionService")
            local input = game:GetService("UserInputService")
            local calls = {}
            local ran = false
            assert(select("#", spawn(function() ran = true end)) == 0)
            assert(not ran)
            cas:BindAction("base", function(name, state, key)
                assert(name == "base" and key.KeyCode == Enum.KeyCode.E)
                table.insert(calls, "base:" .. state.Name)
            end, false, Enum.KeyCode.E)
            cas:BindActionAtPriority("first", function(name, state, key)
                table.insert(calls, "first:" .. state.Name)
                return Enum.ContextActionResult.Pass
            end, false, 3000, Enum.KeyCode.E)
            cas:BindAction("gamepad", function(_, state, key)
                if state == Enum.UserInputState.Cancel then
                    assert(key.UserInputType == Enum.UserInputType.None and key.KeyCode == Enum.KeyCode.Unknown)
                    print("PASS ContextActionService cancel")
                    return
                end
                assert(key.UserInputType == Enum.UserInputType.Gamepad1 and key.KeyCode == Enum.KeyCode.ButtonL1)
                table.insert(calls, "gamepad:" .. state.Name)
            end, false, Enum.KeyCode.ButtonL1)
            input.InputBegan:Connect(function(key, processed)
                if key.KeyCode == Enum.KeyCode.E then assert(processed) end
            end)
            game:GetService("RunService").Heartbeat:Connect(function()
                if not ran or #calls < 5 then return end
                assert(ran and table.concat(calls, ",") == "first:Begin,base:Begin,first:End,base:End,gamepad:Begin")
                cas:UnbindAction("base")
                cas:UnbindAllActions()
                print("PASS ContextActionService order, input state, and deferred spawn")
            end)
            """;
        var clock = new TestClock();
        using var host = new LocalNetworkHost(new LoadedProject(game, [new(script, script.ScriptSource, "context-action.luau")]), clock);
        if (host.Start() != 0) return 1;
        var player = host.ConnectClient();
        var client = host.GetClientGame(player);
        var input = (UserInputService)client.GetService("UserInputService");
        input.SetKey("E", true, false);
        input.SetKey("E", false, false);
        input.SetInput("Gamepad1", "ButtonL1", true, false);
        clock.Now = 0.1;
        if (host.Pump() != 0) return 1;
        clock.Now = 0.2;
        return host.Pump() == 0 && client.ScriptOutput.Count(line => line.StartsWith("PASS ContextActionService", StringComparison.Ordinal)) == 2 ? 0 : 1;
    }

    private sealed class TestClock : IRuntimeClock
    {
        internal double Now;
        public double ElapsedSeconds => Now;
        public void Sleep(TimeSpan duration) => Now += duration.TotalSeconds;
    }
}
