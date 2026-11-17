using Woorf.Engine.DataModel;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings : IDisposable
{
    private readonly DataModel.DataModel game;
    private readonly LuaValueCodec codec;
    private readonly SignalBindings signals;
    private readonly CollectionTags tags;
    private readonly List<int> shutdown = [];
    private nint handle;
    internal BackendBindings(DataModel.DataModel game, LuaValueCodec codec, SignalBindings signals)
    {
        this.game = game; this.codec = codec; this.signals = signals;
        tags = new CollectionTags(game); tags.Attach();
    }
    internal void AttachRuntime(nint runtime) => handle = runtime;
    private object? Read(nint state, int index) => codec.ReadValue(state, index);
    private string Text(nint state, int index) => Read(state, index) as string ?? throw new InvalidOperationException("Expected string.");
    private double Number(nint state, int index) => Read(state, index) is double value ? value : throw new InvalidOperationException("Expected number.");
    private Instance Require(nint state, int index) => Read(state, index) as Instance ?? throw new InvalidOperationException("Expected Instance.");
    private int Push(nint state, object? value) { codec.Push(state, value); return 1; }
    private int List(nint state, IEnumerable<object?> values) => Push(state, new LuaTableValue(values.Select((value, index) => new KeyValuePair<object, object?>((double)index + 1, value)).ToArray()));

    internal bool TryRead(nint state, Instance instance, string property, out int result)
    {
        result = 1;
        if (TryReadSky(state, instance, property)) return true;
        if (TryReadCharacter(state, instance, property)) return true;
        if (TryReadTouch(state, instance, property)) return true;
        if (PhysicsBindings.TryRead(state, instance, property, codec)) return true;
        if (instance is ScriptInstance script && property is "Enabled" or "Disabled" or "_RunContext")
        {
            Push(state, property == "_RunContext" ? (object)script.RunContext : property == "Enabled" ? script.Enabled : !script.Enabled);
            return true;
        }
        if (property == "AttributeChanged") { signals.PushSignal(state, instance.AttributeChanged); return true; }
        if (instance is ValueObject value && property == "Value") { Push(state, value.Value); return true; }
        if (instance is BindableEvent bindable && property == "Event") { signals.PushSignal(state, bindable.Event); return true; }
        if (instance is Humanoid humanoid)
        {
            if (property is "Health" or "MaxHealth") { Push(state, property == "Health" ? humanoid.Health : humanoid.MaxHealth); return true; }
            if (property is "Died" or "HealthChanged") { signals.PushSignal(state, property == "Died" ? humanoid.Died : humanoid.HealthChanged); return true; }
        }
        if (instance is RunService run && property == "Heartbeat") { signals.PushSignal(state, run.Heartbeat); return true; }
        if (instance is Player player && property is "Team" or "Neutral") { Push(state, property == "Team" ? player.Team : player.Neutral); return true; }
        if (IsMethod(instance, property)) { woorf_push_method(state, property); return true; }
        return false;
    }

    private bool IsMethod(Instance instance, string name) => name is "SetAttribute" or "GetAttribute" or "GetAttributes" or "GetAttributeChangedSignal" or "AddTag" or "RemoveTag" or "HasTag" or "GetTags"
        || instance is BindableEvent && name == "Fire"
        || instance is Humanoid && name is "TakeDamage" or "Move"
        || instance is Player && name == "LoadCharacter"
        || instance is BasePart && name == "GetTouchingParts"
        || instance is Players && name == "GetPlayerFromCharacter"
        || instance.ClassName == "CollectionService" && name is "AddTag" or "RemoveTag" or "HasTag" or "GetTags" or "GetTagged" or "GetAllTags" or "GetInstanceAddedSignal" or "GetInstanceRemovedSignal"
        || instance.ClassName == "HttpService" && name is "JSONEncode" or "JSONDecode"
        || instance.ClassName == "Debris" && name == "AddItem"
        || instance == game && name == "BindToClose"
        || instance.ClassName == "Teams" && name == "GetTeams"
        || instance.ClassName == "Team" && name == "GetPlayers"
        || instance == game.Workspace && name is "Raycast" or "_Raycast" or "Spherecast" or "_Spherecast"
        || instance.ClassName == "DataStoreService" && name == "GetDataStore"
        || instance.ClassName == "GlobalDataStore" && name is "GetAsync" or "SetAsync" or "RemoveAsync" or "IncrementAsync" or "UpdateAsync";

    internal bool TryWrite(nint state, Instance instance, string property)
    {
        if (TryWriteSky(state, instance, property)) return true;
        if (TryWriteCharacter(state, instance, property)) return true;
        if (TryWriteTouch(state, instance, property)) return true;
        if (PhysicsBindings.TryWrite(state, instance, property, codec)) return true;
        if (instance is ScriptInstance script && property is "Enabled" or "Disabled" or "_RunContext")
        {
            if (property == "_RunContext") script.RunContext = checked((int)Number(state, 3));
            else
            {
                if (Read(state, 3) is not bool enabled) throw new InvalidOperationException("Expected boolean.");
                script.Enabled = property == "Enabled" ? enabled : !enabled;
            }
            return true;
        }
        if (instance is ValueObject value && property == "Value") { value.Value = Read(state, 3); return true; }
        if (instance is Humanoid humanoid && property is "Health" or "MaxHealth")
        {
            if (property == "Health") humanoid.Health = Number(state, 3); else humanoid.MaxHealth = Number(state, 3);
            return true;
        }
        if (instance is Player player && property == "Team")
        {
            var team = Read(state, 3) as Instance;
            if (team is not null && team.ClassName != "Team") throw new InvalidOperationException("Expected Team.");
            player.Team = team; return true;
        }
        return false;
    }

    internal bool HasShutdown => shutdown.Count > 0;
    internal void BeginShutdown()
    {
        foreach (var reference in shutdown) codec.Invoke(handle, reference, []);
        foreach (var reference in shutdown) woorf_unref_callback(handle, reference);
        shutdown.Clear();
    }

    public void Dispose() { tags.Dispose(); foreach (var reference in shutdown) woorf_unref_callback(handle, reference); shutdown.Clear(); }
}
