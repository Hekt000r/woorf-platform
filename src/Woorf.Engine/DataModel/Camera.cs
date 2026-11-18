using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

public sealed class Camera : Instance
{
    private CFrame frame = CFrame.Identity;
    private CFrame focus = CFrame.Identity;
    private double fieldOfView = 70;
    private Vector2 viewportSize = new(1, 1);
    private GuiEnum cameraType = GuiEnum.From("CameraType", "Custom");
    private Instance? cameraSubject;

    internal Camera() : base("Camera") { }

    public CFrame CFrame { get => frame; set => Set(ref frame, ValidFrame(value), nameof(CFrame)); }
    public CFrame Focus { get => focus; set => Set(ref focus, ValidFrame(value), nameof(Focus)); }
    public Vector2 ViewportSize => viewportSize;
    public double FieldOfView
    {
        get => fieldOfView;
        set => Set(ref fieldOfView, double.IsFinite(value) ? Math.Clamp(value, 1, 120)
            : throw new ArgumentException("Expected finite field of view."), nameof(FieldOfView));
    }

    public GuiEnum CameraType
    {
        get => cameraType;
        set => Set(ref cameraType, value.EnumType == "CameraType" ? value
            : throw new ArgumentException("Expected Enum.CameraType."), nameof(CameraType));
    }

    public Instance? CameraSubject
    {
        get => cameraSubject is { IsDestroyed: false } subject ? subject :
            (GetAncestors().OfType<DataModel>().FirstOrDefault()?.FindService("Players") as Players)?
                .LocalPlayer?.Character?.FindFirstChild("Humanoid");
        set { if (value is not null) Set(ref cameraSubject, value, nameof(CameraSubject)); }
    }

    internal void Resize(int width, int height) => Set(ref viewportSize,
        new(Math.Max(1, width), Math.Max(1, height)), nameof(ViewportSize));

    protected override bool HasProperty(string property) => property is nameof(CFrame) or nameof(Focus) or
        nameof(ViewportSize) or nameof(FieldOfView) or nameof(CameraType) or nameof(CameraSubject) || base.HasProperty(property);

    protected override void CopyPropertiesTo(Instance clone)
    {
        var camera = (Camera)clone;
        camera.frame = frame;
        camera.focus = focus;
        camera.fieldOfView = fieldOfView;
        camera.cameraType = cameraType;
        camera.cameraSubject = cameraSubject;
    }

    private static CFrame ValidFrame(CFrame value)
    {
        var normalized = value.Orthonormalize();
        return normalized.GetComponents().All(float.IsFinite) ? normalized : throw new ArgumentException("Expected finite CFrame.");
    }

    private void Set<T>(ref T field, T value, string property)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        FirePropertyChanged(property);
    }
}
