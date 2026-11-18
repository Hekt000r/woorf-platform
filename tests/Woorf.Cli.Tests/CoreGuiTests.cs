using Woorf.Engine.DataModel;
using Woorf.Engine.Gui;
using Woorf.Engine.Networking;
using Woorf.Engine.Scheduling;

internal static class CoreGuiTests
{
    internal static int Run()
    {
        var core = new CoreGuiOverlay();
        var player = new Player(1, "Runner");
        player.Name = "Runner";
        var folder = Instance.Create("Folder");
        folder.Name = "leaderstats";
        folder.Parent = player;
        var score = (ValueObject)Instance.Create("IntValue");
        score.Name = "Wins";
        score.Value = 7d;
        score.Parent = folder;

        bool HasText(string value) => core.Capture([player], 800, 600)
            .Any(command => command.Content.Contains(value, StringComparison.Ordinal));

        if (!HasText("Wins") || !HasText("7")) return 1;
        core.KeyDown("Escape", false);
        if (!core.MenuIsOpen || !HasText("Resume")) return 1;
        core.KeyDown("Escape", false);
        if (core.MenuIsOpen) return 1;
        core.KeyDown("F3", false);
        if (HasText("Network")) return 1;
        core.KeyDown("F3", true);
        core.KeyDown("F4", true);
        core.KeyDown("F9", false);
        if (!HasText("Network") || !HasText("Frame") || !HasText("Developer Console")) return 1;
        CheckScriptContract();
        Console.WriteLine("PASS core GUI shortcuts, replicated leaderstats, and Luau controls");
        return 0;
    }

    private static void CheckScriptContract()
    {
        var game = new DataModel();
        var script = Instance.Create("LocalScript");
        var templates = Instance.Create("Folder");
        templates.Name = "StarterPlayerScripts";
        templates.Parent = game.GetService("StarterPlayer");
        script.Parent = templates;
        script.ScriptSource = """
            local starter = game:GetService("StarterGui")
            local gui = game:GetService("GuiService")
            assert(starter:GetCoreGuiEnabled(Enum.CoreGuiType.PlayerList))
            starter:SetCoreGuiEnabled(Enum.CoreGuiType.PlayerList, false)
            assert(not starter:GetCoreGuiEnabled(Enum.CoreGuiType.PlayerList))
            starter:SetCoreGuiEnabled(Enum.CoreGuiType.PlayerList, true)
            gui.MenuOpened:Connect(function()
                assert(gui.MenuIsOpen)
                print("core menu opened")
            end)
            """;
        using var host = new LocalNetworkHost(new(game, [new(script, script.ScriptSource, "core.luau")]), new MonotonicClock());
        if (host.Start() != 0) throw new Exception("Core GUI host failed to start.");
        var authority = host.ConnectClient();
        var folder = Instance.Create("Folder");
        folder.Name = "leaderstats";
        folder.Parent = authority;
        var stat = (ValueObject)Instance.Create("IntValue");
        stat.Name = "Wins";
        stat.Value = 41d;
        stat.Parent = folder;
        var client = host.GetClientGame(authority);
        if (host.Pump() != 0) throw new Exception("Core GUI replication failed.");
        var players = ((Players)client.GetService("Players")).GetPlayers();
        if (!client.CoreOverlay.Capture(players, 800, 600).Any(item => item.Content == "41"))
            throw new Exception("Replicated leaderboard score did not appear.");
        client.CoreOverlay.KeyDown("Escape", false);
        if (host.Pump() != 0 || !client.ScriptOutput.Contains("core menu opened"))
            throw new Exception("GuiService.MenuOpened did not reach the LocalScript.");
    }
}
