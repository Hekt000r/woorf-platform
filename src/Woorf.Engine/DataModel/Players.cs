using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public sealed class Players : Instance
{
    internal Players() : base("Players")
    {
        GetSignal("ChildAdded").Connect(arguments =>
        {
            if (arguments[0] is Player player) PlayerAdded.Fire(player);
        });
        GetSignal("Destroying").Connect(_ => { PlayerAdded.Close(); PlayerRemoving.Close(); });
    }

    public Player? LocalPlayer { get; internal set; }
    public ScriptSignal PlayerRemoving { get; } = new();
    public ScriptSignal PlayerAdded { get; } = new();
    private bool shouldAutoLoadCharacters;
    private double respawnTime = 3;
    public bool CharacterAutoLoads
    {
        get => shouldAutoLoadCharacters;
        set { if (shouldAutoLoadCharacters == value) return; shouldAutoLoadCharacters = value; FirePropertyChanged("CharacterAutoLoads"); }
    }
    public double RespawnTime
    {
        get => respawnTime;
        set
        {
            if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (respawnTime == value) return;
            respawnTime = value; FirePropertyChanged("RespawnTime");
        }
    }
    public IReadOnlyList<Player> GetPlayers() => GetChildren().OfType<Player>().ToArray();

    protected override bool HasProperty(string property) => property is "LocalPlayer" or "CharacterAutoLoads" or "RespawnTime" || base.HasProperty(property);
}
