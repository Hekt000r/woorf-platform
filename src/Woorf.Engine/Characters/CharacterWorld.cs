using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Physics;

namespace Woorf.Engine.Characters;

internal sealed class CharacterWorld : IDisposable
{
    private readonly DataModel.DataModel game;
    private readonly PhysicsWorld physics;
    private readonly Players players;
    private readonly Dictionary<Player, Entry> characters = [];
    private KeyValuePair<Player, Entry>[] snapshot = [];
    private bool isMembershipDirty = true, isDisposed;
    private double now;

    internal CharacterWorld(DataModel.DataModel game, PhysicsWorld physics, double now)
    {
        this.game = game; this.physics = physics; this.now = now;
        players = (Players)game.GetService("Players");
        game.DescendantMutated += OnMutation;
    }

    private void OnMutation(Instance instance, string property)
    {
        if (instance is Player && property == "Parent") isMembershipDirty = true;
    }

    internal void Synchronize(double elapsed)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        now = elapsed;
        if (isMembershipDirty) RefreshMembership();
        foreach (var (player, entry) in snapshot)
        {
            if (player.IsDestroyed || player.Parent != players) continue;
            if (entry.Model is null)
            {
                if (players.CharacterAutoLoads) LoadCharacter(player);
                continue;
            }
            if (entry.Root is { IsDestroyed: false, Position.Y: < -100 } && entry.Humanoid is { Health: > 0 })
                entry.Humanoid.Health = 0;
            if (player.IsDestroyed || player.Parent != players)
            {
                RemoveCharacter(player, entry);
                continue;
            }
            var isDead = entry.Model.IsDestroyed || !entry.Model.IsDescendantOf(game.Workspace)
                || entry.Root is null || !entry.Root.IsDescendantOf(entry.Model)
                || entry.Humanoid is null or { IsDestroyed: true } or { Health: <= 0 } || !entry.Humanoid.IsDescendantOf(entry.Model);
            if (!isDead) { entry.RespawnAt = null; continue; }
            entry.RespawnAt ??= now + players.RespawnTime;
            if (players.CharacterAutoLoads && now >= entry.RespawnAt) LoadCharacter(player);
        }
    }

    private void RefreshMembership()
    {
        isMembershipDirty = false;
        foreach (var (player, entry) in snapshot)
        {
            if (!player.IsDestroyed && player.Parent == players) continue;
            RemoveCharacter(player, entry);
            characters.Remove(player);
        }
        foreach (var player in players.GetPlayers())
            if (!characters.ContainsKey(player)) characters.Add(player, new());
        snapshot = characters.ToArray();
    }

    internal void LoadCharacter(Player player)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        if (player.IsDestroyed || player.Parent != players) throw new InvalidOperationException("Player must belong to this world's Players service.");
        if (!characters.TryGetValue(player, out var entry))
        {
            characters.Add(player, entry = new());
            snapshot = characters.ToArray();
        }
        if (entry.IsLoading) return;
        entry.IsLoading = true;
        try
        {
            RemoveCharacter(player, entry);
            CreateCharacter(player, entry);
        }
        finally { entry.IsLoading = false; }
    }

    private void CreateCharacter(Player player, Entry entry)
    {
        var model = (Model)Instance.Create("Model"); model.Name = player.Name;
        model.SetAttribute("WoorfCharacter", true);
        var root = (Part)Instance.Create("Part"); root.Name = "HumanoidRootPart";
        root.Size = new(2, 4, 2); root.CanCollide = true;
        var spawn = game.Workspace.GetAttribute("CharacterSpawn") is Vector3 position ? position : new Vector3(0, 6, 0);
        root.Position = spawn + new Vector3(player.UserId > 0 ? (player.UserId - 1) % 4 * 4 : 0, 0, 0);
        root.CustomPhysicalProperties = new(0.7f, 0, 0);
        root.Parent = model;
        var humanoid = (Humanoid)Instance.Create("Humanoid"); humanoid.Parent = model;
        model.PrimaryPart = root; model.Parent = game.Workspace;
        entry.Model = model; entry.Root = root; entry.Humanoid = humanoid;
        entry.Controller = new(physics, root, humanoid); entry.RespawnAt = null;
        player.Character = model;
        if (player.IsDestroyed || player.Parent != players) RemoveCharacter(player, entry);
    }

    private static void RemoveCharacter(Player player, Entry entry)
    {
        entry.Controller?.Dispose(); entry.Controller = null;
        player.Character = null;
        if (entry.Model is { IsDestroyed: false } model) model.Destroy();
        entry.Model = null; entry.Root = null; entry.Humanoid = null; entry.RespawnAt = null;
    }

    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        game.DescendantMutated -= OnMutation;
        foreach (var (player, entry) in snapshot) RemoveCharacter(player, entry);
        characters.Clear();
        snapshot = [];
    }

    private sealed class Entry
    {
        internal Model? Model;
        internal Part? Root;
        internal Humanoid? Humanoid;
        internal CharacterController? Controller;
        internal double? RespawnAt;
        internal bool IsLoading;
    }
}
