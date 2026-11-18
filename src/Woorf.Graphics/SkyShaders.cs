namespace Woorf.Graphics;

public static class SkyShaders
{
    public const string Vertex = """
        #version 330 core
        out vec2 clipPosition;
        void main() {
            clipPosition = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2) * 2.0 - 1.0;
            gl_Position = vec4(clipPosition, 0.0, 1.0);
        }
        """;
    public const string Fragment = """
        #version 330 core
        in vec2 clipPosition;
        uniform mat4 inverseViewProjection;
        uniform mat4 inverseOrientation;
        uniform samplerCube skyTexture;
        out vec4 color;
        void main() {
            vec4 point = inverseViewProjection * vec4(clipPosition, 1.0, 1.0);
            vec3 direction = mat3(inverseOrientation) * point.xyz;
            color = vec4(texture(skyTexture, direction).rgb, 1.0);
        }
        """;
}

