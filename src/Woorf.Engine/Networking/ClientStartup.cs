using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal static class ClientStartup
{
    internal static int Run(RuntimeEndpoint endpoint, DataModel.DataModel server)
    {
        var player = ((Players)endpoint.Game.GetService("Players")).LocalPlayer!;
        var scripts = Instance.CreateRuntime("PlayerScripts");
        scripts.Parent = player;
        var gui = Instance.CreateRuntime("PlayerGui");
        gui.Parent = player;
        CopyTemplates(server.FindService("StarterPlayer")?.FindFirstChild("StarterPlayerScripts"), scripts);
        CopyTemplates(server.FindService("StarterGui"), gui);
        foreach (var script in player.GetDescendants().Where(instance => instance.ClassName == "LocalScript").ToArray())
        {
            if (script.IsDestroyed) continue;
            if (endpoint.Session.Execute(script.ScriptSource, script.GetFullName(), script) != 0) return 1;
        }
        return 0;
    }

    private static void CopyTemplates(Instance? source, Instance target)
    {
        if (source is null) return;
        foreach (var child in source.GetChildren())
        {
            var clone = child.Clone();
            if (clone is not null) clone.Parent = target;
        }
    }
}
