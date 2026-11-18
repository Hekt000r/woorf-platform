using System.Numerics;
using System.Runtime.InteropServices;

namespace Woorf.Graphics;

public sealed class InstanceBatch
{
    public const int FloatsPerInstance = 28;
    private float[] data = new float[32 * FloatsPerInstance];
    private RenderItem[] previous = new RenderItem[32];
    private int previousCount;
    public int Count { get; private set; }
    public float[] Data => data;
    public void Begin() { previousCount = Count; Count = 0; }

    public void Append(RenderItem item)
    {
        if (Count == previous.Length)
        {
            Array.Resize(ref previous, previous.Length * 2);
            Array.Resize(ref data, previous.Length * FloatsPerInstance);
        }
        if (Count >= previousCount || previous[Count] != item)
        {
            var transform = item.Transform;
            if (!Matrix4x4.Invert(transform, out var inverse) || !float.IsFinite(transform.GetDeterminant()))
                throw new ArgumentException("Draw transforms must be finite and invertible.", nameof(item));
            var values = data.AsSpan(Count * FloatsPerInstance, FloatsPerInstance);
            MemoryMarshal.Write(MemoryMarshal.AsBytes(values[..16]), in transform);
            values[16] = inverse.M11; values[17] = inverse.M21; values[18] = inverse.M31;
            values[19] = inverse.M12; values[20] = inverse.M22; values[21] = inverse.M32;
            values[22] = inverse.M13; values[23] = inverse.M23; values[24] = inverse.M33;
            values[25] = item.Color.X; values[26] = item.Color.Y; values[27] = item.Color.Z;
            previous[Count] = item;
        }
        Count++;
    }
}
