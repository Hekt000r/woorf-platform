using static NetworkProjectTests;

static class NetworkIsolationCases
{
    public static IEnumerable<(string Name, Action<string> Arrange, string Output)> GetCases()
    {
        yield return ("isolates client state modules and private roots while replicating server changes", ArrangeIsolation, "isolation\n");
        yield return ("runs StarterGui LocalScripts in each owning PlayerGui", ArrangePlayerGui, "gui\n");
    }

    private static void ArrangeIsolation(string root)
    {
        Write(root, "src/ReplicatedStorage/Shared.luau", "return {Count = 0}");
        Write(root, "src/ServerStorage/Secret.luau", "error('private source executed on client')");
        Write(root, "src/ServerStorage/Inactive.client.luau", "error('private LocalScript ran')");
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local storage = game:GetService("ReplicatedStorage")
            local remote = Instance.new("RemoteEvent", storage); remote.Name = "Message"
            local part = Instance.new("Part", workspace); part.Name = "SharedPart"; part.Anchored = true
            part.Position = Vector3.new(1,2,3)
            local shared = require(storage.Shared); shared.Count = 100
            local ready, done = 0, 0
            remote.OnServerEvent:Connect(function(player, kind)
                assert(shared.Count == 100 and workspace:FindFirstChild("ClientLocal") == nil)
                if kind == "ready" then
                    ready += 1
                    assert(part.Position == Vector3.new(1,2,3))
                    if ready == 2 then
                        part.Position = Vector3.new(7,8,9)
                        local dynamic = Instance.new("RemoteEvent", storage); dynamic.Name = "Dynamic"
                        remote:FireAllClients("update", dynamic)
                    end
                else
                    assert(kind == "done"); done += 1
                    if done == 2 then print("isolation") end
                end
            end)
            """);
        WriteClient(root, """
            assert(game:GetService("ServerStorage"):FindFirstChild("Secret") == nil)
            assert(game:GetService("ServerScriptService"):FindFirstChild("Main") == nil)
            local storage = game:GetService("ReplicatedStorage")
            local shared = require(storage.Shared)
            assert(shared.Count == 0); shared.Count = game:GetService("Players").LocalPlayer.UserId
            assert(require(storage.Shared) == shared)
            local part = workspace.SharedPart
            assert(part.Position == Vector3.new(1,2,3))
            part.Position = Vector3.new(90,90,90)
            part.Name = "ClientRenamed"
            Instance.new("Folder", workspace).Name = "ClientLocal"
            storage.Message.OnClientEvent:Connect(function(kind, dynamic)
                assert(kind == "update" and dynamic == storage.Dynamic)
                assert(part.Position == Vector3.new(7,8,9))
                assert(part.Name == "ClientRenamed")
                assert(workspace.ClientLocal.Parent == workspace)
                assert(shared.Count == game:GetService("Players").LocalPlayer.UserId)
                storage.Message:FireServer("done")
            end)
            storage.Message:FireServer("ready")
            """);
    }

    private static void ArrangePlayerGui(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local remote = Instance.new("RemoteEvent", game:GetService("ReplicatedStorage")); remote.Name = "Message"
            local completed = 0
            remote.OnServerEvent:Connect(function(player, marker)
                assert(marker == "gui")
                completed += 1
                if completed == 2 then print("gui") end
            end)
            """);
        Write(root, "src/StarterGui/Menu.client.luau", """
            local player = game:GetService("Players").LocalPlayer
            assert(script.Parent == player.PlayerGui)
            game:GetService("ReplicatedStorage").Message:FireServer("gui")
            """);
    }
}
