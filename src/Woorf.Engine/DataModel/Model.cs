using Woorf.Engine.Datatypes;
using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public class Model : Instance
{
    private readonly PivotMovement movement = new();
    private CFrame worldPivot = CFrame.Identity;
    private bool hasWorldPivot;
    private BasePart? primaryPart;
    private bool hasPrimaryBeenDescendant;
    private ScriptConnection? primaryDestroying;
    private ScriptConnection? primaryAncestry;

    internal Model(string className = "Model") : base(className)
    {
        GetSignal("Destroying").Connect(_ => DisconnectPrimaryPart());
    }

    public BasePart? PrimaryPart
    {
        get => primaryPart;
        set
        {
            if (primaryPart == value) return;
            DisconnectPrimaryPart();
            primaryPart = value;
            hasPrimaryBeenDescendant = value?.IsDescendantOf(this) == true;
            if (value is not null)
            {
                primaryDestroying = value.GetSignal("Destroying").Connect(_ => PrimaryPart = null);
                primaryAncestry = value.GetSignal("AncestryChanged").Connect(_ => UpdatePrimaryAncestry(value));
            }
            FirePropertyChanged("PrimaryPart");
        }
    }

    public CFrame WorldPivot
    {
        get => hasWorldPivot ? worldPivot : CalculateInitialPivot();
        set
        {
            var previous = WorldPivot;
            worldPivot = value;
            hasWorldPivot = true;
            if (previous != value) FirePropertyChanged("WorldPivot");
        }
    }

    public CFrame GetPivot() => PrimaryPart?.GetPivot() ?? WorldPivot;
    public void PivotTo(CFrame target) => movement.MoveCached(this, GetPivot(), target);
    public (CFrame Frame, Vector3 Size) GetBoundingBox() =>
        PartBounds.Calculate(GetDescendants().OfType<BasePart>(), GetPivot());
    public Vector3 GetExtentsSize() => GetBoundingBox().Size;

    private void DisconnectPrimaryPart()
    {
        primaryDestroying?.Disconnect();
        primaryAncestry?.Disconnect();
        primaryDestroying = null;
        primaryAncestry = null;
    }

    private void UpdatePrimaryAncestry(BasePart part)
    {
        if (part.IsDescendantOf(this)) hasPrimaryBeenDescendant = true;
        else if (hasPrimaryBeenDescendant) PrimaryPart = null;
    }

    private CFrame CalculateInitialPivot() =>
        PartBounds.Calculate(GetDescendants().OfType<BasePart>(), CFrame.Identity).Frame;

    protected override bool HasProperty(string property) =>
        property is "PrimaryPart" or "WorldPivot" || base.HasProperty(property);

    protected override void CopyPropertiesTo(Instance clone)
    {
        var model = (Model)clone;
        model.worldPivot = worldPivot;
        model.hasWorldPivot = hasWorldPivot;
    }

    protected override void ResolveCloneReferences(Instance clone, IReadOnlyDictionary<Instance, Instance> clones)
    {
        if (primaryPart is not null)
            ((Model)clone).PrimaryPart = clones.TryGetValue(primaryPart, out var copied) ? (BasePart)copied : primaryPart;
    }
}
