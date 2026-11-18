namespace Woorf.Engine.DataModel;

internal static class PlayerInventory
{
    internal static void Reset(DataModel game, Player player)
    {
        var gear = player.FindFirstChild("StarterGear");
        if (gear is null)
        {
            gear = Instance.CreateRuntime("StarterGear");
            gear.Parent = player;
        }
        player.FindFirstChild("Backpack")?.Destroy();
        var backpack = Instance.CreateRuntime("Backpack");
        foreach (var container in new[] { game.FindService("StarterPack"), gear })
        {
            if (container is null) continue;
            foreach (var template in container.GetChildren().OfType<Tool>())
                if (template.Clone() is Tool tool) tool.Parent = backpack;
        }
        backpack.Parent = player;
    }

    internal static void Toggle(Player player, Tool tool)
    {
        if (player.Character?.FindFirstChild("Humanoid") is not Humanoid { Health: > 0 } humanoid) return;
        if (tool.Parent == player.Character) humanoid.UnequipTools();
        else if (tool.Parent is { ClassName: "Backpack" } backpack && backpack.Parent == player) humanoid.EquipTool(tool);
    }

    internal static void Use(Player player, bool isDown)
    {
        var tool = player.Character?.GetChildren().OfType<Tool>().FirstOrDefault();
        if (!isDown) tool?.Deactivate();
        else if (tool is { ManualActivationOnly: false }) tool.Activate();
    }
}
