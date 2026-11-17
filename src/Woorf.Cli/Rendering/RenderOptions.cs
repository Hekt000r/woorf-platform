internal sealed record RenderOptions(string Source, bool IsVisible, int Frames, string? Capture, int Width, int Height, string? Profile, bool IsPlayable,
    string? ConnectAddress = null, int Clients = 1, int WindowSlot = -1, string? IdentityFile = null)
{
    internal static RenderOptions Parse(string[] arguments)
    {
        const string usage = "Usage: woorf <file.luau | project-directory> --render | <project-directory> --play [--clients 1..4] | --connect <ip:port> --play [--identity-file path]; viewer options: [--width N --height N] [--frames N --capture image.bmp] [--profile report.csv] [--hidden]";
        var connectAddress = arguments.Length >= 3 && arguments[0] == "--connect" ? arguments[1] : null;
        if (connectAddress is not null) arguments = [arguments[1], .. arguments.Skip(2)];
        if (arguments.Length < 2 || arguments[1] is not ("--render" or "--play")) throw new ArgumentException(usage);
        var isPlayable = arguments[1] == "--play";
        if (connectAddress is not null && !isPlayable) throw new ArgumentException("--connect requires --play for graphical clients.");
        var isVisible = true; var frames = 0; string? capture = null;
        var width = 960; var height = 640; string? profile = null;
        var clients = 1; var windowSlot = -1; string? identityFile = null;
        for (var index = 2; index < arguments.Length; index++)
        {
            var option = arguments[index];
            if (option == "--hidden") { isVisible = false; continue; }
            if (option == "--play") { isPlayable = true; continue; }
            if (++index >= arguments.Length) throw new ArgumentException(usage);
            var value = arguments[index];
            if (option == "--capture") { capture = value; continue; }
            if (option == "--profile") { profile = value; continue; }
            if (option == "--identity-file") { identityFile = value; continue; }
            if (!int.TryParse(value, out var number) || number < 0) throw new ArgumentException(usage);
            switch (option)
            {
                case "--frames" when number > 0: frames = number; break;
                case "--width" when number is > 0 and <= 8192: width = number; break;
                case "--height" when number is > 0 and <= 8192: height = number; break;
                case "--clients" when number is >= 1 and <= 4: clients = number; break;
                case "--window-slot" when number <= 3: windowSlot = number; break;
                default: throw new ArgumentException(usage);
            }
        }
        if ((!isVisible || capture is not null) && frames == 0) throw new ArgumentException("--hidden and --capture require --frames N. " + usage);
        if (clients > 1 && (!isPlayable || connectAddress is not null)) throw new ArgumentException("--clients requires a local project with --play.");
        return new(arguments[0], isVisible, frames, capture, width, height, profile, isPlayable, connectAddress, clients, windowSlot, identityFile);
    }
}
