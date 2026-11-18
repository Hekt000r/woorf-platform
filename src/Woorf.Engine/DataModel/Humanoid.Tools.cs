namespace Woorf.Engine.DataModel;

public sealed partial class Humanoid
{
    public void EquipTool(Tool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        if (Parent is not Model character || Health <= 0 || tool.Parent == character) return;
        UnequipTools();
        tool.Parent = character;
    }

    public void UnequipTools()
    {
        if (Parent is not Model character) return;
        var game = GetAncestors().OfType<DataModel>().FirstOrDefault();
        var players = game?.FindService("Players") as Players;
        var backpack = players?.GetPlayerFromCharacter(character)?.FindFirstChild("Backpack");
        if (backpack is null) return;
        foreach (var tool in character.GetChildren().OfType<Tool>()) tool.Parent = backpack;
    }
}
