using Woorf.Graphics;
using Woorf.Graphics.Platform;

internal static class CharacterWindowInput
{
    internal static void Update(this CharacterViewer viewer, PerspectiveCamera camera, OpenGlWindow window,
        float elapsed, bool isTextFocused = false, float wheel = 0)
    {
        float Axis(int positive, int negative) => isTextFocused ? 0
            : (window.IsKeyDown(positive) ? 1 : 0) - (window.IsKeyDown(negative) ? 1 : 0);
        var isRightDown = window.IsKeyDown(0x02);
        var motion = window.ReadCameraMotion(!isTextFocused && (isRightDown || viewer.IsFirstPerson), viewer.IsFirstPerson);
        if (!isTextFocused) viewer.ApplyPointer(motion.X, motion.Y, wheel, isRightDown);
        viewer.ApplyInput(camera, Axis('W', 'S'), Axis('D', 'A'), !isTextFocused && window.IsKeyDown(0x20),
            !isTextFocused && window.IsKeyDown('R'), Axis(0x25, 0x27), Axis(0x26, 0x28), elapsed);
    }
}
