using Woorf.Engine.DataModel;
using Woorf.Engine.Networking;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;

internal static class SharedGlobalsTests
{
    internal static int Run()
    {
        var game = new DataModel();
        var server = game.GetService("ServerScriptService");
        var starter = Instance.CreateRuntime("StarterPlayerScripts");
        starter.Parent = game.GetService("StarterPlayer");
        var scripts = new[]
        {
            Script(server, "A", """
                _G.ServerValue = 42
                shared.ServerValue = 43
                OrdinaryGlobal = true
                assert(not pcall(function() math.abs = false end))
                """),
            Script(server, "B", """
                assert(_G.ServerValue == 42 and shared.ServerValue == 43)
                assert(OrdinaryGlobal == nil)
                """),
            Script(starter, "A", """
                assert(_G.ServerValue == nil and shared.ServerValue == nil)
                assert(_G.ClientValue == nil and shared.ClientValue == nil)
                _G.ClientValue = 11
                shared.ClientValue = 12
                """),
            Script(starter, "B", """
                assert(_G.ClientValue == 11 and shared.ClientValue == 12)
                """)
        };
        using var host = new LocalNetworkHost(new(game, scripts), new MonotonicClock());
        if (host.Start() != 0) return 1;
        host.ConnectClient();
        host.ConnectClient();
        if (host.Pump() != 0) return 1;
        Console.WriteLine("PASS _G and shared are writable per runtime with isolated script environments");
        return 0;
    }

    private static ProjectScript Script(Instance parent, string name, string source)
    {
        var script = Instance.Create(parent.ClassName == "StarterPlayerScripts" ? "LocalScript" : "Script");
        script.Name = name;
        script.Parent = parent;
        script.ScriptSource = source;
        return new(script, source, name + ".luau");
    }
}
