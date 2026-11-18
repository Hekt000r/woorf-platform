using System.Text;
using Woorf.Engine;
using Woorf.Engine.Projects;

Console.OutputEncoding = new UTF8Encoding(false);
try
{
    if (args.FirstOrDefault() == "import-roblox")
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: woorf import-roblox <place.rbxl | place.rbxlx> <new-project-directory>");
            return 2;
        }
        var report = RobloxPlaceImporter.Import(args[1], args[2]);
        Console.WriteLine($"Imported {report.Instances} instances and {report.Scripts} scripts.");
        Console.WriteLine($"Compatibility report: {Path.Combine(args[2], "import-report.json")}");
        return 0;
    }
    var renderStatus = RenderCli.Run(args);
    if (renderStatus is int rendered) return rendered;
    var socketStatus = SocketCli.Run(args);
    if (socketStatus is int result) return result;
}
catch (Exception error) when (error is IOException or InvalidDataException or System.Net.Sockets.SocketException or ArgumentException
    or System.Text.Json.JsonException or FormatException or InvalidOperationException or OperationCanceledException
    or System.Xml.XmlException or UnauthorizedAccessException or OverflowException or NotSupportedException or System.ComponentModel.Win32Exception or DllNotFoundException or EntryPointNotFoundException)
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
