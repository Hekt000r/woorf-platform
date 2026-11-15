using static NetworkProjectTests;

static class NetworkValueCases
{
    public static IEnumerable<(string Name, Action<string> Arrange, string Output)> GetCases()
    {
        yield return ("copies RemoteEvent values across VMs preserving tuples and replicated references", ArrangeValues, "values\n");
        yield return ("rejects invalid RemoteEvent calls and replaces transmitted functions with nil", ArrangeErrors, "errors\n");
    }

    private static void ArrangeValues(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local storage = game:GetService("ReplicatedStorage")
            local remote = Instance.new("RemoteEvent", storage); remote.Name = "Message"
            local marker = Instance.new("Folder", storage); marker.Name = "Marker"
            local secret = Instance.new("Folder", game:GetService("ServerStorage"))
            local completed = 0
            remote.OnServerEvent:Connect(function(player, kind, ...)
                if kind == "ack" then
                    completed += 1
                    if completed == 2 then print("values") end
                    return
                end
                assert(kind == "values")
                local args = table.pack(...)
                assert(args.n == 9 and args[9] == nil)
                local data = args[1]
                assert(data.Map.Greeting == "cześć 世界" and data.Array[1] == 4 and data.Array[2] == false)
                assert(args[2] == 42.25 and args[3] == true and args[4] == "雪")
                assert(args[5] == Vector3.new(1,2,3) and args[6] == CFrame.new(4,5,6))
                assert(args[7] == marker and args[8] == nil)
                data.Map.Greeting = "server"
                remote:FireClient(player, data, player, secret, marker, nil)
                data.Map.Greeting = "edited after send"
            end)
            """);
        WriteClient(root, """
            local remote = game:GetService("ReplicatedStorage").Message
            local marker = game:GetService("ReplicatedStorage").Marker
            local localPart = Instance.new("Folder", workspace)
            local sent = {Map = {Greeting = "cześć 世界"}, Array = {4, false, "six"}}
            remote.OnClientEvent:Connect(function(...)
                local args = table.pack(...)
                assert(args.n == 5 and args[5] == nil)
                assert(args[1] ~= sent and args[1].Map.Greeting == "server")
                assert(args[2] == game:GetService("Players").LocalPlayer and args[3] == nil and args[4] == marker)
                remote:FireServer("ack")
            end)
            remote:FireServer("values", sent, 42.25, true, "雪", Vector3.new(1,2,3), CFrame.new(4,5,6), marker, localPart, nil)
            sent.Map.Greeting = "edited client table"
            """);
    }

    private static void ArrangeErrors(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local remote = Instance.new("RemoteEvent", game:GetService("ReplicatedStorage")); remote.Name = "Message"
            assert(not pcall(function() remote:FireServer("bad") end))
            assert(not pcall(function() remote:FireClient(workspace) end))
            local completed, receivedFunctions = 0, {}
            remote.OnServerEvent:Connect(function(player, kind, ...)
                if kind == "functions" then
                    local args = table.pack(...)
                    assert(args.n == 3 and args[1] == nil and args[3] == nil)
                    assert(args[2].Nested.Callback == nil and args[2].Nested.Keep == "kept")
                    receivedFunctions[player] = true
                    return
                end
                assert(kind == "errors checked" and receivedFunctions[player])
                completed += 1
                if completed == 2 then print("errors") end
            end)
            """);
        WriteClient(root, """
            local remote = game:GetService("ReplicatedStorage").Message
            assert(not pcall(function() remote:FireClient(game:GetService("Players").LocalPlayer) end))
            assert(not pcall(function() remote:FireAllClients("bad") end))
            remote:FireServer("functions", function() end, {Nested = {Callback = function() end, Keep = "kept"}}, nil)
            local cycle = {}; cycle.Self = cycle
            assert(not pcall(function() remote:FireServer(cycle) end))
            assert(not pcall(function() remote:FireServer(coroutine.create(function() end)) end))
            remote:FireServer("errors checked")
            """);
    }
}
