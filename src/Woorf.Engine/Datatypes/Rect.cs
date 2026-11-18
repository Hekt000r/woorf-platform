namespace Woorf.Engine.Datatypes;

public readonly record struct Rect(Vector2 Min, Vector2 Max)
{
    public float Width => Max.X - Min.X;
    public float Height => Max.Y - Min.Y;
}
