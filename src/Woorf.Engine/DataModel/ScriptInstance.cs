namespace Woorf.Engine.DataModel;

public sealed class ScriptInstance : Instance
{
    internal ScriptInstance(string className) : base(className) { }
    private bool isEnabled = true;
    private int runContext;
    public bool Enabled
    {
        get => isEnabled;
        set
        {
            if (isEnabled == value) return;
            isEnabled = value;
            FirePropertyChanged("Enabled");
            FirePropertyChanged("Disabled");
        }
    }
    public int RunContext
    {
        get => runContext;
        set
        {
            if (value is < 0 or > 2) throw new InvalidOperationException("Unsupported RunContext.");
            if (runContext == value) return;
            runContext = value;
            FirePropertyChanged("RunContext");
        }
    }
    protected override bool HasProperty(string property) => property is "Enabled" or "Disabled" or "RunContext" || base.HasProperty(property);
    protected override void CopyPropertiesTo(Instance clone)
    {
        var script = (ScriptInstance)clone;
        script.Enabled = Enabled;
        script.RunContext = RunContext;
    }
}
