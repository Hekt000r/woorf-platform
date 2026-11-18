using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;

namespace Woorf.Engine.Networking;

public sealed class LocalPlayHost : IDisposable
{
    private readonly LocalNetworkHost host;
    private readonly Player authority;
    public DataModel.DataModel Game { get; }
    public Player Player { get; }
    public int StartupStatus { get; }
    public bool HasFailed => host.HasFailed;

    public LocalPlayHost(LoadedProject project, IRuntimeClock clock)
    {
        host = new(project, clock) { IsInteractivePreview = true };
        try
        {
            ((Players)project.Game.GetService("Players")).CharacterAutoLoads = true;
            StartupStatus = host.Start();
            if (StartupStatus != 0) throw new InvalidOperationException("Server script startup failed.");
            authority = host.ConnectClient();
            Game = host.GetClientGame(authority);
            Player = ((Players)Game.GetService("Players")).LocalPlayer!;
            Pump();
        }
        catch { host.Dispose(); throw; }
    }

    public int Pump() => host.Pump();

    public void SendInput(Vector3 direction, bool shouldJump, bool shouldReset, Vector3 cameraHeading = default)
    {
        if (authority.Character?.FindFirstChild("Humanoid") is not Humanoid humanoid) return;
        humanoid.CameraHeading = cameraHeading;
        humanoid.Move(direction);
        if (shouldJump) humanoid.Jump = true;
        if (shouldReset) humanoid.Health = 0;
    }

    public void ToggleTool(Tool tool)
    {
        if (host.GetAuthorityTool(authority, tool) is { } original) PlayerInventory.Toggle(authority, original);
        else PlayerInventory.Toggle(Player, tool);
    }

    public void UseTool(bool isDown)
    {
        PlayerInventory.Use(Player, isDown);
        PlayerInventory.Use(authority, isDown);
    }

    public int Finish(int status) { Dispose(); return status != 0 || HasFailed ? 1 : 0; }
    public void Dispose() => host.Dispose();
}
