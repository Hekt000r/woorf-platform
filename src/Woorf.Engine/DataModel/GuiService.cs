using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

public sealed class GuiService : Instance
{
    private double preferredTransparency = 1;
    internal const int TopbarHeight = 44;
    public Rect TopbarInset { get; private set; } = new(default, new(1, TopbarHeight));
    private GuiInstance? selectedObject;
    public GuiInstance? SelectedObject
    {
        get => selectedObject is { IsDestroyed: false } ? selectedObject : null;
        set
        {
            if (selectedObject == value) return;
            var previous = selectedObject;
            selectedObject = value;
            previous?.FireGuiSignal("SelectionLost");
            value?.FireGuiSignal("SelectionGained");
            FirePropertyChanged(nameof(SelectedObject));
        }
    }

    internal void ResizeTopbar(int width)
    {
        var inset = new Rect(default, new(Math.Max(1, width), TopbarHeight));
        if (inset == TopbarInset) return;
        TopbarInset = inset;
        FirePropertyChanged(nameof(TopbarInset));
    }

    internal GuiService() : base("GuiService") { }

    public double PreferredTransparency
    {
        get => preferredTransparency;
        internal set
        {
            if (!double.IsFinite(value) || value is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (preferredTransparency == value) return;
            preferredTransparency = value;
            FirePropertyChanged(nameof(PreferredTransparency));
        }
    }

    protected override bool HasProperty(string property) =>
        property is nameof(PreferredTransparency) or nameof(TopbarInset) or nameof(SelectedObject) or "MenuIsOpen" || base.HasProperty(property);
}
