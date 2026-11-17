using Woorf.Engine.Networking;
using Woorf.Engine.Projects;

internal static class SocketCli
{
    internal static int? Run(string[] arguments)
    {
        if (!arguments.Contains("--listen") && !arguments.Contains("--connect")) return null;
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, args) => { args.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            if ((arguments.Length == 3 || arguments.Length == 4 && arguments[3] == "--characters") && arguments[1] == "--listen" && Directory.Exists(arguments[0]))
                return SocketTransport.Listen(ProjectLoader.Load(arguments[0]), arguments[2], cancellation.Token, shouldLoadCharacters: arguments.Length == 4);
            if (arguments[0] == "--connect" && (arguments.Length == 2 || arguments.Length == 4 && arguments[2] == "--identity-file"))
                return SocketTransport.Connect(arguments[1], cancellation.Token, identityFile: arguments.Length == 4 ? arguments[3] : null);
            Console.Error.WriteLine("Usage: woorf <project-directory> --listen <ip:port> | woorf --connect <host:port> [--identity-file <path>]");
            return 2;
        }
        finally { Console.CancelKeyPress -= handler; }
    }
}
