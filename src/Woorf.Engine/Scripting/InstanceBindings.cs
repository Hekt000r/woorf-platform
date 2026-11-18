using System.Runtime.InteropServices;
using System.Text;
using Woorf.Engine.DataModel;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed class InstanceBindings : IDisposable
{
    private readonly DataModel.DataModel game;
    private readonly SignalBindings signalBindings;
    private readonly NetworkBindings networkBindings;
    private readonly BackendBindings backendBindings;
    private readonly Dictionary<Instance, int> identifiers = [];
    private readonly Dictionary<int, Instance> instances = [];
    private static readonly HashSet<string> Methods =
    [
        "IsA", "GetChildren", "GetDescendants", "GetFullName", "FindFirstChild",
        "FindFirstChildOfClass", "FindFirstChildWhichIsA", "FindFirstAncestor",
        "FindFirstAncestorOfClass", "FindFirstAncestorWhichIsA", "IsDescendantOf",
        "IsAncestorOf", "Clone", "Destroy", "ClearAllChildren", "GetPropertyChangedSignal", "WaitForChild",
    ];

    internal InstanceBindings(DataModel.DataModel game, Woorf.Engine.Networking.RuntimeEndpoint? endpoint = null)
    {
        this.game = game;
        var codec = new LuaValueCodec(GetId, id => instances.GetValueOrDefault(id));
        signalBindings = new SignalBindings(codec);
        networkBindings = new NetworkBindings(endpoint, codec, signalBindings);
        backendBindings = new BackendBindings(game, codec, signalBindings);
    }

    internal void AttachRuntime(nint handle) { signalBindings.AttachRuntime(handle); backendBindings.AttachRuntime(handle); }
    public void Dispose() { backendBindings.Dispose(); signalBindings.Dispose(); }
    internal bool HasShutdown => backendBindings.HasShutdown;
    internal void BeginShutdown() => backendBindings.BeginShutdown();
    internal void AttachCharacters(Action<Player> load) => backendBindings.AttachCharacters(load);

    internal int GetId(Instance? instance)
    {
        if (instance is null) return 0;
        if (identifiers.TryGetValue(instance, out var id)) return id;
        id = identifiers.Count + 1;
        identifiers.Add(instance, id);
        instances.Add(id, instance);
        return id;
    }

    internal int Dispatch(nint state, string operation)
    {
        signalBindings.EnterDispatch();
        try
        {
            if (operation.StartsWith("font_", StringComparison.Ordinal))
                return backendBindings.CallFont(state, operation);
            if (operation.StartsWith("brickcolor_", StringComparison.Ordinal))
                return backendBindings.CallBrickColor(state, operation);
            if (operation.StartsWith("cframe_", StringComparison.Ordinal))
                return CFrameBindings.Dispatch(state, operation);
            if (operation.StartsWith("vector3_", StringComparison.Ordinal))
                return Vector3Bindings.Dispatch(state, operation);
            if (operation is "signal_get" or "signal_set" or "connection_get" or "connection_set"
                or "Connect" or "Once" or "Disconnect")
                return signalBindings.Dispatch(state, operation);
            if (operation == "new") return Create(state);
            if (operation == "module_source") return ReadModuleSource(state);
            var instance = ReadInstance(state, 1) ?? throw new InvalidOperationException("Expected Instance self.");
            return operation switch
            {
                "get" => ReadProperty(state, instance),
                "set" => WriteProperty(state, instance),
                "tostring" => PushString(state, instance.Name),
                _ => CallMethod(state, instance, operation),
            };
        }
        catch (Exception error)
        {
            PushString(state, error.Message);
            return -1;
        }
        finally { signalBindings.ExitDispatch(); }
    }

    private int Create(nint state)
    {
        var className = ReadString(state, 1);
        var parent = ReadInstance(state, 2);
        var instance = Instance.Create(className);
        instance.Parent = parent;
        return PushInstance(state, instance);
    }

    private int ReadModuleSource(nint state)
    {
        var module = ReadInstance(state, 1);
        if (module?.ClassName != "ModuleScript")
            throw new InvalidOperationException("require expects a ModuleScript instance.");
        PushString(state, module.ScriptSource);
        PushString(state, "@" + module.GetFullName());
        return 2;
    }

    private int ReadProperty(nint state, Instance instance)
    {
        var property = ReadString(state, 2);
        if (backendBindings.TryRead(state, instance, property, out var backendResult)) return backendResult;
        if (networkBindings.TryRead(state, instance, property, out var result)) return result;
        if (Instance.IsSignalName(property)) return signalBindings.PushSignal(state, instance.GetSignal(property));
        if (instance is Model && property is "GetBoundingBox" or "GetExtentsSize")
        {
            woorf_push_method(state, property);
            return 1;
        }
        if ((instance is BasePart || instance is Model) && (property is "GetPivot" or "PivotTo"))
        {
            woorf_push_method(state, property);
            return 1;
        }
        if (Methods.Contains(property) || instance == game && property is "GetService" or "getService" or "service" or "FindService" or "IsLoaded")
        {
            woorf_push_method(state, property);
            return 1;
        }
        return property switch
        {
            "PivotOffset" when instance is BasePart part => CFrameBindings.Push(state, part.PivotOffset),
            "WorldPivot" when instance is Model model => CFrameBindings.Push(state, model.WorldPivot),
            "PrimaryPart" when instance is Model model => PushInstance(state, model.PrimaryPart),
            "CFrame" when instance is BasePart part => CFrameBindings.Push(state, part.CFrame),
            "Position" when instance is BasePart part => Vector3Bindings.Push(state, part.Position),
            "Size" when instance is BasePart part => Vector3Bindings.Push(state, part.Size),
            "Anchored" when instance is BasePart part => PushBoolean(state, part.Anchored),
            "Name" or "ClassName" => PushString(state, property == "Name" ? instance.Name : instance.ClassName),
            "Parent" => PushInstance(state, instance.Parent),
            "Archivable" => PushBoolean(state, instance.Archivable),
            "Workspace" when instance == game => PushInstance(state, game.Workspace),
            _ => PushInstance(state, instance.FindFirstChild(property) ??
                throw new InvalidOperationException($"{property} is not a valid member of {instance.ClassName} '{instance.Name}'.")),
        };
    }

    private int WriteProperty(nint state, Instance instance)
    {
        var property = ReadString(state, 2);
        if (backendBindings.TryWrite(state, instance, property)) return 0;
        switch (property)
        {
            case "PivotOffset" when instance is BasePart part: part.PivotOffset = CFrameBindings.Read(state, 3); break;
            case "WorldPivot" when instance is Model model: model.WorldPivot = CFrameBindings.Read(state, 3); break;
            case "PrimaryPart" when instance is Model model:
                var primary = ReadInstance(state, 3);
                if (primary is not null && primary is not BasePart)
                    throw new InvalidOperationException("PrimaryPart must be a BasePart or nil.");
                model.PrimaryPart = (BasePart?)primary;
                break;
            case "CFrame" when instance is BasePart part: part.CFrame = CFrameBindings.Read(state, 3); break;
            case "Position" when instance is BasePart part: part.Position = Vector3Bindings.Read(state, 3); break;
            case "Size" when instance is BasePart part: part.Size = Vector3Bindings.Read(state, 3); break;
            case "Anchored" when instance is BasePart part: part.Anchored = ReadBoolean(state, 3); break;
            case "Name": instance.Name = ReadString(state, 3); break;
            case "Parent": instance.Parent = ReadInstance(state, 3); break;
            case "Archivable": instance.Archivable = ReadBoolean(state, 3); break;
            default: throw new InvalidOperationException($"Cannot assign member '{property}'.");
        }
        return 0;
    }

    private int CallMethod(nint state, Instance instance, string method)
    {
        if (backendBindings.TryCall(state, instance, method, out var backendResult)) return backendResult;
        if (networkBindings.TryCall(state, instance, method, out var result)) return result;
        switch (method)
        {
            case "GetBoundingBox" when instance is Model model:
                var bounds = model.GetBoundingBox();
                CFrameBindings.Push(state, bounds.Frame);
                Vector3Bindings.Push(state, bounds.Size);
                return 2;
            case "GetExtentsSize" when instance is Model model:
                return Vector3Bindings.Push(state, model.GetExtentsSize());
            case "GetPivot" when instance is BasePart part: return CFrameBindings.Push(state, part.GetPivot());
            case "GetPivot" when instance is Model model: return CFrameBindings.Push(state, model.GetPivot());
            case "PivotTo" when instance is BasePart part: part.PivotTo(CFrameBindings.Read(state, 2)); return 0;
            case "PivotTo" when instance is Model model: model.PivotTo(CFrameBindings.Read(state, 2)); return 0;
            case "GetPropertyChangedSignal":
                return signalBindings.PushSignal(state, instance.GetPropertyChangedSignal(ReadString(state, 2)));
            case "IsA": return PushBoolean(state, instance.IsA(ReadString(state, 2)));
            case "GetChildren": return PushList(state, instance.GetChildren());
            case "GetDescendants": return PushList(state, instance.GetDescendants());
            case "GetFullName": return PushString(state, instance.GetFullName());
            case "FindFirstChild": return PushInstance(state, instance.FindFirstChild(ReadString(state, 2), ReadBoolean(state, 3, true)));
            case "FindFirstChildOfClass":
            case "FindFirstChildWhichIsA":
            case "FindFirstAncestor":
            case "FindFirstAncestorOfClass":
            case "FindFirstAncestorWhichIsA": return Find(state, instance, method);
            case "IsDescendantOf": return PushBoolean(state, instance.IsDescendantOf(RequireInstance(state, 2)));
            case "IsAncestorOf": return PushBoolean(state, RequireInstance(state, 2).IsDescendantOf(instance));
            case "Clone": return PushInstance(state, instance.Clone());
            case "Destroy": instance.Destroy(); return 0;
            case "ClearAllChildren": instance.ClearAllChildren(); return 0;
            case "GetService" or "getService" or "service" when instance == game: return PushInstance(state, game.GetService(ReadString(state, 2)));
            case "FindService" when instance == game: return PushInstance(state, game.FindService(ReadString(state, 2)));
            case "IsLoaded" when instance == game: return PushBoolean(state, true);
            default: throw new InvalidOperationException($"Unsupported method '{method}'.");
        }
    }

    private int Find(nint state, Instance instance, string method)
    {
        var target = ReadString(state, 2);
        var isAncestor = method.StartsWith("FindFirstAncestor", StringComparison.Ordinal);
        var isRecursive = method == "FindFirstChildWhichIsA" && ReadBoolean(state, 3, true);
        var candidates = isAncestor ? instance.GetAncestors() :
            isRecursive ? instance.GetDescendants() : instance.GetChildren();
        var match = candidates.FirstOrDefault(candidate => method.EndsWith("WhichIsA", StringComparison.Ordinal)
            ? candidate.IsA(target) : method.EndsWith("OfClass", StringComparison.Ordinal)
                ? candidate.ClassName == target : candidate.Name == target);
        return PushInstance(state, match);
    }

    private Instance RequireInstance(nint state, int index) => ReadInstance(state, index)
        ?? throw new InvalidOperationException("Expected Instance argument.");

    private Instance? ReadInstance(nint state, int index)
    {
        if (woorf_kind(state, index) == 0) return null;
        if (woorf_kind(state, index) != 3 || !instances.TryGetValue(woorf_get_id(state, index), out var instance))
            throw new InvalidOperationException("Expected Instance or nil.");
        return instance;
    }

    private static string ReadString(nint state, int index)
    {
        if (woorf_kind(state, index) != 2) throw new InvalidOperationException("Expected string argument.");
        var pointer = woorf_get_string(state, index, out var length);
        return Marshal.PtrToStringUTF8(pointer, checked((int)length))!;
    }

    private static bool ReadBoolean(nint state, int index, bool isOptional = false)
    {
        if (isOptional && woorf_kind(state, index) == 0) return false;
        if (woorf_kind(state, index) != 1) throw new InvalidOperationException("Expected boolean argument.");
        return woorf_get_bool(state, index) != 0;
    }

    private int PushInstance(nint state, Instance? instance)
    {
        woorf_push_instance(state, GetId(instance));
        return 1;
    }

    private int PushList(nint state, IEnumerable<Instance> values)
    {
        var ids = values.Select(GetId).ToArray();
        woorf_push_list(state, ids, ids.Length);
        return 1;
    }

    private static int PushString(nint state, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        woorf_push_string(state, bytes, (nuint)bytes.Length);
        return 1;
    }

    private static int PushBoolean(nint state, bool value)
    {
        woorf_push_bool(state, value ? 1 : 0);
        return 1;
    }
}
