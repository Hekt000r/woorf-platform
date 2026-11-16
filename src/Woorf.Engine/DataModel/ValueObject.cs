using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

public sealed class ValueObject : Instance
{
    private object? value;
    internal ValueObject(string className) : base(className)
    {
        value = className switch
        {
            "Vector3Value" => new Vector3(), "CFrameValue" => CFrame.Identity, "Color3Value" => new Color3(),
            "StringValue" => "", "BoolValue" => false, "ObjectValue" => null,
 _ => 0d,
        };
    }

    public object? Value
    {
        get => value;
        set
        {
            var isValid = ClassName switch
            {
                "Vector3Value" => value is Vector3, "CFrameValue" => value is CFrame, "Color3Value" => value is Color3,
                "StringValue" => value is string, "BoolValue" => value is bool,
                "ObjectValue" => value is null or Instance,
                    _ => value is double,
            };
            if (!isValid) throw new InvalidOperationException("Invalid Value type.");
            var next = ClassName == "IntValue" ? Math.Round((double)value!, MidpointRounding.AwayFromZero) : value;
            if (Equals(this.value, next)) return;
            this.value = next;
            FirePropertyChanged("Value");
            GetSignal("Changed").Fire(next);
        }
    }

    protected override void FirePropertyChanged(string property)
    {
        if (property != "Value") { base.FirePropertyChanged(property); return; }
        NotifyMutation(property);
        GetPropertyChangedSignal("Value").Fire();
    }

    protected override bool HasProperty(string property) => property == "Value" || base.HasProperty(property);
    protected override void CopyPropertiesTo(Instance clone) => ((ValueObject)clone).value = value;
    protected override void ResolveCloneReferences(Instance clone, IReadOnlyDictionary<Instance, Instance> clones)
    {
        if (value is Instance instance) ((ValueObject)clone).value = clones.GetValueOrDefault(instance, instance);
    }
}
