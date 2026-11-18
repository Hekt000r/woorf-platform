using System.Numerics;
using System.Runtime.InteropServices;
using Woorf.Graphics.OpenGL;
using Woorf.Graphics.Platform;

namespace Woorf.Graphics;

public sealed partial class GuiRenderer : IDisposable
{
    private readonly OpenGlWindow window;
    private readonly Gl gl;
    private readonly ShaderProgram program;
    private readonly Dictionary<string, GuiTexture> textures = [];
    private readonly record struct GuiTexture(uint Handle, int Width, int Height);
    private uint vao, buffer, white;
    private const int VertexSize = 9;
    private readonly float[] vertices = new float[6 * VertexSize];
    private const string Vertex = """
        #version 330 core
        layout(location=0) in vec2 position;
        layout(location=1) in vec2 uv;
        layout(location=2) in vec4 color;
        layout(location=3) in float gradientTime;
        out vec2 texcoord; out vec4 tint; out float alongGradient;
        void main(){gl_Position=vec4(position,0,1); texcoord=uv; tint=color; alongGradient=gradientTime;}
        """;
    private const string Fragment = """
        #version 330 core
        in vec2 texcoord; in vec4 tint; in float alongGradient;
        uniform sampler2D image; uniform vec3 shape;
        uniform vec3 gradientInfo;
        uniform vec3 gradientColors[40];
        uniform vec3 gradientStops[40];
        out vec4 result;
        vec4 sampleGradient(){
          int count=int(gradientInfo.x);
          if(count==0) return vec4(1);
          vec4 previous=vec4(gradientColors[0],gradientStops[0].y);
          if(alongGradient<gradientStops[0].x) return previous;
          for(int i=1;i<count;i++){
            vec4 next=vec4(gradientColors[i],gradientStops[i].y);
            if(alongGradient<gradientStops[i].x)
              return mix(previous,next,(alongGradient-gradientStops[i-1].x)/(gradientStops[i].x-gradientStops[i-1].x));
            previous=next;
          }
          return previous;
        }
        void main(){
          vec2 p=abs((texcoord-0.5)*shape.xy)-(shape.xy*0.5-vec2(shape.z));
          float d=length(max(p,vec2(0)))+min(max(p.x,p.y),0.0)-shape.z;
          float coverage=shape.z>0.0 ? 1.0-smoothstep(-0.5,0.5,d):1.0;
          result=texture(image,texcoord)*tint*sampleGradient(); result.a*=coverage;
        }
        """;

    public GuiRenderer(OpenGlWindow window)
    {
        this.window = window; gl = new(window.GetProcedureAddress); program = new(gl, Vertex, Fragment);
        gl.GenVertexArrays(1, out vao); gl.BindVertexArray(vao);
        gl.GenBuffers(1, out buffer); gl.BindBuffer(0x8892, buffer);
        gl.BufferData(0x8892, vertices.Length * sizeof(float), 0, 0x88E8);
        gl.EnableVertexAttribArray(0); gl.VertexAttribPointer(0, 2, 0x1406, 0, VertexSize * sizeof(float), 0);
        gl.EnableVertexAttribArray(1); gl.VertexAttribPointer(1, 2, 0x1406, 0, VertexSize * sizeof(float), 8);
        gl.EnableVertexAttribArray(2); gl.VertexAttribPointer(2, 4, 0x1406, 0, VertexSize * sizeof(float), 16);
        gl.EnableVertexAttribArray(3); gl.VertexAttribPointer(3, 1, 0x1406, 0, VertexSize * sizeof(float), 32);
        white = Upload(new(1, 1, [255, 255, 255, 255])).Handle;
    }

    public void Draw(IEnumerable<GuiDrawItem> items)
    {
        var (width, height) = window.GetSize(); if (width <= 0 || height <= 0) return;
        Gl.glDisable(0x0B71); Gl.glDisable(0x0B44); Gl.glEnable(0x0BE2); Gl.glEnable(0x0C11);
        Gl.glBlendFunc(0x0302, 0x0303); program.Use(); gl.BindVertexArray(vao); gl.BindBuffer(0x8892, buffer);
        try
        {
            foreach (var item in items)
            {
                if (item.Color.W <= 0 || item.Bounds.Width <= 0 || item.Bounds.Height <= 0 || item.Clip.Width <= 0 || item.Clip.Height <= 0) continue;
                if (item.Kind is "Text" or "Image" && item.Content.Length == 0) continue;
                var texture = item.Kind == "Rectangle" ? new GuiTexture(white, 1, 1) : GetTexture(item);
                if (texture.Handle == 0) continue;
                Gl.glBindTexture(0x0DE1, texture.Handle);
                Gl.glTexParameteri(0x0DE1, 0x2802, item.ScaleType == "Tile" ? 0x2901 : 0x812F);
                Gl.glTexParameteri(0x0DE1, 0x2803, item.ScaleType == "Tile" ? 0x2901 : 0x812F);
                var left = Math.Clamp((int)MathF.Floor(item.Clip.X), 0, width); var top = Math.Clamp((int)MathF.Floor(item.Clip.Y), 0, height);
                var right = Math.Clamp((int)MathF.Ceiling(item.Clip.X + item.Clip.Width), left, width);
                var bottom = Math.Clamp((int)MathF.Ceiling(item.Clip.Y + item.Clip.Height), top, height);
                Gl.glScissor(left, height - bottom, right - left, bottom - top);
                var drawing = FitImage(item, texture);
                SetGradient(item.Gradient);
                FillVertices(drawing, texture, width, height); program.SetVector("shape", new(drawing.Bounds.Width, drawing.Bounds.Height, Math.Min(item.Radius, Math.Min(drawing.Bounds.Width, drawing.Bounds.Height) / 2)));
                var pinned = GCHandle.Alloc(vertices, GCHandleType.Pinned);
                try { gl.BufferSubData(0x8892, 0, vertices.Length * sizeof(float), pinned.AddrOfPinnedObject()); }
                finally { pinned.Free(); }
                Gl.glDrawArrays(0x0004, 0, 6);
            }
        }
        finally { Gl.glDisable(0x0C11); Gl.glDisable(0x0BE2); Gl.glEnable(0x0B71); Gl.glEnable(0x0B44); }
    }

