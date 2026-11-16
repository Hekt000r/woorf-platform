namespace Woorf.Engine.DataModel;

public sealed class Workspace : Model
{
    private double gravity = 196.2;
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
    protected override bool HasProperty(string property) => property == "Gravity" || base.HasProperty(property);
}
