namespace Woorf.Engine.Events;

public sealed class ScriptConnection
{
    private ScriptSignal? signal;
    private Action<object?[]>? handler;
    private readonly bool isOnce;

    internal ScriptConnection(ScriptSignal signal, Action<object?[]> handler, bool isOnce)
    {
        this.signal = signal;
        this.handler = handler;
        this.isOnce = isOnce;
        Connected = true;
    }

    public bool Connected { get; private set; }
    public event Action? Disconnected;

    public void Disconnect()
    {
        if (!Connected) return;
        Connected = false;
        signal?.Remove(this);
        signal = null;
        handler = null;
        var onDisconnected = Disconnected;
        Disconnected = null;
        onDisconnected?.Invoke();
    }

    internal void Invoke(object?[] arguments)
    {
        if (!Connected) return;
        var callback = handler!;
        if (isOnce) Disconnect();
        callback(arguments);
    }
}
