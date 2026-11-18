using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

public sealed class RigidJoint : Instance
{
    private BasePart? part0, part1;
    private bool enabled = true;
    private CFrame c0 = CFrame.Identity, c1 = CFrame.Identity;

    internal RigidJoint(string className) : base(className) { }

    public BasePart? Part0 { get => part0; set => Set(ref part0, value, nameof(Part0)); }
    public BasePart? Part1 { get => part1; set => Set(ref part1, value, nameof(Part1)); }
    public bool Enabled { get => enabled; set => Set(ref enabled, value, nameof(Enabled)); }
    public CFrame C0 { get => c0; set => Set(ref c0, value, nameof(C0)); }
    public CFrame C1 { get => c1; set => Set(ref c1, value, nameof(C1)); }
    internal bool IsActive => Enabled && Part0 is { IsDestroyed: false } && Part1 is { IsDestroyed: false };

    protected override bool HasProperty(string property) => property is nameof(Part0) or nameof(Part1) or
        nameof(Enabled) or nameof(C0) or nameof(C1) or "Active" || base.HasProperty(property);

    protected override void CopyPropertiesTo(Instance clone)
    {
        var joint = (RigidJoint)clone;
        joint.enabled = enabled;
        joint.c0 = c0;
        joint.c1 = c1;
    }

    protected override void ResolveCloneReferences(Instance instance, IReadOnlyDictionary<Instance, Instance> clones)
    {
        var joint = (RigidJoint)instance;
        joint.part0 = part0 is null ? null : (BasePart)clones.GetValueOrDefault(part0, part0);
        joint.part1 = part1 is null ? null : (BasePart)clones.GetValueOrDefault(part1, part1);
    }

    private void Set<T>(ref T field, T value, string property)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        FirePropertyChanged(property);
    }
}
