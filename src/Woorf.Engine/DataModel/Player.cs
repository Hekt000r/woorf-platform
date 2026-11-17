namespace Woorf.Engine.DataModel;

public sealed partial class Player : Instance
{
    internal Player(int userId = 0, string name = "Player") : base("Player")
    {
        UserId = userId;
        Name = name;
        GetSignal("Destroying").Connect(_ => { Character = null; CharacterAdded.Close(); CharacterRemoving.Close(); });
    }

    private Instance? team;
    public Instance? Team
    {
        get => team;
        set
        {
            if (value is not null && value.ClassName != "Team") throw new InvalidOperationException("Expected Team.");
            if (team == value) return;
            team = value;
            FirePropertyChanged("Team");
            FirePropertyChanged("Neutral");
        }
    }
    public bool Neutral => Team is null;
    public int UserId { get; internal set; }

    protected override bool HasProperty(string property) => property is "UserId" or "Team" or "Neutral" or "Character" || base.HasProperty(property);

    protected override void CopyPropertiesTo(Instance clone) => ((Player)clone).UserId = UserId;
}
