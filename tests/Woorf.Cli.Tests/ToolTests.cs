using Woorf.Engine.DataModel;
using Woorf.Engine.Networking;
using Woorf.Engine.Scheduling;

internal static class ToolTests
{
    internal static int Run()
    {
        var game = new DataModel();
        var template = (Tool)Instance.Create("Tool");
        template.Name = "Marker";
        template.RequiresHandle = false;
        template.Parent = game.GetService("StarterPack");
        var script = (ScriptInstance)Instance.Create("Script");
        script.Parent = template;
        script.ScriptSource = """
            local tool = script.Parent
            assert(tool:IsA("BackpackItem") and not tool.RequiresHandle)
            tool.Equipped:Connect(function() tool:SetAttribute("EquippedByServer", true) end)
            tool.Activated:Connect(function() tool:SetAttribute("UsedByServer", true) end)
            """;
        var localScript = (ScriptInstance)Instance.Create("LocalScript");
        localScript.Parent = template;
        localScript.ScriptSource = """
            script.Parent.Activated:Connect(function() print("local tool activated") end)
            """;
        using var host = new LocalPlayHost(new(game, []), new MonotonicClock());
        var tool = host.Player.FindFirstChild("Backpack")?.FindFirstChild("Marker") as Tool
            ?? throw new Exception("StarterPack tool must replicate to the player's Backpack.");
        var activated = 0;
        var released = 0;
        tool.Activated.Connect(_ => activated++);
        tool.Deactivated.Connect(_ => released++);
        var humanoid = (Humanoid)host.Player.Character!.FindFirstChild("Humanoid")!;
        host.Pump();
        var hotbar = new Woorf.Engine.Gui.ToolHotbar(host.Player, host.ToggleTool, host.UseTool);
        if (!hotbar.Capture(800, 600, true).Any(item => item.Content == "1  Marker") || !hotbar.KeyDown("1"))
            throw new Exception("The first starter tool must be visible and selectable in slot 1.");
        if (host.Pump() != 0) throw new Exception("Equip replication failed.");
        hotbar.Pointer(400, 300, true, false);
        hotbar.Pointer(400, 300, false, false);
        if (host.Pump() != 0 || tool.GetAttribute("UsedByServer") is not true ||
            tool.GetAttribute("EquippedByServer") is not true || !host.Game.ScriptOutput.Contains("local tool activated"))
            throw new Exception("Authoritative tool scripts must receive equip and activation.");
        if (tool.Parent != host.Player.Character || activated != 1 || released != 1)
            throw new Exception("Equipped tools must activate and deactivate.");
        tool.Enabled = false;
        tool.Activate();
        tool.Enabled = true;
        tool.RequiresHandle = true;
        tool.Activate();
        if (activated != 1) throw new Exception("Disabled and handleless tools must not activate.");
        humanoid.UnequipTools();
        if (tool.Parent?.ClassName != "Backpack") throw new Exception("Unequip must return the tool to Backpack.");
        ((Players)game.GetService("Players")).RespawnTime = 0;
        var oldBackpack = host.Player.FindFirstChild("Backpack");
        host.SendInput(default, false, true);
        if (host.Pump() != 0 || host.Player.FindFirstChild("Backpack") is not { } newBackpack ||
            newBackpack == oldBackpack || newBackpack.FindFirstChild("Marker") is not Tool)
            throw new Exception("Respawn must replace the backpack and clone starter tools again.");
        using (var session = new Woorf.Engine.Scripting.LuauSession(new DataModel(), new MonotonicClock()))
        {
            if (session.Execute("""
                local tool = Instance.new("Tool")
                tool.Enabled = false
                tool.RequiresHandle = false
                tool.GripPos = Vector3.new(1, 2, 3)
                assert(not tool.Enabled and not tool.RequiresHandle and tool.Grip.Position.X == 1)
                """, "tool-api") != 0)
                throw new Exception("Luau Tool properties must round-trip.");
        }
        Console.WriteLine("PASS tool spawn, hotbar, authoritative and client activation, and Luau properties");
        return 0;
    }
}
