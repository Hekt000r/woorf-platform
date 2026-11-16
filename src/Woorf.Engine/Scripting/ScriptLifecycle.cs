using Woorf.Engine.DataModel;
using Woorf.Engine.Projects;

namespace Woorf.Engine.Scripting;

internal sealed class ScriptLifecycle : IDisposable
{
    private readonly DataModel.DataModel game;
    private readonly LuauSession session;
    private readonly bool isClient;
    private readonly Dictionary<ScriptInstance, bool> active = [];
    private readonly HashSet<ScriptInstance> stopping = [];
    private readonly Dictionary<Instance, string> paths = [];
    private bool isDirty = true;
    internal bool HasPending => isDirty;

    internal ScriptLifecycle(DataModel.DataModel game, LuauSession session, bool isClient)
    {
        this.game = game; this.session = session; this.isClient = isClient;
        game.DescendantMutated += OnMutation;
    }

    private void OnMutation(Instance instance, string property)
    {
        if (property is not ("Parent" or "Enabled" or "RunContext")) return;
        isDirty = true;
        foreach (var script in instance.GetDescendants().Prepend(instance).OfType<ScriptInstance>())
            if (active.GetValueOrDefault(script) && (property == "RunContext" || !CanRun(script))) stopping.Add(script);
    }

    private bool CanRun(ScriptInstance script)
    {
        if (!script.Enabled || script.IsDestroyed || !script.IsDescendantOf(game)) return false;
        if (script.ClassName == "LocalScript" || script.RunContext == 2)
        {
            if (!isClient) return false;
            var player = ((Players)game.GetService("Players")).LocalPlayer;
            if (player is not null && script.IsDescendantOf(player)) return true;
            return script.RunContext == 2 && script.GetAncestors().Any(parent => parent.ClassName is "Workspace" or "ReplicatedStorage" or "ReplicatedFirst");
        }
        if (isClient) return false;
        return script.GetAncestors().Any(parent => parent.ClassName is "Workspace" or "ServerScriptService"
            || script.RunContext == 1 && parent.ClassName == "ReplicatedStorage");
    }

    internal int Start(IEnumerable<ProjectScript> scripts)
    {
        foreach (var script in scripts) paths[script.Instance] = script.FilePath;
        return Synchronize();
    }

    internal int Synchronize()
    {
        if (!isDirty) return 0;
        isDirty = false;
        foreach (var script in active.Where(entry => entry.Value && (!CanRun(entry.Key) || stopping.Contains(entry.Key))).Select(entry => entry.Key).ToArray())
        {
            active[script] = false;
            session.StopScript(script);
        }
        stopping.Clear();
        var scripts = game.GetDescendants().OfType<ScriptInstance>()
            .OrderBy(script => paths.GetValueOrDefault(script, script.GetFullName()), StringComparer.Ordinal).ToArray();
        foreach (var script in scripts)
        {
            if (active.GetValueOrDefault(script) || !CanRun(script)) continue;
            active[script] = true;
            if (session.Execute(script.ScriptSource, paths.GetValueOrDefault(script, script.GetFullName()), script) != 0) return 1;
        }
        foreach (var script in active.Keys.Where(script => script.IsDestroyed).ToArray()) active.Remove(script);
        return 0;
    }

    public void Dispose() => game.DescendantMutated -= OnMutation;
}
