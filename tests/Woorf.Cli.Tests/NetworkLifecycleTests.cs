using Woorf.Engine;
using Woorf.Engine.Networking;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;

static class NetworkLifecycleTests
{
    internal static int Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-lifecycle-" + Guid.NewGuid().ToString("N"));
        try
        {
            Arrange(root);
            using var host = new LocalNetworkHost(ProjectLoader.Load(root), new TestClock());
            var original = Console.Out;
            using var output = new StringWriter();
            try
            {
                Console.SetOut(output);
                AssertSuccess(host.Start());
                var first = host.ConnectClient();
                AssertSuccess(host.RunUntilIdle());
                host.DisconnectClient(first);
                host.DisconnectClient(first);
                var second = host.ConnectClient();
                AssertSuccess(host.RunUntilIdle());
                host.ConnectClient();
                host.DisconnectClient(second);
                AssertSuccess(host.RunUntilIdle());
            }
            finally { Console.SetOut(original); }
            if (output.ToString().Replace("\r\n", "\n") != "late join\nobserved join\nobserved removal\n")
                throw new Exception("Late client did not receive the current server snapshot: " + output);
            Console.WriteLine("PASS joins clients after startup and preserves snapshots across disconnects");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL joins clients after startup and preserves snapshots across disconnects: " + error);
            return 1;
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static void Arrange(string root)
    {
        NetworkProjectTests.Write(root, "woorf.project.json", "{\"name\":\"Lifecycle\",\"root\":\"src\"}");
        NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
            local players = game:GetService("Players")
            local storage = game:GetService("ReplicatedStorage")
            local remote = Instance.new("RemoteEvent", storage); remote.Name = "Ready"
            local joins, replies = 0, 0
            players.PlayerAdded:Connect(function(player)
                joins += 1
                assert(player.UserId == joins)
            end)
            remote.OnServerEvent:Connect(function(player)
                replies += 1
                assert(player.UserId == replies)
                if replies == 1 then
                    Instance.new("Folder", workspace).Name = "LateSnapshot"
                elseif replies == 2 then
                    assert(joins == 2)
                    print("late join")
                else assert(joins == 3) end
            end)
            """);
        NetworkProjectTests.WriteClient(root, """
            local player = game:GetService("Players").LocalPlayer
            if player.UserId == 2 then
                assert(workspace.LateSnapshot.Parent == workspace)
                game:GetService("Players").PlayerAdded:Connect(function(joined)
                    assert(joined.UserId == 3)
                    print("observed join")
                end)
            elseif player.UserId == 3 then
                game:GetService("Players").PlayerRemoving:Connect(function(leaving)
                    assert(leaving.UserId == 2 and leaving.Parent == game:GetService("Players"))
                    assert(#game:GetService("Players"):GetPlayers() == 2)
                    print("observed removal")
                end)
            end
            game:GetService("ReplicatedStorage").Ready:FireServer()
            """);
    }

    private static void AssertSuccess(object? result)
    {
        if (!Equals(result, 0)) throw new Exception("Host execution failed.");
    }

    private sealed class TestClock : IRuntimeClock
    {
        public double ElapsedSeconds { get; private set; }
        public void Sleep(TimeSpan duration) => ElapsedSeconds += duration.TotalSeconds;
    }
}
