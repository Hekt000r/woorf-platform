namespace Woorf.Graphics.OpenGL;

internal static class SceneShaders
{
    internal const string Vertex = """
        #version 330 core
        layout(location = 0) in vec3 position;
        layout(location = 1) in vec3 normal;
        layout(location = 2) in mat4 model;
        layout(location = 6) in vec3 instanceColor;
        layout(location = 7) in mat3 normalTransform;
        uniform mat4 viewProjection;
        out vec3 worldNormal;
        out vec3 worldPosition;
        out vec3 surfaceColor;
        void main() {
            vec4 world = model * vec4(position, 1.0);
            worldPosition = world.xyz;
            worldNormal = normalTransform * normal;
            surfaceColor = instanceColor;
            gl_Position = viewProjection * world;
        }
        """;
    internal const string Fragment = """
        #version 330 core
        in vec3 worldNormal;
        in vec3 worldPosition;
        in vec3 surfaceColor;
        uniform vec3 eye;
        out vec4 fragment;
        void main() {
            vec3 n = normalize(worldNormal);
            vec3 light = normalize(vec3(-0.4, 0.85, 0.3));
            float diffuse = max(dot(n, light), 0.0);
            vec3 halfVector = normalize(light + normalize(eye - worldPosition));
            float specular = pow(max(dot(n, halfVector), 0.0), 40.0) * 0.15;
            vec3 lit = surfaceColor * (0.3 + 0.7 * diffuse) + vec3(specular);
            fragment = vec4(pow(lit, vec3(1.0 / 2.2)), 1.0);
        }
        """;
}
