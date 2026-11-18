using Woorf.Engine.DataModel;
using Woorf.Graphics.Platform;

internal static class GamepadWindowInput
{
    private static readonly (string Name, int Mask)[] Buttons =
    [
        ("DPadUp", 0x0001), ("DPadDown", 0x0002), ("DPadLeft", 0x0004), ("DPadRight", 0x0008),
        ("ButtonStart", 0x0010), ("ButtonSelect", 0x0020), ("ButtonL3", 0x0040), ("ButtonR3", 0x0080),
        ("ButtonL1", 0x0100), ("ButtonR1", 0x0200), ("ButtonA", 0x1000), ("ButtonB", 0x2000),
        ("ButtonX", 0x4000), ("ButtonY", 0x8000)
    ];

    internal static void Update(OpenGlWindow window, UserInputService input, bool isProcessed)
    {
        var (buttons, leftTrigger, rightTrigger) = window.ReadGamepad();
        foreach (var (name, mask) in Buttons) input.SetInput("Gamepad1", name, (buttons & mask) != 0, isProcessed);
        input.SetInput("Gamepad1", "ButtonL2", leftTrigger > 30, isProcessed);
        input.SetInput("Gamepad1", "ButtonR2", rightTrigger > 30, isProcessed);
    }
}
