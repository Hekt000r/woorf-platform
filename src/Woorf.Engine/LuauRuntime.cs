using Woorf.Engine.Projects;
using Woorf.Engine.Scripting;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Networking;
using GameDataModel = Woorf.Engine.DataModel.DataModel;

namespace Woorf.Engine;

public static class LuauRuntime
{
    public static int ExecuteProject(LoadedProject project, int clients)
    {
        if (clients is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(clients));
        using var host = new LocalNetworkHost(project, clients, new MonotonicClock());
        var status = host.Run();
        host.Dispose();
        return status != 0 || host.HasFailed ? 1 : 0;
    }
    public static int Execute(string source, string filePath) => Execute(source, filePath, new MonotonicClock());

    public static int Execute(string source, string filePath, IRuntimeClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        using var session = new LuauSession(new GameDataModel(), clock);
        var status = session.Execute(source, filePath);
        return session.Finish(status != 0 ? status : session.Drain(clock));
    }

    public static int ExecuteProject(LoadedProject project) => ExecuteProject(project, new MonotonicClock());

    public static int ExecuteProject(LoadedProject project, IRuntimeClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        using var session = new LuauSession(project.Game, clock);
        var status = session.StartScripts(project.Scripts);
        return session.Finish(status != 0 ? status : session.Drain(clock));
    }
}
