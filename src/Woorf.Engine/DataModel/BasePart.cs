using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

public abstract partial class BasePart : Instance
{
    private readonly PivotMovement movement = new();
    private CFrame cframe = CFrame.Identity;
    private CFrame pivotOffset = CFrame.Identity;
    private Vector3 size = new(4, 1.2f, 2);
    private bool isAnchored;
    private bool locked;
    private double reflectance;
    public double Reflectance
    {
        get => reflectance;
        set
        {
            if (!double.IsFinite(value)) throw new ArgumentException("Expected finite reflectance.");
            var next = Math.Clamp(value, 0, 1);
            if (reflectance == next) return;
            reflectance = next;
            FirePropertyChanged(nameof(Reflectance));
        }
    }
    private readonly Dictionary<string, GuiEnum> surfaces = [];

    internal static bool IsSurfaceProperty(string property) =>
        property is "TopSurface" or "BottomSurface" or "LeftSurface" or "RightSurface" or "FrontSurface" or "BackSurface";

    internal GuiEnum GetSurface(string property) => surfaces.GetValueOrDefault(property) ?? GuiEnum.From("SurfaceType", "Smooth");
    internal void SetSurface(string property, GuiEnum value)
    {
        if (value.EnumType != "SurfaceType") throw new ArgumentException("Expected Enum.SurfaceType.");
        if (GetSurface(property) == value) return;
        surfaces[property] = value;
        FirePropertyChanged(property);
    }

    internal BasePart(string className) : base(className)
    {
        Touched = new(() => CanTouch);
        TouchEnded = new(() => CanTouch);
        GetSignal("Destroying").Connect(_ => { Touched.Close(); TouchEnded.Close(); touchingParts.Clear(); });
    }

    public Vector3 Position
    {
        get => cframe.Position;
        set
        {
            if (Position != value) SetTransform(cframe.WithPosition(value));
        }
    }

    public CFrame CFrame
    {
        get => cframe;
        set => SetTransform(value.Orthonormalize());
    }

    public CFrame PivotOffset
    {
        get => pivotOffset;
        set
        {
            if (pivotOffset == value) return;
            pivotOffset = value;
            FirePropertyChanged("PivotOffset");
        }
    }

    public CFrame GetPivot() => cframe * pivotOffset;

    public void PivotTo(CFrame target) => movement.MoveCached(this, GetPivot(), target);

    internal void MoveToPivotTransform(CFrame value) => SetTransform(value.Orthonormalize(), false);

    private void SetTransform(CFrame value, bool shouldNotifyPosition = true)
    {
        if (cframe == value) return;
        var hasPositionChanged = cframe.Position != value.Position;
        cframe = value;
        FirePropertyChanged("CFrame");
        if (shouldNotifyPosition && hasPositionChanged) FirePropertyChanged("Position");
    }

    public Vector3 Size
    {
        get => size;
        set
        {
            var clamped = new Vector3(Math.Clamp(value.X, 0.001f, 2048),
                Math.Clamp(value.Y, 0.001f, 2048), Math.Clamp(value.Z, 0.001f, 2048));
            if (size == clamped) return;
            size = clamped;
            FirePropertyChanged("Size");
        }
    }

    public bool Anchored
    {
        get => isAnchored;
        set
        {
            if (isAnchored == value) return;
            isAnchored = value;
            FirePropertyChanged("Anchored");
        }
    }

    public bool Locked
    {
        get => locked;
        set
        {
            if (locked == value) return;
            locked = value;
            FirePropertyChanged(nameof(Locked));
        }
    }

    protected override bool HasProperty(string property) =>
        property is "Position" or "CFrame" or "PivotOffset" or "Size" or "Anchored" or "Locked" or "CanTouch" or "Color" or "BrickColor" or "Reflectance" || IsSurfaceProperty(property) || HasPhysicsProperty(property) || base.HasProperty(property);

    protected override void CopyPropertiesTo(Instance clone)
    {
        var part = (BasePart)clone;
        part.ColorOverride = ColorOverride;
        part.cframe = cframe;
        part.pivotOffset = pivotOffset;
        part.size = size;
        part.isAnchored = isAnchored;
        part.canTouch = canTouch;
        part.locked = locked;
        part.reflectance = reflectance;
        foreach (var (name, value) in surfaces) part.surfaces.Add(name, value);
        CopyPhysicsTo(part);
    }
}
