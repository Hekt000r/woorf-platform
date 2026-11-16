using System.Runtime.InteropServices;

namespace Woorf.Graphics.OpenGL;

internal sealed class GpuMesh : IDisposable
{
    private readonly Gl gl;
    private uint array, vertices, indices, instances;
    private readonly int count;
    internal GpuMesh(Gl gl, MeshData mesh)
    {
        this.gl = gl; count = mesh.Indices.Length;
        try
        {
            gl.GenVertexArrays(1, out array); gl.BindVertexArray(array);
            gl.GenBuffers(1, out vertices); Upload(0x8892, vertices, mesh.Vertices, mesh.Vertices.Length * sizeof(float));
            gl.GenBuffers(1, out indices); Upload(0x8893, indices, mesh.Indices, mesh.Indices.Length * sizeof(uint));
            gl.EnableVertexAttribArray(0); gl.VertexAttribPointer(0, 3, 0x1406, 0, 24, 0);
            gl.EnableVertexAttribArray(1); gl.VertexAttribPointer(1, 3, 0x1406, 0, 24, 12);
            gl.GenBuffers(1, out instances); gl.BindBuffer(0x8892, instances);
            for (uint index = 0; index < 4; index++) InstanceAttribute(2 + index, 4, (int)index * 16);
            for (uint index = 0; index < 3; index++) InstanceAttribute(7 + index, 3, 64 + (int)index * 12);
            InstanceAttribute(6, 3, 100);
            gl.BindVertexArray(0);
        }
        catch { Dispose(); throw; }
    }
    private void Upload(uint target, uint buffer, Array data, int bytes)
    {
        var pin = GCHandle.Alloc(data, GCHandleType.Pinned);
        try { gl.BindBuffer(target, buffer); gl.BufferData(target, bytes, pin.AddrOfPinnedObject(), 0x88E4); }
        finally { pin.Free(); }
    }
    private void InstanceAttribute(uint index, int components, int offset)
    {
        gl.EnableVertexAttribArray(index); gl.VertexAttribPointer(index, components, 0x1406, 0, InstanceBatch.FloatsPerInstance * 4, offset);
        gl.VertexAttribDivisor(index, 1);
    }
    internal void Draw(InstanceBatch batch)
    {
        var pin = GCHandle.Alloc(batch.Data, GCHandleType.Pinned);
        try
        {
            gl.BindVertexArray(array); gl.BindBuffer(0x8892, instances);
            gl.BufferData(0x8892, batch.Data.Length * sizeof(float), 0, 0x88E0);
            gl.BufferSubData(0x8892, 0, batch.Count * InstanceBatch.FloatsPerInstance * sizeof(float), pin.AddrOfPinnedObject());
            gl.DrawElementsInstanced(4, count, 0x1405, 0, batch.Count);
        }
        finally { pin.Free(); }
    }
    public void Dispose()
    {
        if (indices != 0) gl.DeleteBuffers(1, ref indices);
        if (instances != 0) gl.DeleteBuffers(1, ref instances);
        if (vertices != 0) gl.DeleteBuffers(1, ref vertices);
        if (array != 0) gl.DeleteVertexArrays(1, ref array);
        instances = indices = vertices = array = 0;
    }
}
