namespace Woorf.Engine.Datatypes;

public readonly record struct Vector2(float X, float Y)
{
    public float Magnitude => MathF.Sqrt(X * X + Y * Y);
    public Vector2 Unit => this / Magnitude;
    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, float b) => new(a.X * b, a.Y * b);
    public static Vector2 operator /(Vector2 a, float b) => new(a.X / b, a.Y / b);
    public float Dot(Vector2 other) => X * other.X + Y * other.Y;
}
