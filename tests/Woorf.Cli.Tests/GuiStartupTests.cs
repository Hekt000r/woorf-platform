using Woorf.Engine.DataModel;
using Woorf.Engine.Networking;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;

static class GuiStartupTests
{
    internal static int Run()
    {
        try
        {
            CheckRespawn();
            Console.WriteLine("PASS GUI respawn replaces reset templates and preserves persistent direct screens");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL GUI startup: " + error.Message); return 1; }
    }

    private static void CheckRespawn()
    {
        var game = new DataModel();
        var setup = Instance.Create("Script"); setup.Parent = game.GetService("ServerScriptService");
        setup.ScriptSource = """
            local starter = game:GetService("StarterGui")
            local reset = Instance.new("ScreenGui", starter); reset.Name = "Reset"
            local persistent = Instance.new("ScreenGui", starter)
            persistent.Name = "Persistent"; persistent.ResetOnSpawn = false
            local folder = Instance.new("Folder", starter); folder.Name = "Nested"
            local nested = Instance.new("ScreenGui", folder); nested.ResetOnSpawn = false
            """;
        using var host = new LocalNetworkHost(new(game, [new(setup, setup.ScriptSource, "setup.luau")]), new MonotonicClock());
        if (host.Start() != 0) throw new Exception("GUI template setup failed.");
        var authority = host.ConnectClient();
        var client = host.GetClientGame(authority);
        var gui = ((Players)client.GetService("Players")).LocalPlayer!.FindFirstChild("PlayerGui")!;
        var reset = gui.FindFirstChild("Reset")!;
        var persistent = gui.FindFirstChild("Persistent")!;
        var nested = gui.FindFirstChild("Nested")!;
        persistent.SetAttribute("LocalEdit", true);
        authority.Character = (Model)Instance.Create("Model"); authority.Character.Parent = game.Workspace;
        if (host.Pump() != 0) throw new Exception("Spawn synchronization failed.");
        if (!reset.IsDestroyed || ReferenceEquals(gui.FindFirstChild("Reset"), reset))
            throw new Exception("Character spawn must replace resettable GUI clones.");
        if (gui.FindFirstChild("Persistent") != persistent || persistent.GetAttribute("LocalEdit") is not true)
            throw new Exception("Direct ResetOnSpawn=false screens must retain local edits and identity.");
        if (!nested.IsDestroyed) throw new Exception("Indirect screens must reset even with ResetOnSpawn=false.");
        var replacement = gui.FindFirstChild("Reset")!;
        authority.Character = (Model)Instance.Create("Model"); authority.Character.Parent = game.Workspace;
        host.Pump();
        if (!replacement.IsDestroyed || gui.GetChildren().Count != 3)
            throw new Exception("Repeated respawn must replace screens without duplicates.");
    }
}
