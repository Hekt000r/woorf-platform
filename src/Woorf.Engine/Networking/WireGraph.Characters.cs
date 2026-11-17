using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal sealed partial class WireGraph
{
    private readonly Dictionary<Player, Model> characterReferences = [];
    private readonly Dictionary<Model, HashSet<Player>> characterDependents = [];

    private void TrackCharacter(Player player)
    {
        if (characterReferences.GetValueOrDefault(player) == player.Character) return;
        ForgetCharacter(player);
        if (player.Character is not Model model) return;
        characterReferences[player] = model;
        if (!characterDependents.TryGetValue(model, out var holders)) characterDependents[model] = holders = [];
        holders.Add(player);
    }

    private void ForgetCharacter(Player player)
    {
        if (!characterReferences.Remove(player, out var model)) return;
        if (!characterDependents.TryGetValue(model, out var holders)) return;
        holders.Remove(player);
        if (holders.Count == 0) characterDependents.Remove(model);
    }
}
