namespace Woorf.Engine.DataModel;

public sealed class Part : BasePart
{
    internal Part() : base("Part") { }
    private PartType shape = PartType.Block;
    public PartType Shape
    {
        get => shape;
        set
        {
            if (value is not (PartType.Block or PartType.Ball)) throw new ArgumentException("Only Block and Ball shapes are supported.");
            if (shape == value) return;
            shape = value; FirePropertyChanged("Shape");
        }
    }
    protected override bool HasProperty(string property) => property == "Shape" || base.HasProperty(property);
    protected override void CopyPropertiesTo(Instance clone) { base.CopyPropertiesTo(clone); ((Part)clone).shape = shape; }
}

public enum PartType { Ball = 0, Block = 1 }
