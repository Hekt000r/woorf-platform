namespace Woorf.Presentation;

using Woorf.Engine.DataModel;
using Woorf.Graphics;

internal sealed class CharacterFrameComposer(Player player) : IDisposable
{
    private readonly CharacterView view = new();
    private RenderItem[] items = [];
    private Model? previousCharacter;
    private Humanoid? humanoid;
    private readonly Dictionary<Player, CharacterFrameComposer> peers = [];
    private RenderItem[] rendered = [];
    private Players? observedPlayers;
    private Player[] currentPlayers = [];
    private bool isMembershipDirty = true;

    internal RenderItem[] Capture(RenderItem[] scene, double now)
    {
        if (player.Parent is not Players players) return CaptureSingle(scene, now);
        RefreshPlayers(players);
        var count = scene.Length;
        foreach (var peer in currentPlayers)
        {
            if (!peers.TryGetValue(peer, out var composer)) peers.Add(peer, composer = new(peer));
            count += composer.CaptureSingle([], now).Length;
        }
        if (items.Length != count) items = new RenderItem[count];
        scene.CopyTo(items, 0);
        var offset = scene.Length;
        foreach (var peer in currentPlayers)
        {
            var character = peers[peer].rendered;
            character.CopyTo(items, offset); offset += character.Length;
        }
        return items;
    }

    private void RefreshPlayers(Players players)
    {
        if (observedPlayers != players)
        {
            if (observedPlayers is not null) observedPlayers.DescendantMutated -= OnMutation;
            observedPlayers = players; observedPlayers.DescendantMutated += OnMutation;
            isMembershipDirty = true;
        }
        if (!isMembershipDirty) return;
        currentPlayers = players.GetPlayers().ToArray();
        RemoveDepartedPlayers(players);
        isMembershipDirty = false;
    }

    private void RemoveDepartedPlayers(Players players)
    {
        foreach (var departed in peers.Keys.Where(peer => peer.Parent != players).ToArray()) peers.Remove(departed);
    }

    private void OnMutation(Instance instance, string property)
    {
        if (instance is Player && property == "Parent") isMembershipDirty = true;
    }

    public void Dispose()
    {
        if (observedPlayers is not null) observedPlayers.DescendantMutated -= OnMutation;
        foreach (var peer in peers.Values) peer.Dispose();
        peers.Clear();
    }

    private RenderItem[] CaptureSingle(RenderItem[] scene, double now)
    {
        var model = player.Character;
        if (model != previousCharacter)
        {
            previousCharacter = model;
            humanoid = model?.FindFirstChild("Humanoid") as Humanoid;
        }
        if (model?.PrimaryPart is not Part root || root.IsDestroyed || humanoid is null || humanoid.IsDestroyed || humanoid.Parent != model) return rendered = scene;
        var character = view.Capture(root, humanoid, now);
        if (items.Length != scene.Length + character.Length) items = new RenderItem[scene.Length + character.Length];
        scene.CopyTo(items, 0); character.CopyTo(items, scene.Length);
        return rendered = items;
    }
}

