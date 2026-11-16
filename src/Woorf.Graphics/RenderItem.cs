using System.Numerics;

namespace Woorf.Graphics;

public enum MeshShape { Box, Sphere }

public readonly record struct RenderItem(MeshShape Shape, Matrix4x4 Transform, Vector3 Color);
