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
        GetSignal("Destroying").Connect(_ => PlayerAdded.Close());
    }

    public Player? LocalPlayer { get; internal set; }
    public ScriptSignal PlayerAdded { get; } = new();
    public IReadOnlyList<Player> GetPlayers() => GetChildren().OfType<Player>().ToArray();

    protected override bool HasProperty(string property) => property == "LocalPlayer" || base.HasProperty(property);
}
