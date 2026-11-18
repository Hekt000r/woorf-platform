namespace Woorf.Graphics;

public static class ShaderProfile
{
    public static string ForWebGl(string source) => source.Replace("#version 330 core",
        "#version 300 es\nprecision highp float;\nprecision highp int;", StringComparison.Ordinal);

    public static object WebGlPrograms() => new
    {
        scene = new { vertex = ForWebGl(SceneShaders.Vertex), fragment = ForWebGl(SceneShaders.Fragment) },
        sky = new { vertex = ForWebGl(SkyShaders.Vertex), fragment = ForWebGl(SkyShaders.Fragment) },
        stride = InstanceBatch.FloatsPerInstance,
    };
}
