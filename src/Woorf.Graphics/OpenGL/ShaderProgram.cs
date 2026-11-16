using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Woorf.Graphics.OpenGL;

internal sealed class ShaderProgram : IDisposable
{
    private readonly Gl gl;
    private uint program;
    private readonly Dictionary<string, int> uniforms = [];
    private readonly float[] matrixValues = new float[16];
    internal ShaderProgram(Gl gl, string vertex, string fragment)
    {
        this.gl = gl;
        uint vs = 0, fs = 0;
        try
        {
            vs = Compile(0x8B31, vertex); fs = Compile(0x8B30, fragment);
            program = gl.CreateProgram(); gl.AttachShader(program, vs); gl.AttachShader(program, fs); gl.LinkProgram(program);
            gl.GetProgramiv(program, 0x8B82, out var isLinked);
            if (isLinked == 0)
            {
                var log = new StringBuilder(4096); gl.GetProgramInfoLog(program, log.Capacity, out _, log);
                throw new InvalidOperationException("OpenGL shader link failed: " + log);
            }
        }
        catch { Dispose(); throw; }
        finally { if (vs != 0) gl.DeleteShader(vs); if (fs != 0) gl.DeleteShader(fs); }
    }
    private uint Compile(uint type, string source)
    {
        var shader = gl.CreateShader(type);
        var text = Marshal.StringToCoTaskMemUTF8(source);
        var pointer = Marshal.AllocHGlobal(nint.Size);
        try
        {
            Marshal.WriteIntPtr(pointer, text); gl.ShaderSource(shader, 1, pointer, 0); gl.CompileShader(shader);
            gl.GetShaderiv(shader, 0x8B81, out var isCompiled);
            if (isCompiled != 0) return shader;
            var log = new StringBuilder(4096); gl.GetShaderInfoLog(shader, log.Capacity, out _, log);
            throw new InvalidOperationException("OpenGL shader compilation failed: " + log);
        }
        catch { gl.DeleteShader(shader); throw; }
        finally { Marshal.FreeHGlobal(pointer); Marshal.FreeCoTaskMem(text); }
    }
    internal void Use() => gl.UseProgram(program);
    internal void SetMatrix(string name, Matrix4x4 matrix)
    {
        MemoryMarshal.Write(MemoryMarshal.AsBytes(matrixValues.AsSpan()), in matrix);
        gl.UniformMatrix4fv(GetUniform(name), 1, 0, matrixValues);
    }
    private int GetUniform(string name)
    {
        if (!uniforms.TryGetValue(name, out var location)) uniforms[name] = location = gl.GetUniformLocation(program, name);
        return location;
    }
    internal void SetVector(string name, Vector3 value) => gl.Uniform3f(GetUniform(name), value.X, value.Y, value.Z);
    public void Dispose() { if (program != 0) gl.DeleteProgram(program); program = 0; }
}
