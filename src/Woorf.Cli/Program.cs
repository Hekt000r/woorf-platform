using System.Text;
using Woorf.Engine;
using Woorf.Engine.Projects;

Console.OutputEncoding = new UTF8Encoding(false);
if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: woorf <file.luau | project-directory>");
    return 2;
}

try
{
    if (Directory.Exists(args[0]))
        return LuauRuntime.ExecuteProject(ProjectLoader.Load(args[0]));
    var source = File.ReadAllText(args[0]);
    return LuauRuntime.Execute(source, args[0]);
}
catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException
    or DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
