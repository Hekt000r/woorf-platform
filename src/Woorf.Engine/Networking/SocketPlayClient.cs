using System.Diagnostics;
using System.Net;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Networking;

internal sealed class SocketPlayClient : IDisposable
{
    private readonly SocketRuntime runtime;
    internal DataModel.DataModel Game => runtime.PlayGame;
    internal Player LocalPlayer => runtime.PlayPlayer ?? throw new InvalidOperationException("Client handshake is incomplete.");

    internal SocketPlayClient(string address, string? identityFile = null)
    {
        var endpoint = IPEndPoint.Parse(address);
        runtime = new(identityFile: identityFile);
        try
        {
            runtime.OpenPlay(endpoint.Address.ToString(), endpoint.Port);
            var started = Stopwatch.StartNew();
            while (runtime.PlayPlayer is null)
            {
                if (started.Elapsed.TotalSeconds > 5) throw new IOException("Server handshake timed out.");
                runtime.PumpPlay(); Thread.Sleep(1);
            }
        }
        catch { runtime.Dispose(); throw; }
    }

    internal void Pump() => runtime.PumpPlay();
    internal void ToggleTool(Tool tool) => runtime.SendToolInput(tool, false);
    internal void UseTool(bool isDown)
    {
        if (LocalPlayer is { } player) PlayerInventory.Use(player, isDown);
        runtime.SendToolInput(null, isDown);
    }

    internal void SendInput(Vector3 direction, bool shouldJump, bool shouldReset, Vector3 cameraHeading = default) =>
        runtime.SendPlayInput(direction, shouldJump, shouldReset, cameraHeading);
    public void Dispose() => runtime.Dispose();
}
