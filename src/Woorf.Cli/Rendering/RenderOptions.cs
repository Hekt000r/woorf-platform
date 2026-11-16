internal sealed record RenderOptions(string Source, bool IsVisible, int Frames, string? Capture, int Width, int Height, string? Profile)
{
    internal static RenderOptions Parse(string[] arguments)
    {
        const string usage = "Usage: woorf <file.luau | project-directory> --render [--width N --height N] [--frames N --capture image.bmp] [--profile report.csv] [--hidden]";
        if (arguments.Length < 2 || arguments[1] != "--render") throw new ArgumentException(usage);
        var isVisible = true; var frames = 0; string? capture = null;
        var width = 960; var height = 640; string? profile = null;
        for (var index = 2; index < arguments.Length; index++)
        {
            var option = arguments[index];
            if (option == "--hidden") { isVisible = false; continue; }
            if (++index >= arguments.Length) throw new ArgumentException(usage);
            var value = arguments[index];
            if (option == "--capture") { capture = value; continue; }
            if (option == "--profile") { profile = value; continue; }
            if (!int.TryParse(value, out var number) || number < 1) throw new ArgumentException(usage);
            switch (option)
            {
                case "--frames": frames = number; break;
                case "--width" when number <= 8192: width = number; break;
                case "--height" when number <= 8192: height = number; break;
                default: throw new ArgumentException(usage);
            }
        }
        if ((!isVisible || capture is not null) && frames == 0) throw new ArgumentException("--hidden and --capture require --frames N. " + usage);
        return new(arguments[0], isVisible, frames, capture, width, height, profile);
    }
}
