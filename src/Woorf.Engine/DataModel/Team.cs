using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

public sealed class Team : Instance
{
    private BrickColor teamColor = new(1);

    internal Team() : base("Team") { }

    public BrickColor TeamColor
    {
        get => teamColor;
        set
        {
            if (teamColor == value) return;
            teamColor = value;
            FirePropertyChanged(nameof(TeamColor));
        }
    }

    protected override bool HasProperty(string property) => property == nameof(TeamColor) || base.HasProperty(property);
    protected override void CopyPropertiesTo(Instance clone) => ((Team)clone).teamColor = teamColor;
}
