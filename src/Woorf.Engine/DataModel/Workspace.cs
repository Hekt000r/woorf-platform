namespace Woorf.Engine.DataModel;

public sealed class Workspace : Model
{
    private double gravity = 196.2;
    private Camera? currentCamera;
    private Events.ScriptConnection? cameraParentChanged;
    internal Workspace() : base("Workspace") { }
    public double Gravity
    {
        get => gravity;
        set
        {
            if (!double.IsFinite(value) || Math.Abs(value) > float.MaxValue) throw new ArgumentException("Expected finite gravity.");
            if (gravity == value) return;
            gravity = value;
            FirePropertyChanged("Gravity");
        }
    }
    public Camera? CurrentCamera => Parent is DataModel game && ((RunService)game.GetService("RunService")).IsClient()
        ? currentCamera ?? SetCurrentCamera(null) : null;

    internal Camera SetCurrentCamera(Camera? camera)
    {
        if (camera is null || !camera.IsDescendantOf(this)) camera = new Camera { Parent = this };
        if (currentCamera == camera) return camera;
        var previous = currentCamera;
        cameraParentChanged?.Disconnect();
        currentCamera = camera;
        cameraParentChanged = camera.GetPropertyChangedSignal("Parent").Connect(_ =>
        {
            if (currentCamera == camera && !camera.IsDescendantOf(this)) SetCurrentCamera(null);
        });
        var obsolete = GetChildren().OfType<Camera>().Where(other => other != camera).ToArray();
        foreach (var other in obsolete) other.Destroy();
        if (previous != camera && previous?.IsDescendantOf(this) == true) previous.Destroy();
        FirePropertyChanged(nameof(CurrentCamera));
        return camera;
    }

    protected override bool HasProperty(string property) =>
        property is nameof(Gravity) or nameof(CurrentCamera) || base.HasProperty(property);
}
