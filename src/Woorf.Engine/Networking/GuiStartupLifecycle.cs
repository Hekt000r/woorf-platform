using Woorf.Engine.DataModel;
using Woorf.Engine.Events;

namespace Woorf.Engine.Networking;

internal sealed class GuiStartupLifecycle : IDisposable
{
    private readonly Instance? starter;
    private readonly Instance playerGui;
    private readonly Dictionary<Instance, Instance> persistentClones = [];
    private readonly ScriptConnection spawned;

    internal GuiStartupLifecycle(Player player, Instance playerGui, Instance? starter)
    {
        this.starter = starter;
        this.playerGui = playerGui;
        CopyTemplates();
        spawned = player.CharacterAdded.Connect(_ => Reset());
    }

    private static bool ShouldPersist(Instance instance) =>
        instance is GuiInstance { ClassName: "ScreenGui" } gui && !gui.Get<bool>("ResetOnSpawn");

    private void Reset()
    {
        if (playerGui.IsDestroyed) return;
        foreach (var child in playerGui.GetChildren())
            if (!ShouldPersist(child)) child.Destroy();
        CopyTemplates();
    }

    private void CopyTemplates()
    {
        if (starter is null || starter.IsDestroyed) return;
        foreach (var template in starter.GetChildren())
        {
            var shouldPersist = ShouldPersist(template);
            if (shouldPersist && persistentClones.TryGetValue(template, out var previous)
                && !previous.IsDestroyed && previous.Parent == playerGui) continue;
            var clone = template.Clone();
            if (clone is null) continue;
            clone.Parent = playerGui;
            if (shouldPersist) persistentClones[template] = clone;
        }
    }

    public void Dispose() { spawned.Disconnect(); persistentClones.Clear(); }
}
