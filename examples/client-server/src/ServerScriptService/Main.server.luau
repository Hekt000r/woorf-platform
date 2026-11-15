local players = game:GetService("Players")
local remote = Instance.new("RemoteEvent", game:GetService("ReplicatedStorage"))
remote.Name = "Greeting"

players.PlayerAdded:Connect(function(player)
    print("Joined:", player.Name)
end)

remote.OnServerEvent:Connect(function(player, message)
    print("Server received from", player.Name, message)
    remote:FireClient(player, "Hello, " .. player.Name)
end)
