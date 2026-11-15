using Woorf.Engine.Projects;
using Woorf.Engine.Scripting;
using Woorf.Engine.Scheduling;
using GameDataModel = Woorf.Engine.DataModel.DataModel;

namespace Woorf.Engine;

public static class LuauRuntime
{
    public static int Execute(string source, string filePath) => Execute(source, filePath, new MonotonicClock());

    public static int Execute(string source, string filePath, IRuntimeClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        using var session = new LuauSession(new GameDataModel(), clock);
        var status = session.Execute(source, filePath);
        return status != 0 ? status : session.Drain(clock);
    }

    public static int ExecuteProject(LoadedProject project) => ExecuteProject(project, new MonotonicClock());

    public static int ExecuteProject(LoadedProject project, IRuntimeClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        using var session = new LuauSession(project.Game, clock);
        foreach (var script in project.Scripts)
        {
            if (script.Instance.ClassName != "Script" || script.Instance.IsDestroyed) continue;
            var canRun = script.Instance.GetAncestors()
                .Any(ancestor => ancestor.ClassName is "Workspace" or "ServerScriptService");
            if (!canRun) continue;
            var status = session.Execute(script.Source, script.FilePath, script.Instance);
            if (status != 0) return status;
        }
        return session.Drain(clock);
    }
}
