using System.Runtime.InteropServices;
using System.Text;

namespace Woorf.Graphics.OpenGL;

internal sealed class Gl
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void Gen(int count, out uint value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void Delete(int count, ref uint value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void Bind(uint target, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void BindArray(uint value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void Buffer(uint target, nint size, nint data, uint usage);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void Attribute(uint index, int size, uint type, byte normalized, int stride, nint offset);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void EnableAttribute(uint index);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate uint CreateShaderDelegate(uint type);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void ShaderSourceDelegate(uint shader, int count, nint strings, nint lengths);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void ObjectAction(uint value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void GetObject(uint value, uint parameter, out int result);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void InfoLog(uint value, int capacity, out int length, StringBuilder log);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate uint CreateProgramDelegate();
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void Attach(uint program, uint shader);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate int UniformLocation(uint program, [MarshalAs(UnmanagedType.LPStr)] string name);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void Matrix(int location, int count, byte transpose, float[] values);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void Vector(int location, float x, float y, float z);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void Divisor(uint index, uint divisor);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void DrawInstanced(uint mode, int count, uint type, nint offset, int instances);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate void SubBuffer(uint target, nint offset, nint size, nint data);

    internal readonly Gen GenVertexArrays, GenBuffers;
    internal readonly Delete DeleteVertexArrays, DeleteBuffers;
    internal readonly BindArray BindVertexArray;
    internal readonly Bind BindBuffer;
    internal readonly Buffer BufferData;
    internal readonly Attribute VertexAttribPointer;
    internal readonly EnableAttribute EnableVertexAttribArray;
    internal readonly CreateShaderDelegate CreateShader;
    internal readonly ShaderSourceDelegate ShaderSource;
    internal readonly ObjectAction CompileShader, DeleteShader, LinkProgram, UseProgram, DeleteProgram;
    internal readonly GetObject GetShaderiv, GetProgramiv;
    internal readonly InfoLog GetShaderInfoLog, GetProgramInfoLog;
    internal readonly CreateProgramDelegate CreateProgram;
    internal readonly Attach AttachShader;
    internal readonly UniformLocation GetUniformLocation;
    internal readonly Matrix UniformMatrix4fv;
    internal readonly Vector Uniform3f;
    internal readonly Divisor VertexAttribDivisor;
    internal readonly DrawInstanced DrawElementsInstanced;
    internal readonly SubBuffer BufferSubData;

    internal Gl(Func<string, nint> resolve)
    {
        T Load<T>(string name) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(resolve("gl" + name));
        GenVertexArrays = Load<Gen>("GenVertexArrays"); GenBuffers = Load<Gen>("GenBuffers");
        DeleteVertexArrays = Load<Delete>("DeleteVertexArrays"); DeleteBuffers = Load<Delete>("DeleteBuffers");
        BindVertexArray = Load<BindArray>("BindVertexArray"); BindBuffer = Load<Bind>("BindBuffer");
        BufferData = Load<Buffer>("BufferData"); VertexAttribPointer = Load<Attribute>("VertexAttribPointer");
        EnableVertexAttribArray = Load<EnableAttribute>("EnableVertexAttribArray");
        CreateShader = Load<CreateShaderDelegate>("CreateShader"); ShaderSource = Load<ShaderSourceDelegate>("ShaderSource");
        CompileShader = Load<ObjectAction>("CompileShader"); DeleteShader = Load<ObjectAction>("DeleteShader");
        GetShaderiv = Load<GetObject>("GetShaderiv"); GetShaderInfoLog = Load<InfoLog>("GetShaderInfoLog");
        CreateProgram = Load<CreateProgramDelegate>("CreateProgram"); AttachShader = Load<Attach>("AttachShader");
        LinkProgram = Load<ObjectAction>("LinkProgram"); UseProgram = Load<ObjectAction>("UseProgram");
        DeleteProgram = Load<ObjectAction>("DeleteProgram"); GetProgramiv = Load<GetObject>("GetProgramiv");
        GetProgramInfoLog = Load<InfoLog>("GetProgramInfoLog"); GetUniformLocation = Load<UniformLocation>("GetUniformLocation");
        UniformMatrix4fv = Load<Matrix>("UniformMatrix4fv"); Uniform3f = Load<Vector>("Uniform3f");
        VertexAttribDivisor = Load<Divisor>("VertexAttribDivisor"); DrawElementsInstanced = Load<DrawInstanced>("DrawElementsInstanced");
        BufferSubData = Load<SubBuffer>("BufferSubData");
    }

    [DllImport("opengl32")] internal static extern void glViewport(int x, int y, int width, int height);
    [DllImport("opengl32")] internal static extern void glBlendFunc(uint source, uint destination);
    [DllImport("opengl32")] internal static extern void glScissor(int x, int y, int width, int height);
    [DllImport("opengl32")] internal static extern void glDisable(uint capability);
    [DllImport("opengl32")] internal static extern void glDrawArrays(uint mode, int first, int count);
    [DllImport("opengl32")] internal static extern void glGenTextures(int count, out uint texture);
    [DllImport("opengl32")] internal static extern void glDeleteTextures(int count, ref uint texture);
    [DllImport("opengl32")] internal static extern void glBindTexture(uint target, uint texture);
    [DllImport("opengl32")] internal static extern void glTexParameteri(uint target, uint parameter, int value);
    [DllImport("opengl32")] internal static extern void glTexImage2D(uint target, int level, int format, int width, int height, int border, uint sourceFormat, uint type, nint pixels);
    [DllImport("opengl32")] internal static extern void glClearColor(float r, float g, float b, float a);
    [DllImport("opengl32")] internal static extern void glClear(uint mask);
    [DllImport("opengl32")] internal static extern void glEnable(uint capability);
    [DllImport("opengl32")] internal static extern void glDrawElements(uint mode, int count, uint type, nint offset);
    [DllImport("opengl32")] internal static extern uint glGetError();
    [DllImport("opengl32")] internal static extern nint glGetString(uint name);
    [DllImport("opengl32")] internal static extern void glPixelStorei(uint parameter, int value);
    [DllImport("opengl32")] internal static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, byte[] pixels);
    [DllImport("opengl32")] internal static extern void glReadBuffer(uint buffer);
}
