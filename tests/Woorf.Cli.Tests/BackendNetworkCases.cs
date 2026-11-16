using static NetworkProjectTests;

static class BackendNetworkCases
{
    internal static IEnumerable<(string Name, Action<string> Arrange, string Output)> GetCases()
    {
        yield return ("replicates backend metadata values health and team state across isolated clients",
            Arrange, "backend replication\n");
    }

    private static void Arrange(string root)
    {
        Write(root, "src/ServerScriptService/Main.server.luau", """
            local storage = game:GetService("ReplicatedStorage")
            local state = Instance.new("Folder", storage); state.Name = "State"
            state:SetAttribute("Coins", 10); state:SetAttribute("Tint", Color3.new(1,0,0))
            state:AddTag("Active")
            local target = Instance.new("Folder", game:GetService("ServerStorage")); target.Name = "Target"
            local pointer = Instance.new("ObjectValue", state); pointer.Name = "Pointer"; pointer.Value = target
            local coins = Instance.new("IntValue", state); coins.Name = "Coins"; coins.Value = 10
            local humanoid = Instance.new("Humanoid", state); humanoid.Name = "Health"; humanoid.Health = 75
            local team = Instance.new("Team", game:GetService("Teams")); team.Name = "Blue"
            game:GetService("Players").PlayerAdded:Connect(function(player) player.Team = team end)
            local event = Instance.new("RemoteEvent", storage); event.Name = "Message"
            local ready, done = 0, 0
            event.OnServerEvent:Connect(function(player, kind)
                if kind == "ready" then
                    ready += 1
                    if ready == 2 then
                        target.Parent = workspace
                        state:SetAttribute("Coins", 20); state:RemoveTag("Active")
                        coins.Value = 20; humanoid.Health = 25
                        event:FireAllClients("update", Color3.new(0,1,0))
                    end
                else
                    assert(kind == "done"); done += 1
                    if done == 2 then print("backend replication") end
                end
            end)
            """);
        WriteClient(root, """
            local storage = game:GetService("ReplicatedStorage")
            local state = storage.State
            local player = game:GetService("Players").LocalPlayer
            assert(not pcall(function() game:GetService("DataStoreService"):GetDataStore("Private") end))
            assert(player.Team == game:GetService("Teams").Blue and not player.Neutral)
            assert(state:GetAttribute("Coins") == 10 and state:GetAttribute("Tint") == Color3.new(1,0,0))
            assert(state:HasTag("Active") and state.Coins.Value == 10 and state.Health.Health == 75)
            assert(state.Pointer.Value == nil)
            state:SetAttribute("Coins", 999); state:SetAttribute("ClientOnly", true)
            storage.Message.OnClientEvent:Connect(function(kind, color)
                assert(state.Pointer.Value == workspace.Target)
                assert(kind == "update" and color == Color3.new(0,1,0))
                assert(state:GetAttribute("Coins") == 20 and state:GetAttribute("ClientOnly") == true)
                assert(not state:HasTag("Active") and state.Coins.Value == 20 and state.Health.Health == 25)
                storage.Message:FireServer("done")
            end)
            storage.Message:FireServer("ready")
            """);
    }
}
