local player = game:GetService("Players").LocalPlayer
local remote = game:GetService("ReplicatedStorage"):WaitForChild("Greeting")

remote.OnClientEvent:Connect(function(message)
    print("Client", player.Name, "received:", message)
end)

remote:FireServer("Hello from the client")
