using System.Text;
using Woorf.Engine;
using Woorf.Engine.Projects;

Console.OutputEncoding = new UTF8Encoding(false);
try
{
    var socketStatus = SocketCli.Run(args);
    if (socketStatus is int result) return result;
}
catch (Exception error) when (error is IOException or InvalidDataException or System.Net.Sockets.SocketException or ArgumentException
    or System.Text.Json.JsonException or FormatException or InvalidOperationException or OperationCanceledException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
var clientCount = 0;
if (args.Length != 1 && (args.Length != 3 || args[1] != "--clients"
    || !int.TryParse(args[2], out clientCount) || clientCount is < 1 or > 32 || !Directory.Exists(args[0])))
{
    Console.Error.WriteLine("Usage: woorf <file.luau | project-directory> [--clients 1..32]");
    return 2;
}

try
{
    if (Directory.Exists(args[0]))
    {
        var project = ProjectLoader.Load(args[0]);
        return clientCount == 0 ? LuauRuntime.ExecuteProject(project) : LuauRuntime.ExecuteProject(project, clientCount);
    }
    var source = File.ReadAllText(args[0]);
    return LuauRuntime.Execute(source, args[0]);
}
catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException
    or DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
