using System.Numerics;
using Woorf.Graphics;

static class GraphicsMathTests
{
    internal static int Run()
    {
        try
        {
            CheckMesh(MeshData.CreateBox(), "box", 0.5f);
            CheckMesh(MeshData.CreateSphere(), "sphere", 1);
            var camera = new PerspectiveCamera();
            camera.Position = Vector3.Zero;
            camera.Target = -Vector3.UnitZ;
            camera.NearPlane = 1f; camera.FarPlane = 11f;
            Matrix4x4 matrix = camera.GetViewProjection(1f);
            var near = Vector4.Transform(new Vector4(0, 0, -1, 1), matrix);
            var far = Vector4.Transform(new Vector4(0, 0, -11, 1), matrix);
            if (MathF.Abs(near.Z / near.W + 1) > 0.0001f || MathF.Abs(far.Z / far.W - 1) > 0.0001f)
                throw new Exception("Camera does not map near/far planes to OpenGL -1/+1 clip depth.");
            Console.WriteLine("PASS graphics meshes have outward normals and camera uses OpenGL clip depth");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL graphics math: " + error.Message); return 1; }
    }

    private static void CheckMesh(MeshData mesh, string name, float extent)
    {
        float[] vertices = mesh.Vertices; uint[] indices = mesh.Indices;
        if (vertices.Length == 0 || vertices.Length % 6 != 0 || indices.Length == 0 || indices.Length % 3 != 0)
            throw new Exception("Mesh has no complete indexed triangles.");
        for (var index = 0; index < vertices.Length; index += 6)
        {
            var position = new Vector3(vertices[index], vertices[index + 1], vertices[index + 2]);
            var normal = new Vector3(vertices[index + 3], vertices[index + 4], vertices[index + 5]);
            if (MathF.Abs(normal.Length() - 1) > 0.0001f || Vector3.Dot(position, normal) < extent - 0.0001f)
                throw new Exception(name + " has invalid positions or inward normals.");
        }
        for (var index = 0; index < indices.Length; index += 3)
        {
            Vector3 Read(uint vertex)
            {
                var offset = checked((int)vertex * 6);
                return new(vertices[offset], vertices[offset + 1], vertices[offset + 2]);
            }
            var a = Read(indices[index]); var b = Read(indices[index + 1]); var c = Read(indices[index + 2]);
            var cross = Vector3.Cross(b - a, c - a);
            if (cross.LengthSquared() < 1e-12f || Vector3.Dot(cross, a + b + c) <= 0)
                throw new Exception(name + " has degenerate or clockwise triangles.");
        }
    }
}