    private void FillVertices(GuiDrawItem item, GuiTexture texture, int width, int height)
    {
        ReadOnlySpan<Vector2> corners = [new(0, 0), new(1, 0), new(1, 1), new(0, 0), new(1, 1), new(0, 1)];
        var angle = item.Rotation * MathF.PI / 180; var sine = MathF.Sin(angle); var cosine = MathF.Cos(angle);
        for (var index = 0; index < 6; index++)
        {
            var uv = corners[index]; var x = (uv.X - 0.5f) * item.Bounds.Width; var y = (uv.Y - 0.5f) * item.Bounds.Height;
            var px = item.Bounds.X + item.Bounds.Width / 2 + x * cosine - y * sine;
            var py = item.Bounds.Y + item.Bounds.Height / 2 + x * sine + y * cosine;
            var offset = index * VertexSize;
            vertices[offset] = px / width * 2 - 1; vertices[offset + 1] = 1 - py / height * 2;
            var sample = ImageUv(item, texture, uv);
            vertices[offset + 2] = sample.X; vertices[offset + 3] = sample.Y;
            vertices[offset + 4] = item.Color.X; vertices[offset + 5] = item.Color.Y; vertices[offset + 6] = item.Color.Z; vertices[offset + 7] = item.Color.W;
            vertices[offset + 8] = GradientTime(item.Gradient, new(item.Bounds.X + uv.X * item.Bounds.Width,
                item.Bounds.Y + uv.Y * item.Bounds.Height));
        }
    }

    private void SetGradient(GuiGradientFill? gradient)
    {
        program.SetVector("gradientInfo", new(gradient?.Stops.Length ?? 0, 0, 0));
        if (gradient is null) return;
        for (var index = 0; index < gradient.Stops.Length; index++)
        {
            var stop = gradient.Stops[index];
            program.SetVector($"gradientColors[{index}]", new(stop.Color.X, stop.Color.Y, stop.Color.Z));
            program.SetVector($"gradientStops[{index}]", new(stop.Time, stop.Color.W, 0));
        }
    }

    private static float GradientTime(GuiGradientFill? gradient, Vector2 position)
    {
        if (gradient is null) return 0;
        var direction = gradient.End - gradient.Start;
        var local = position - new Vector2(gradient.Bounds.X, gradient.Bounds.Y) - gradient.Start;
        return direction == Vector2.Zero ? 0 : Vector2.Dot(direction, local) / direction.LengthSquared();
    }

    private GuiTexture GetTexture(GuiDrawItem item)
    {
        var key = item.Kind == "Image" ? "Image:" + item.Content
            : $"Text:{item.Bounds.Width}:{item.Bounds.Height}:{item.TextSize}:{item.XAlignment}:{item.YAlignment}:{item.Wrapped}:{item.Font}:{item.Bold}:{item.Italic}:{item.Underline}:{item.Strikethrough}:{item.Content}";
        if (textures.TryGetValue(key, out var texture)) return texture;
        if (textures.Count >= 128) { foreach (var old in textures.Values) DeleteTexture(old.Handle); textures.Clear(); }
        var image = item.Kind == "Image" ? SkyImage.TryLoad(item.Content) : GuiTextImage.Create(item);
        texture = image is null ? default : Upload(image);
        textures.Add(key, texture);
        return texture;
    }

    private static GuiTexture Upload(SkyImage image)
    {
        Gl.glGenTextures(1, out var texture); Gl.glBindTexture(0x0DE1, texture);
        Gl.glTexParameteri(0x0DE1, 0x2801, 0x2601); Gl.glTexParameteri(0x0DE1, 0x2800, 0x2601);
        Gl.glTexParameteri(0x0DE1, 0x2802, 0x812F); Gl.glTexParameteri(0x0DE1, 0x2803, 0x812F);
        var pinned = GCHandle.Alloc(image.Pixels, GCHandleType.Pinned);
        try { Gl.glTexImage2D(0x0DE1, 0, 0x8058, image.Width, image.Height, 0, 0x1908, 0x1401, pinned.AddrOfPinnedObject()); }
        finally { pinned.Free(); }
        return new(texture, image.Width, image.Height);
    }

    private static void DeleteTexture(uint value) => Gl.glDeleteTextures(1, ref value);
    public void Dispose()
    {
        foreach (var texture in textures.Values) DeleteTexture(texture.Handle); textures.Clear();
        if (white != 0) { DeleteTexture(white); white = 0; }
        if (buffer != 0) gl.DeleteBuffers(1, ref buffer); if (vao != 0) gl.DeleteVertexArrays(1, ref vao);
        buffer = vao = 0; program.Dispose();
    }
}
