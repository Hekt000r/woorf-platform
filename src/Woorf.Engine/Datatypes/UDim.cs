namespace Woorf.Engine.Datatypes;

public readonly record struct UDim(float Scale, int Offset)
{
    public float Resolve(float parentSize) => Scale * parentSize + Offset;
}

public readonly record struct UDim2(UDim X, UDim Y)
{
    public UDim2(float xScale, int xOffset, float yScale, int yOffset)
        : this(new(xScale, xOffset), new(yScale, yOffset)) { }
    public static UDim2 FromOffset(int x, int y) => new(0, x, 0, y);
    public static UDim2 FromScale(float x, float y) => new(x, 0, y, 0);
    public Vector2 Resolve(Vector2 parentSize) => new(X.Resolve(parentSize.X), Y.Resolve(parentSize.Y));
}
