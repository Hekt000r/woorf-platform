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
        endpoint.GuiStartup = new GuiStartupLifecycle(player, gui, server.FindService("StarterGui"));
        return endpoint.Session.StartClientScripts();
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
