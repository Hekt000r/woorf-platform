namespace Woorf.Engine.DataModel;

public sealed class Player : Instance
{
    internal Player(int userId = 0, string name = "Player") : base("Player")
    {
        UserId = userId;
        Name = name;
    }

    public int UserId { get; internal set; }

    protected override bool HasProperty(string property) => property == "UserId" || base.HasProperty(property);

    protected override void CopyPropertiesTo(Instance clone) => ((Player)clone).UserId = UserId;
}
