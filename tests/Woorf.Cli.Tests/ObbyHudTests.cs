using Woorf.Engine.DataModel;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Scripting;
using Woorf.Engine.Networking;
using Woorf.Engine.Projects;
using Woorf.Engine.Gui;

static class ObbyHudTests
{
    internal static int Run()
    {
        try
        {
            var course = File.ReadAllText("examples/obby/src/ServerScriptService/Course.luau");
            var game = new DataModel();
            using var session = new LuauSession(game, new MonotonicClock());
            var source = "local Course = (function()\n" + course + "\nend)()\n" + Checks;
            if (session.Execute(source, "obby-hud-status.luau") != 0 || session.HasFailed)
                throw new Exception("Obby HUD status acceptance failed.");
            CheckHud();
            Console.WriteLine("PASS obby HUD tracks elapsed time deaths and authoritative restart");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL obby HUD status: " + error.Message); return 1; }
    }

    private static void CheckHud()
    {
        var project = ProjectLoader.Load("examples/obby");
        using var host = new LocalPlayHost(project, new MonotonicClock());
        var screen = host.Player.FindFirstChild("PlayerGui")?.FindFirstChild("ObbyHud");
        if (screen?.ClassName != "ScreenGui" || screen.FindFirstChild("Panel")?.FindFirstChild("Restart") is not Instance restart)
            throw new Exception("Playable obby must create a scripted HUD with a restart button.");
        var authority = ((Players)project.Game.GetService("Players")).GetPlayers()[0];
        authority.SetAttribute("ObbyStage", 2d);
        authority.SetAttribute("ObbyFinished", true);
        authority.SetAttribute("ObbyElapsed", 9d);
        authority.SetAttribute("ObbyDeaths", 3d);
        host.Pump();
        var stats = (GuiInstance)screen.FindFirstChild("Panel")!.FindFirstChild("Stats")!;
        if (!stats.Get<string>("Text").Contains("00:09") || !stats.Get<string>("Text").Contains("Deaths 3"))
            throw new Exception("HUD must display the replicated time and deaths.");
        var scene = new GuiScene();
        scene.Capture(host.Player.FindFirstChild("PlayerGui"), 640, 480);
        scene.Pointer(100, 160, true);
        scene.Pointer(100, 160, false);
        host.Pump();
        host.Pump();
        if (!Equals(authority.GetAttribute("ObbyStage"), 1d) || authority.GetAttribute("ObbyFinished") is not false
            || !Equals(authority.GetAttribute("ObbyDeaths"), 0d))
            throw new Exception("HUD restart must change the authoritative player's course state through a remote.");
        if (host.HasFailed) throw new Exception("HUD LocalScript failed.");
    }

    private const string Checks = """
        local course = Course.build()
        local player = Instance.new("Player", game:GetService("Players"))
        course:addPlayer(player)
        player:LoadCharacter()
        assert(player:GetAttribute("ObbyDeaths") == 0, "HUD must begin with zero deaths")
        course:update(1.25)
        assert(math.abs(player:GetAttribute("ObbyElapsed") - 1.25) < 0.001, "HUD time must advance")
        player.Character.Humanoid.Health = 0
        assert(player:GetAttribute("ObbyDeaths") == 1, "Death must increment HUD count once")
        player.Character.Humanoid.Health = 0
        assert(player:GetAttribute("ObbyDeaths") == 1, "Repeated health writes must not count twice")
        player:SetAttribute("ObbyFinished", true)
        course:update(0.5)
        assert(math.abs(player:GetAttribute("ObbyElapsed") - 1.25) < 0.001, "Finish must freeze time")
        course:restartPlayer(player)
        assert(player:GetAttribute("ObbyStage") == 1 and player:GetAttribute("ObbyFinished") == false)
        assert(player:GetAttribute("ObbyDeaths") == 0 and player:GetAttribute("ObbyElapsed") == 0)
        assert(player.Character.Humanoid.Health == 100 and player.Character.PrimaryPart.Position.Z == 8)
        course:update(0.25)
        assert(player:GetAttribute("ObbyElapsed") == 0.25, "Restarted time must advance again")
        player:Destroy()
        course:update(0.25)
        """;
}
