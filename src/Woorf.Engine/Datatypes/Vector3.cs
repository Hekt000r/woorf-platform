using System.Globalization;

namespace Woorf.Engine.Datatypes;

public readonly struct Vector3(float x, float y, float z) : IEquatable<Vector3>
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Z { get; } = z;
    public float Magnitude => MathF.Sqrt(X * X + Y * Y + Z * Z);
    public Vector3 Unit => this / Magnitude;

    public static Vector3 operator +(Vector3 left, Vector3 right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    public static Vector3 operator -(Vector3 left, Vector3 right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    public static Vector3 operator *(Vector3 value, float scalar) => new(value.X * scalar, value.Y * scalar, value.Z * scalar);
    public static Vector3 operator /(Vector3 value, float scalar) => new(value.X / scalar, value.Y / scalar, value.Z / scalar);
    public float Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;
    public Vector3 Cross(Vector3 other) => new(Y * other.Z - Z * other.Y, Z * other.X - X * other.Z, X * other.Y - Y * other.X);
    public Vector3 Lerp(Vector3 goal, float alpha) => this + (goal - this) * alpha;

    public bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? other) => other is Vector3 vector && Equals(vector);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);
    public static bool operator !=(Vector3 left, Vector3 right) => !left.Equals(right);
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{X}, {Y}, {Z}");
}
