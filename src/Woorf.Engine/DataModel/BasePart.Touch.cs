using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public abstract partial class BasePart
{
    private bool canTouch = true;
    private readonly HashSet<BasePart> touchingParts = [];
    public ScriptSignal Touched { get; }
    public ScriptSignal TouchEnded { get; }
    public bool CanTouch
    {
        get => canTouch;
        set
        {
            if (canTouch == value) return;
            canTouch = value;
            if (!value) { Touched.DisconnectAll(); TouchEnded.DisconnectAll(); }
            FirePropertyChanged("CanTouch");
        }
    }

    public IReadOnlyList<BasePart> GetTouchingParts() => !CanCollide && !Touched.HasConnections ? []
        : touchingParts.Where(part => !part.IsDestroyed && part.Parent is not null && part.CanCollide).ToArray();

    internal void SetTouchingParts(IEnumerable<BasePart> parts)
    {
        touchingParts.Clear();
        touchingParts.UnionWith(parts);
    }
}
