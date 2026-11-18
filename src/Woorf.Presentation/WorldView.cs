using Woorf.Engine.DataModel;
using Woorf.Graphics;

namespace Woorf.Presentation;

public sealed record WorldFrame(RenderItem[] Items, Skybox? Sky);

public sealed class WorldView : IDisposable
{
    private readonly WorkspaceScene scene;
    private readonly WorkspaceSky sky;
    private readonly CharacterFrameComposer? characters;
    private readonly Player? player;
    private readonly NpcFrameComposer npcs;

    public WorldView(DataModel game, Player? player)
    {
        this.player = player;
        npcs=new(game);
        scene = new(game.Workspace); sky = new(game);
        if (player is not null) characters = new(player);
    }

    public WorldFrame Capture(double now)
    {
        scene.SetHiddenPart(player?.Character?.PrimaryPart as Part);
        var items = scene.Capture();
        return new(npcs.Capture(characters?.Capture(items, now) ?? items,now), sky.Capture());
    }

    public void Dispose() { characters?.Dispose(); npcs.Dispose(); sky.Dispose(); scene.Dispose(); }
}
