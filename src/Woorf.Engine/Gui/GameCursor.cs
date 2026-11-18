using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Gui;

public sealed class GameCursor
{
    private GuiRect viewport;
    public Vector2 Position { get; private set; }

    public void Resize(int width, int height)
    {
        if (viewport.Width == width && viewport.Height == height) return;
        var shouldCenter = viewport.Width <= 0 || viewport.Height <= 0;
        viewport = new GuiRect(0, 0, width, height);
        if (shouldCenter) Position = new Vector2(width / 2f, height / 2f);
        Move(0, 0, false);
    }

    public void Move(float deltaX, float deltaY, bool isFrozen)
    {
        if (isFrozen) return;
        Place(Position.X + deltaX, Position.Y + deltaY);
    }

    public void Place(float x, float y) => Position = new Vector2(
        Math.Clamp(x, 0, Math.Max(0, viewport.Width - 1)),
        Math.Clamp(y, 0, Math.Max(0, viewport.Height - 1)));

    public IEnumerable<GuiDrawCommand> Draw(bool isVisible)
    {
        if (!isVisible || viewport.Width <= 0 || viewport.Height <= 0) yield break;
        var shadow = new GuiRect(Position.X - 1, Position.Y - 1, 10, 10);
        var tip = new GuiRect(Position.X, Position.Y, 6, 6);
        yield return new("Rectangle", shadow, viewport, new Color3(0, 0, 0), 1, Radius: 3);
        yield return new("Rectangle", tip, viewport, new Color3(1, 1, 1), 1, Radius: 2);
    }
}
