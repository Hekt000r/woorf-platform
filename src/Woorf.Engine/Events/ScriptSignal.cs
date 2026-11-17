namespace Woorf.Engine.Events;

public sealed class ScriptSignal
{
    private readonly List<ScriptConnection> connections = [];
    private bool isClosed;
    private readonly Func<bool>? canConnect;
    internal bool HasConnections => connections.Count > 0;

    public ScriptSignal() { }
    internal ScriptSignal(Func<bool> canConnect) => this.canConnect = canConnect;

    public ScriptConnection Connect(Action<object?[]> handler, bool isOnce = false)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (canConnect?.Invoke() == false) throw new InvalidOperationException("Cannot connect touch events when CanTouch is false.");
        var connection = new ScriptConnection(this, handler, isOnce);
        if (isClosed) connection.Disconnect();
        else connections.Add(connection);
        return connection;
    }

    public void Fire(params object?[] arguments)
    {
        if (isClosed) return;
        foreach (var connection in connections.ToArray())
        {
            try { connection.Invoke(arguments); }
            catch (Exception error) { Console.Error.WriteLine(error.Message); }
        }
    }

    public void Close()
    {
        if (isClosed) return;
        isClosed = true;
        DisconnectAll();
    }

    internal void DisconnectAll()
    {
        foreach (var connection in connections.ToArray()) connection.Disconnect();
    }

    internal void Remove(ScriptConnection connection) => connections.Remove(connection);
}
