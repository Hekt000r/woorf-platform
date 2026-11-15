namespace Woorf.Engine.Datatypes;

public readonly struct CFrame(Vector3 position, Vector3 right, Vector3 up, Vector3 back) : IEquatable<CFrame>
{
    public static CFrame Identity { get; } = new(default, new(1, 0, 0), new(0, 1, 0), new(0, 0, 1));
    public Vector3 Position { get; } = position;
    public Vector3 RightVector { get; } = right;
    public Vector3 UpVector { get; } = up;
    public Vector3 ZVector { get; } = back;
    public Vector3 LookVector => ZVector * -1;
    public CFrame Rotation => WithPosition(default);

    public CFrame(Vector3 position) : this(position, new(1, 0, 0), new(0, 1, 0), new(0, 0, 1)) { }

    public CFrame WithPosition(Vector3 value) => new(value, RightVector, UpVector, ZVector);
    public Vector3 TransformVector(Vector3 value) => RightVector * value.X + UpVector * value.Y + ZVector * value.Z;
    public Vector3 TransformPoint(Vector3 value) => Position + TransformVector(value);

    public CFrame Inverse()
    {
        var rotation = new CFrame(default,
            new(RightVector.X, UpVector.X, ZVector.X),
            new(RightVector.Y, UpVector.Y, ZVector.Y),
            new(RightVector.Z, UpVector.Z, ZVector.Z));
        return rotation.WithPosition(rotation.TransformVector(Position) * -1);
    }

    public CFrame Orthonormalize()
    {
        const float tolerance = 0.000001f;
        var isOrthonormal = MathF.Abs(RightVector.Dot(RightVector) - 1) < tolerance
            && MathF.Abs(UpVector.Dot(UpVector) - 1) < tolerance
            && MathF.Abs(ZVector.Dot(ZVector) - 1) < tolerance
            && MathF.Abs(RightVector.Dot(UpVector)) < tolerance
            && MathF.Abs(RightVector.Dot(ZVector)) < tolerance
            && MathF.Abs(UpVector.Dot(ZVector)) < tolerance
            && RightVector.Cross(UpVector).Dot(ZVector) > 1 - tolerance;
        if (isOrthonormal) return this;
        var right = RightVector.Unit;
        var up = (UpVector - right * right.Dot(UpVector)).Unit;
        return new(Position, right, up, right.Cross(up).Unit);
    }

    public static CFrame operator *(CFrame left, CFrame right) => new(left.TransformPoint(right.Position),
        left.TransformVector(right.RightVector), left.TransformVector(right.UpVector), left.TransformVector(right.ZVector));
    public static Vector3 operator *(CFrame frame, Vector3 point) => frame.TransformPoint(point);
    public static CFrame operator +(CFrame frame, Vector3 offset) => frame.WithPosition(frame.Position + offset);
    public static CFrame operator -(CFrame frame, Vector3 offset) => frame.WithPosition(frame.Position - offset);

    public static CFrame FromComponents(float[] values)
    {
        if (values.Length != 12) throw new ArgumentException("Expected twelve CFrame components.", nameof(values));
        return new(new(values[0], values[1], values[2]), new(values[3], values[6], values[9]),
            new(values[4], values[7], values[10]), new(values[5], values[8], values[11]));
    }

    public float[] GetComponents() => [Position.X, Position.Y, Position.Z,
        RightVector.X, UpVector.X, ZVector.X, RightVector.Y, UpVector.Y, ZVector.Y, RightVector.Z, UpVector.Z, ZVector.Z];

    public static CFrame FromMatrix(Vector3 position, Vector3 right, Vector3 up, Vector3? back = null) =>
        new(position, right, up, back ?? right.Cross(up).Unit);

    public static CFrame FromEulerAnglesXYZ(float x, float y, float z) =>
        FromAxisAngle(new(1, 0, 0), x) * FromAxisAngle(new(0, 1, 0), y) * FromAxisAngle(new(0, 0, 1), z);

    public static CFrame FromEulerAnglesYXZ(float x, float y, float z) =>
        FromAxisAngle(new(0, 1, 0), y) * FromAxisAngle(new(1, 0, 0), x) * FromAxisAngle(new(0, 0, 1), z);

    public static CFrame FromAxisAngle(Vector3 axis, float angle)
    {
        var normalized = axis.Unit;
        var sine = MathF.Sin(angle / 2);
        return FromQuaternion(default, normalized.X * sine, normalized.Y * sine, normalized.Z * sine, MathF.Cos(angle / 2));
    }

    public static CFrame FromQuaternion(Vector3 position, float x, float y, float z, float w)
    {
        var magnitude = MathF.Sqrt(x * x + y * y + z * z + w * w);
        x /= magnitude;
        y /= magnitude;
        z /= magnitude;
        w /= magnitude;
        return new(position,
            new(1 - 2 * (y * y + z * z), 2 * (x * y + z * w), 2 * (x * z - y * w)),
            new(2 * (x * y - z * w), 1 - 2 * (x * x + z * z), 2 * (y * z + x * w)),
            new(2 * (x * z + y * w), 2 * (y * z - x * w), 1 - 2 * (x * x + y * y)));
    }

    public static CFrame LookAlong(Vector3 position, Vector3 direction, Vector3 up)
    {
        var back = direction.Unit * -1;
        var right = up.Cross(back);
        if (right.Magnitude == 0) right = new Vector3(1, 0, 0).Cross(back);
        right = right.Unit;
        return new(position, right, back.Cross(right), back);
    }

    public bool Equals(CFrame other) => Position == other.Position && RightVector == other.RightVector
        && UpVector == other.UpVector && ZVector == other.ZVector;
    public override bool Equals(object? other) => other is CFrame frame && Equals(frame);
    public override int GetHashCode() => HashCode.Combine(Position, RightVector, UpVector, ZVector);
    public static bool operator ==(CFrame left, CFrame right) => left.Equals(right);
    public static bool operator !=(CFrame left, CFrame right) => !left.Equals(right);
}
