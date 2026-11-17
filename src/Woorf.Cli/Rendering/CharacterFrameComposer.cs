using Woorf.Engine.DataModel;
using Woorf.Graphics;

internal sealed class CharacterFrameComposer(Player player)
{
    private readonly CharacterView view = new();
    private RenderItem[] items = [];
    private Model? previousCharacter;
    private Humanoid? humanoid;

    internal RenderItem[] Capture(RenderItem[] scene, double now)
    {
        var model = player.Character;
        if (model != previousCharacter)
        {
            previousCharacter = model;
            humanoid = model?.FindFirstChild("Humanoid") as Humanoid;
        }
        if (model?.PrimaryPart is not Part root || root.IsDestroyed || humanoid is null || humanoid.IsDestroyed || humanoid.Parent != model) return scene;
        var character = view.Capture(root, humanoid, now);
        if (items.Length != scene.Length + character.Length) items = new RenderItem[scene.Length + character.Length];
        scene.CopyTo(items, 0); character.CopyTo(items, scene.Length);
        return items;
    }
}
