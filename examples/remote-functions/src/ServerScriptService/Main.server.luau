local query = Instance.new("RemoteFunction", game:GetService("ReplicatedStorage"))
query.Name = "Greeting"
query.OnServerInvoke = function(player, message)
    task.wait(0.01)
    return "Hello, " .. player.Name, {Received = message}
end
