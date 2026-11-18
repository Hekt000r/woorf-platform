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

    private double cameraMinZoomDistance = .5;
    private double cameraMaxZoomDistance = 128;
    private Datatypes.GuiEnum cameraMode = Datatypes.GuiEnum.From("CameraMode", "Classic");

    public double CameraMinZoomDistance
    {
        get => cameraMinZoomDistance;
        set => SetZoomDistance(ref cameraMinZoomDistance, value, .5, cameraMaxZoomDistance, nameof(CameraMinZoomDistance));
    }

    public double CameraMaxZoomDistance
    {
        get => cameraMaxZoomDistance;
        set => SetZoomDistance(ref cameraMaxZoomDistance, value, cameraMinZoomDistance, 400, nameof(CameraMaxZoomDistance));
    }

    public Datatypes.GuiEnum CameraMode
    {
        get => cameraMode;
        set
        {
            if (value.EnumType != "CameraMode") throw new ArgumentException("Expected Enum.CameraMode.");
            if (cameraMode == value) return;
            cameraMode = value;
            FirePropertyChanged(nameof(CameraMode));
        }
    }

    private void SetZoomDistance(ref double distance, double value, double min, double max, string property)
    {
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        var updated = Math.Clamp(value, min, max);
        if (distance == updated) return;
        distance = updated;
        FirePropertyChanged(property);
    }

    protected override bool HasProperty(string property) => property is "UserId" or "Team" or "Neutral" or "Character" or "CameraMinZoomDistance"
        or "CameraMaxZoomDistance" or "CameraMode" || base.HasProperty(property);

    protected override void CopyPropertiesTo(Instance clone)
    {
        var player = (Player)clone;
        player.UserId = UserId;
        player.cameraMinZoomDistance = cameraMinZoomDistance;
        player.cameraMaxZoomDistance = cameraMaxZoomDistance;
        player.cameraMode = cameraMode;
    }
}
