using System.Numerics;

namespace Woorf.Graphics;

public sealed record MeshData(float[] Vertices, uint[] Indices)
{
    public static MeshData CreateBox()
    {
        var vertices = new List<float>(); var indices = new List<uint>();
        foreach (var normal in new[] { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ })
        {
            var right = MathF.Abs(normal.Y) > 0.5f ? Vector3.UnitX : Vector3.Normalize(Vector3.Cross(Vector3.UnitY, normal));
            var up = Vector3.Cross(normal, right);
            var start = (uint)(vertices.Count / 6);
            foreach (var corner in new[] { -right - up, right - up, right + up, -right + up })
                Append(vertices, (normal + corner) * 0.5f, normal);
            indices.AddRange([start, start + 1, start + 2, start, start + 2, start + 3]);
        }
        return new(vertices.ToArray(), indices.ToArray());
    }

    public static MeshData CreateSphere()
    {
        const int slices = 32, stacks = 16;
        var vertices = new List<float>(); var indices = new List<uint>();
        Append(vertices, Vector3.UnitY, Vector3.UnitY);
        for (var stack = 1; stack < stacks; stack++)
        {
            var phi = MathF.PI * stack / stacks;
            for (var slice = 0; slice < slices; slice++)
            {
                var theta = 2 * MathF.PI * slice / slices;
                var point = new Vector3(MathF.Sin(phi) * MathF.Cos(theta), MathF.Cos(phi), MathF.Sin(phi) * MathF.Sin(theta));
                Append(vertices, point, point);
            }
        }
        var bottom = (uint)(vertices.Count / 6);
        Append(vertices, -Vector3.UnitY, -Vector3.UnitY);
        for (uint slice = 0; slice < slices; slice++)
        {
            var next = (slice + 1) % slices;
            indices.AddRange([0, 1 + next, 1 + slice]);
            for (uint stack = 0; stack < stacks - 2; stack++)
            {
                var a = 1 + stack * slices + slice; var b = 1 + stack * slices + next;
                indices.AddRange([a, b, a + slices, b, b + slices, a + slices]);
            }
            const uint last = 1 + (stacks - 2) * slices;
            indices.AddRange([last + slice, last + next, bottom]);
        }
        return new(vertices.ToArray(), indices.ToArray());
    }

    private static void Append(List<float> vertices, Vector3 position, Vector3 normal) =>
        vertices.AddRange([position.X, position.Y, position.Z, normal.X, normal.Y, normal.Z]);
}
