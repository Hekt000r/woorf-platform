using Woorf.Graphics;
using Woorf.Graphics.Platform;

internal static class CharacterWindowInput
{
    internal static void Update(this CharacterViewer viewer, PerspectiveCamera camera, OpenGlWindow window,
        float elapsed, bool isTextFocused = false)
    {
        float Axis(int positive, int negative) => isTextFocused ? 0
            : (window.IsKeyDown(positive) ? 1 : 0) - (window.IsKeyDown(negative) ? 1 : 0);
        viewer.ApplyInput(camera, Axis('W', 'S'), Axis('D', 'A'), !isTextFocused && window.IsKeyDown(0x20),
            !isTextFocused && window.IsKeyDown('R'), Axis(0x25, 0x27), Axis(0x26, 0x28), elapsed);
    }
}
