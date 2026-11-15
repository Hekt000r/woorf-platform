namespace Woorf.Engine.Events;

public sealed class ScriptSignal
{
    private readonly List<ScriptConnection> connections = [];
    private bool isClosed;

    public ScriptConnection Connect(Action<object?[]> handler, bool isOnce = false)
    {
        ArgumentNullException.ThrowIfNull(handler);
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
        foreach (var connection in connections.ToArray()) connection.Disconnect();
    }

    internal void Remove(ScriptConnection connection) => connections.Remove(connection);
}
