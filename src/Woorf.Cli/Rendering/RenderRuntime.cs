using Woorf.Engine.DataModel;
using Woorf.Engine.Networking;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Scripting;

internal sealed class RenderRuntime : IDisposable
{
    private readonly LuauSession? session;
    private readonly SocketPlayClient? client;
    private readonly LocalPlayHost? local;
    internal DataModel Game { get; }
    internal Player? Player { get; }
    internal int StartupStatus { get; }
    internal bool HasFailed => session?.HasFailed == true || local?.HasFailed == true;

    internal RenderRuntime(RenderOptions options, MonotonicClock clock)
    {
        if (options.ConnectAddress is not null)
        {
            client = new(options.ConnectAddress, options.IdentityFile);
            Game = client.Game; Player = client.LocalPlayer;
            return;
        }
        var project = Directory.Exists(options.Source) ? ProjectLoader.Load(options.Source) : null;
        if (options.IsPlayable && project is not null)
        {
            local = new(project, clock);
            Game = local.Game; Player = local.Player; StartupStatus = local.StartupStatus;
            return;
        }
        Game = project?.Game ?? new DataModel();
        session = new(Game, clock);
        StartupStatus = project is null ? session.Execute(File.ReadAllText(options.Source), options.Source) : session.StartScripts(project.Scripts);
        if (options.IsPlayable && StartupStatus == 0)
        {
            var players = (Players)Game.GetService("Players"); players.CharacterAutoLoads = true;
            Player = new Player(1, "Player1") { Parent = players };
        }
    }

    internal CharacterViewer? CreateViewer() => Player is null ? null : new(Player, client is null && local is null ? null : SendInput);
    private void SendInput(CharacterInput input)
    {
        if (local is not null) local.SendInput(input.MoveDirection, input.ShouldJump, input.ShouldReset);
        else client!.SendInput(input.MoveDirection, input.ShouldJump, input.ShouldReset);
    }
    internal void Step(double now) { if (local is not null) local.Pump(); else if (client is null) session!.Step(now); else client.Pump(); }
    internal int Finish(int status) => local?.Finish(status) ?? session?.Finish(status) ?? status;
    public void Dispose() { local?.Dispose(); client?.Dispose(); session?.Dispose(); }
}
