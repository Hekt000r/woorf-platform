using System.Diagnostics;
using System.Net.Sockets;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Networking;

static class MultiplayerInputTests
{
    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-play-input-" + Guid.NewGuid().ToString("N"));
        Process? server = null;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src/ServerScriptService"));
            File.WriteAllText(Path.Combine(root, "woorf.project.json"), "{}");
            File.WriteAllText(Path.Combine(root, "src/ServerScriptService/Main.server.luau"), """
                workspace.Gravity = 80
                local floor = Instance.new("Part", workspace)
                floor.Anchored = true; floor.Size = Vector3.new(200, 1, 200)
                workspace:SetAttribute("CharacterSpawn", Vector3.new(0, 3, 0))
                local players = game:GetService("Players")
                players.CharacterAutoLoads = true; players.RespawnTime = 0.05
                """);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in new[] { cliPath, root, "--listen", "127.0.0.1:0" }) start.ArgumentList.Add(arg);
            server = Process.Start(start)!;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var listening = await server.StandardOutput.ReadLineAsync(timeout.Token);
            if (listening?.StartsWith("Listening ") != true) throw new Exception("Server failed to listen.");
            var address = listening[10..];
            using var first = new Client(address); using var second = new Client(address);
            await Until(() => first.Player?.Character is not null && second.Player?.Character is not null, first, second);
            await Task.Delay(250); first.Pump(); second.Pump();
            var firstStart = first.Player!.Character!.PrimaryPart!.Position;
            var secondStart = second.Player!.Character!.PrimaryPart!.Position;
            first.Send(new(1, 0, 0), false, false);
            await Until(() => first.Player.Character.PrimaryPart.Position.X > firstStart.X + 1, first, second);
            if (MathF.Abs(second.Player.Character.PrimaryPart.Position.X - secondStart.X) > 0.1f)
                throw new Exception("One client's input moved another player.");
            first.Send(default, true, false);
            await Until(() => first.Player.Character.PrimaryPart.AssemblyLinearVelocity.Y > 15, first, second);
            var old = first.Player.Character;
            first.Send(default, false, true);
            await Until(() => first.Player.Character != old && first.Player.Character is not null, first, second);
            using var pumped = new SocketPlayClient(address);
            await Until(() => { pumped.Pump(); return pumped.LocalPlayer.Character is not null; }, first, second);
            var pumpedStart = pumped.LocalPlayer.Character!.PrimaryPart!.Position;
            pumped.SendInput(new(0, 0, -1), false, false);
            await Until(() => { pumped.Pump(); return pumped.LocalPlayer.Character.PrimaryPart.Position.Z < pumpedStart.Z - 1; }, first, second);
            await Task.Delay(700); pumped.Pump();
            var stopped = pumped.LocalPlayer.Character.PrimaryPart.Position;
            await Task.Delay(150); pumped.Pump();
            if ((pumped.LocalPlayer.Character.PrimaryPart.Position - stopped).Magnitude > 0.1f)
                throw new Exception("A stalled graphical client retained movement after the input lease expired.");
            await CheckRejectedInput(address, new("input", Player: 999, Arguments: Input(default)), first, second);
            await CheckRejectedInput(address, new("input", Arguments: Input(new(float.NaN, 0, 0))), first, second);
            Console.WriteLine("PASS graphical input moves only its authenticated player and jumps resets and respawns");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL graphical network input: " + error.Message); return 1; }
        finally
        {
            if (server is not null) { if (!server.HasExited) { server.Kill(true); await server.WaitForExitAsync(); } server.Dispose(); }
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static WireValue[] Input(Vector3 direction) =>
        [WireValue.Encode(direction, _ => 0), WireValue.Encode(false, _ => 0), WireValue.Encode(false, _ => 0)];

    private static async Task CheckRejectedInput(string address, WirePacket packet, params Client[] healthy)
    {
        using var attacker = new Client(address);
        await Until(() => attacker.Player is not null, [attacker, .. healthy]);
        attacker.SendPacket(packet);
        var started = Stopwatch.StartNew();
        while (started.Elapsed.TotalSeconds < 2)
        {
            attacker.Pump(true);
            foreach (var client in healthy) client.Pump();
            if (attacker.IsClosed) return;
            await Task.Delay(5);
        }
        throw new Exception("Server accepted forged or nonfinite player input.");
    }

    private static async Task Until(Func<bool> condition, params Client[] clients)
    {
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed.TotalSeconds < 4)
        {
            foreach (var client in clients) client.Pump();
            if (condition()) return;
            await Task.Delay(5);
        }
        throw new Exception("Timed out waiting for authoritative character state.");
    }

    private sealed class Client : IDisposable
    {
        private readonly SocketPeer peer;
        private readonly WireGraph graph;
        private readonly ReplicaUpdates updates;
        internal Player? Player;
        internal bool IsClosed => peer.IsClosed;
        internal Client(string address)
        {
            var endpoint = System.Net.IPEndPoint.Parse(address);
            var socket = new TcpClient { NoDelay = true }; socket.Connect(endpoint);
            peer = new(socket); graph = new(new DataModel(), false); updates = new(graph);
            peer.Send(new("hello")); peer.Writes.Pump();
        }
        internal void Pump(bool canClose = false)
        {
            foreach (var packet in peer.Read())
            {
                if (packet.Kind == "welcome") { updates.Initialize(packet.Nodes!); Player = (Player)graph.Find(packet.Player)!; }
                else if (packet.Kind == "delta") updates.Apply(packet);
            }
            if (peer.IsClosed && !canClose) throw new Exception("Server rejected the graphical input connection.");
            peer.Writes.Pump();
        }
        internal void Send(Vector3 direction, bool jump, bool reset) => peer.Send(new("input", Arguments:
            [WireValue.Encode(direction, _ => 0), WireValue.Encode(jump, _ => 0), WireValue.Encode(reset, _ => 0)]));
        internal void SendPacket(WirePacket packet) { peer.Send(packet); peer.Writes.Pump(); }
        public void Dispose() { peer.Dispose(); graph.Dispose(); }
    }
}
