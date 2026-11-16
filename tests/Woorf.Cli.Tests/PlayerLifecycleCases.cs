using static NetworkProjectTests;

static class PlayerLifecycleCases
{
    public static IEnumerable<(string Name, Action<string> Arrange, string Output)> GetCases()
    {
        yield return ("disconnects a client while failing its pending calls and preserving other clients",
            ArrangeDisconnect, "lifecycle\n");
        yield return ("cancels server callbacks and client timers when their player disconnects",
            ArrangeCancelledServerCallback, "cancelled requests\n");
    }

    private static void ArrangeCancelledServerCallback(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local storage = game:GetService("ReplicatedStorage")
            local remote = Instance.new("RemoteFunction", storage); remote.Name = "Query"
            local started = false
            remote.OnServerInvoke = function(player)
                assert(player.UserId == 1)
                started = true
                task.wait(0.05)
                error("disconnected server callback resumed")
            end
            local ready = Instance.new("RemoteEvent", storage); ready.Name = "Ready"
            ready.OnServerEvent:Connect(function(player)
                assert(player.UserId == 2 and started)
                assert(#game:GetService("Players"):GetPlayers() == 1)
                print("cancelled requests")
            end)
            """);
        WriteClient(root, """
            local storage = game:GetService("ReplicatedStorage")
            local player = game:GetService("Players").LocalPlayer
            if player.UserId == 1 then
                task.spawn(function() storage.Query:InvokeServer() end)
                task.delay(0.01, function() player:Kick() end)
                task.delay(0.06, function() error("disconnected client timer resumed") end)
            else
                assert(not pcall(function() game:GetService("Players"):GetPlayers()[1]:Kick() end))
                task.delay(0.08, function() storage.Ready:FireServer() end)
            end
            """);
    }

    private static void ArrangeDisconnect(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local players = game:GetService("Players")
            local remote = Instance.new("RemoteFunction", game:GetService("ReplicatedStorage"))
            remote.Name = "Query"
            local ready = Instance.new("RemoteEvent", game:GetService("ReplicatedStorage"))
            ready.Name = "Ready"
            local removed = 0
            players.PlayerRemoving:Connect(function(player)
                assert(player.UserId == 1 and player.Parent == players)
                assert(#players:GetPlayers() == 2)
                removed += 1
            end)
            ready.OnServerEvent:Connect(function(player)
                if player.UserId == 1 then
                    local ok, message = pcall(function() remote:InvokeClient(player) end)
                    assert(not ok and string.find(message, "disconnected", 1, true))
                    task.wait(0.01)
                    assert(removed == 1 and #players:GetPlayers() == 1)
                    assert(not pcall(function() remote:InvokeClient(player) end))
                    assert(remote:InvokeClient(players:GetPlayers()[1]) == "alive")
                    print("lifecycle")
                end
            end)
            """);
        WriteClient(root, """
            local player = game:GetService("Players").LocalPlayer
            game:GetService("ReplicatedStorage").Query.OnClientInvoke = function()
                if player.UserId == 1 then
                    player:Kick("leaving")
                    task.wait(1)
                    error("disconnected callback resumed")
                end
                return "alive"
            end
            game:GetService("ReplicatedStorage").Ready:FireServer()
            """);
    }
}
