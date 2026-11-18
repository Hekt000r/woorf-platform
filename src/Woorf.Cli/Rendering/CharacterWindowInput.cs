using Woorf.Engine.DataModel;
using Woorf.Graphics;
using Woorf.Graphics.Platform;

internal static class CharacterWindowInput
{
    internal static void Update(this CharacterViewer viewer, PerspectiveCamera camera, OpenGlWindow window,
        float elapsed, UserInputService keyboard, bool isTextFocused = false, float wheel = 0)
    {
        bool KeyDown(int key) => !isTextFocused && window.IsKeyDown(key) &&
            (key == 0x20 ? !keyboard.IsKeyConsumed("Space") : key is < 65 or > 90 || !keyboard.IsKeyConsumed(((char)key).ToString()));
        float Axis(int positive, int negative) => (KeyDown(positive) ? 1 : 0) - (KeyDown(negative) ? 1 : 0);
        var isRightDown = window.IsKeyDown(0x02);
        var motion = window.ReadCameraMotion(!isTextFocused && (isRightDown || viewer.IsFirstPerson), viewer.IsFirstPerson);
        if (!isTextFocused) viewer.ApplyPointer(motion.X, motion.Y, wheel, isRightDown);
        viewer.ApplyInput(camera, Axis('W', 'S'), Axis('D', 'A'), KeyDown(0x20),
            KeyDown('R'), Axis(0x25, 0x27), Axis(0x26, 0x28), elapsed);
    }
}
