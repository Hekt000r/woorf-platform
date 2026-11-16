using static NetworkProjectTests;

static class RemoteFunctionCases
{
    public static IEnumerable<(string Name, Action<string> Arrange, string Output)> GetCases()
    {
        yield return ("returns yielding RemoteFunction replies to their calling clients", ArrangeServerInvoke, "functions\n");
        yield return ("fails pending invocations when their remote is destroyed", ArrangeDestruction, "destroyed\n");
        yield return ("fails pending invocations when the callback script is destroyed", ArrangeCallbackCancellation, "cancelled\n");
        yield return ("invokes yielding client callbacks and propagates their failures", ArrangeClientInvoke, "client invoke\n");
        yield return ("queues calls before callback assignment and correlates concurrent replies", ArrangeLateCallback, "late concurrent\n");
    }

    private static void ArrangeDestruction(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local storage = game:GetService("ReplicatedStorage")
            local remote = Instance.new("RemoteFunction", storage)
            remote.Name = "Query"
            remote.OnServerInvoke = function() task.wait(10) end
            task.delay(0.04, function() remote:Destroy() end)
            """);
        WriteClient(root, """
            local remote = game:GetService("ReplicatedStorage").Query
            local ok, message = pcall(function() remote:InvokeServer() end)
            assert(not ok and string.find(message, "destroyed", 1, true))
            if game:GetService("Players").LocalPlayer.UserId == 1 then print("destroyed") end
            """);
    }

    private static void ArrangeCallbackCancellation(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local remote = Instance.new("RemoteFunction", game:GetService("ReplicatedStorage"))
            remote.Name = "Query"
            remote.OnServerInvoke = function() task.wait(10) end
            task.delay(0.04, function() script:Destroy() end)
            """);
        WriteClient(root, """
            local remote = game:GetService("ReplicatedStorage").Query
            local ok, message = pcall(function() remote:InvokeServer() end)
            assert(not ok and string.find(message, "cancelled", 1, true))
            if game:GetService("Players").LocalPlayer.UserId == 1 then print("cancelled") end
            """);
    }

    private static void ArrangeClientInvoke(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local remote = Instance.new("RemoteFunction", game:GetService("ReplicatedStorage"))
            remote.Name = "Query"
            assert(remote:IsA("Instance") and not remote:IsA("RemoteEvent"))
            local ready = Instance.new("RemoteEvent", game:GetService("ReplicatedStorage"))
            ready.Name = "Ready"
            local completed = 0
            ready.OnServerEvent:Connect(function(player)
                local result = table.pack(remote:InvokeClient(player, player, nil))
                assert(result.n == 3 and result[1] == player.UserId and result[2] == nil)
                assert(result[3] == "client")
                local ok, message = pcall(function() remote:InvokeClient(player, "error") end)
                assert(not ok and string.find(message, "client failure", 1, true))
                completed += 1
                if completed == 2 then print("client invoke") end
            end)
            """);
        WriteClient(root, """
            local remote = game:GetService("ReplicatedStorage").Query
            remote.OnClientInvoke = function(player, ...)
                if player == "error" then error("client failure") end
                assert(player == game:GetService("Players").LocalPlayer)
                assert(select("#", ...) == 1)
                task.wait(0.01)
                return player.UserId, nil, "client"
            end
            assert(not pcall(function() remote:InvokeClient(game:GetService("Players").LocalPlayer) end))
            assert(not pcall(function() remote.OnServerInvoke = function() end end))
            assert(not pcall(function() remote.OnClientInvoke = 42 end))
            game:GetService("ReplicatedStorage").Ready:FireServer()
            """);
    }

    private static void ArrangeLateCallback(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local remote = Instance.new("RemoteFunction", game:GetService("ReplicatedStorage"))
            remote.Name = "Query"
            task.wait(0.03)
            remote.OnServerInvoke = function(player, value)
                if value == "error" then error("server failure") end
                if value == "cycle" then local cycle = {}; cycle.Self = cycle; return cycle end
                task.wait(if value == 1 then 0.02 else 0.01)
                return value
            end
            """);
        WriteClient(root, """
            local remote = game:GetService("ReplicatedStorage").Query
            local completed = 0
            for value = 1, 2 do
                task.spawn(function()
                    assert(remote:InvokeServer(value) == value)
                    completed += 1
                end)
            end
            task.spawn(function()
                local ok, message = pcall(function() remote:InvokeServer("error") end)
                assert(not ok and string.find(message, "server failure", 1, true))
                local canReturnCycle, cycleError = pcall(function() remote:InvokeServer("cycle") end)
                assert(not canReturnCycle and string.find(cycleError, "Cyclic", 1, true))
                while completed ~= 2 do task.wait() end
                if game:GetService("Players").LocalPlayer.UserId == 1 then print("late concurrent") end
            end)
            """);
    }

    private static void ArrangeServerInvoke(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local remote = Instance.new("RemoteFunction", game:GetService("ReplicatedStorage"))
            remote.Name = "Query"
            local completed = 0
            remote.OnServerInvoke = function(player, data, ...)
                assert(select("#", ...) == 1 and ... == nil)
                assert(data.id == player.UserId)
                task.wait(0.01)
                completed += 1
                if completed == 2 then print("functions") end
                return player, {id = data.id, message = "reply"}, nil
            end
            """);
        WriteClient(root, """
            local player = game:GetService("Players").LocalPlayer
            local remote = game:GetService("ReplicatedStorage").Query
            local sent = {id = player.UserId}
            local result = table.pack(remote:InvokeServer(sent, nil))
            assert(result.n == 3 and result[3] == nil)
            assert(result[1] == player)
            assert(result[2] ~= sent and result[2].id == player.UserId)
            assert(result[2].message == "reply")
            """);
    }
}
