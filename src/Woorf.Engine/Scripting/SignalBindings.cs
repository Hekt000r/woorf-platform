using System.Runtime.InteropServices;
using System.Text;
using Woorf.Engine.DataModel;
using Woorf.Engine.Events;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed class SignalBindings(LuaValueCodec codec) : IDisposable
{
    private readonly Dictionary<ScriptSignal, int> signalIds = [];
    private readonly Dictionary<int, ScriptSignal> signals = [];
    private readonly Dictionary<int, ScriptConnection> connections = [];
    private readonly HashSet<int> callbackReferences = [];
    private readonly HashSet<int> pendingReleases = [];
    private nint handle;
    private int dispatchDepth;

    internal void AttachRuntime(nint runtime) => handle = runtime;
    internal void EnterDispatch() => dispatchDepth++;

    internal void ExitDispatch()
    {
        if (--dispatchDepth == 0) ReleaseCallbacks();
    }

    internal int PushSignal(nint state, ScriptSignal signal)
    {
        if (!signalIds.TryGetValue(signal, out var id))
        {
            id = signalIds.Count + 1;
            signalIds.Add(signal, id);
            signals.Add(id, signal);
        }
        woorf_push_signal(state, id);
        return 1;
    }

    internal int Dispatch(nint state, string operation)
    {
        switch (operation)
        {
            case "signal_get":
                ReadSignal(state);
                var property = ReadString(state, 2);
                if (property is not ("Connect" or "Once" or "Wait"))
                    throw new InvalidOperationException($"Unsupported RBXScriptSignal member '{property}'.");
                woorf_push_method(state, property);
                return 1;
            case "connection_get":
                var connection = ReadConnection(state);
                var member = ReadString(state, 2);
                if (member == "Connected")
                {
                    woorf_push_bool(state, connection.Connected ? 1 : 0);
                    return 1;
                }
                if (member != "Disconnect")
                    throw new InvalidOperationException($"Unsupported RBXScriptConnection member '{member}'.");
                woorf_push_method(state, "Disconnect");
                return 1;
            case "Connect": return Connect(state, false);
            case "Once": return Connect(state, true);
            case "Disconnect": ReadConnection(state).Disconnect(); return 0;
            default: throw new InvalidOperationException("Signal and connection members are read-only.");
        }
    }

    private int Connect(nint state, bool isOnce)
    {
        var signal = ReadSignal(state);
        if (woorf_kind(state, 2) != 7)
            throw new InvalidOperationException("Expected function callback.");
        var reference = woorf_ref_function(state, 2);
        callbackReferences.Add(reference);
        var connection = signal.Connect(arguments => Invoke(reference, arguments), isOnce);
        connection.Disconnected += () => pendingReleases.Add(reference);
        if (!connection.Connected) pendingReleases.Add(reference);
        var id = connections.Count + 1;
        connections.Add(id, connection);
        woorf_push_connection(state, id);
        return 1;
    }

    private void Invoke(int reference, object?[] arguments)
    {
        codec.Invoke(handle, reference, arguments);
    }

    private ScriptSignal ReadSignal(nint state)
    {
        if (woorf_kind(state, 1) != 5 || !signals.TryGetValue(woorf_get_signal_id(state, 1), out var signal))
            throw new InvalidOperationException("Expected RBXScriptSignal self.");
        return signal;
    }

    private ScriptConnection ReadConnection(nint state)
    {
        if (woorf_kind(state, 1) != 6 || !connections.TryGetValue(woorf_get_connection_id(state, 1), out var connection))
            throw new InvalidOperationException("Expected RBXScriptConnection self.");
        return connection;
    }

    private static string ReadString(nint state, int index)
    {
        if (woorf_kind(state, index) != 2) throw new InvalidOperationException("Expected string member name.");
        var data = woorf_get_string(state, index, out var length);
        return Marshal.PtrToStringUTF8(data, checked((int)length))!;
    }

    private void ReleaseCallbacks()
    {
        // Once disconnects before invocation; keep its closure alive until the host operation finishes.
        foreach (var reference in pendingReleases)
        {
            if (callbackReferences.Remove(reference)) woorf_unref_callback(handle, reference);
        }
        pendingReleases.Clear();
    }

    public void Dispose()
    {
        foreach (var connection in connections.Values) connection.Disconnect();
        foreach (var reference in callbackReferences) woorf_unref_callback(handle, reference);
        callbackReferences.Clear();
        pendingReleases.Clear();
    }
}
