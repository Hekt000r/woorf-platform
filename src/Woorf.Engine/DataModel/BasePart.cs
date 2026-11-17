using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

public abstract partial class BasePart : Instance
{
    private readonly PivotMovement movement = new();
    private CFrame cframe = CFrame.Identity;
    private CFrame pivotOffset = CFrame.Identity;
    private Vector3 size = new(4, 1.2f, 2);
    private bool isAnchored;

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

    protected override bool HasProperty(string property) =>
        property is "Position" or "CFrame" or "PivotOffset" or "Size" or "Anchored" or "CanTouch" || HasPhysicsProperty(property) || base.HasProperty(property);

    protected override void CopyPropertiesTo(Instance clone)
    {
        var part = (BasePart)clone;
        part.cframe = cframe;
        part.pivotOffset = pivotOffset;
        part.size = size;
        part.isAnchored = isAnchored;
        part.canTouch = canTouch;
        CopyPhysicsTo(part);
    }
}
