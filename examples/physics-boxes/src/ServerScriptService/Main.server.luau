workspace.Gravity = 50
local material = PhysicalProperties.new(0.7, 0.5, 0)
local floor = Instance.new("Part", workspace); floor.Name = "Floor"
floor.Anchored = true; floor.Size = Vector3.new(40, 2, 40); floor.Position = Vector3.new(0, -1, 0)
floor.CustomPhysicalProperties = material
local box = Instance.new("Part", workspace); box.Name = "Box"
box.Anchored = true; box.Size = Vector3.new(2, 2, 2); box.Position = Vector3.new(0, 10, 0)
box.CustomPhysicalProperties = material
local ball = box:Clone(); ball.Name = "Ball"; ball.Position = Vector3.new(5,10,0); ball.Parent = workspace
local event = Instance.new("RemoteEvent", game:GetService("ReplicatedStorage")); event.Name = "Motion"
local ready, done = {}, {}
local elapsed, count = 0, 0
local connection
connection = game:GetService("RunService").Heartbeat:Connect(function(delta)
    if count ~= 2 then return end
    elapsed += delta
    if elapsed < 1.2 then event:FireAllClients("motion", box.Position, box.AssemblyLinearVelocity)
    else
        assert(math.abs(box.Position.Y - 1) < 0.08 and math.abs(box.AssemblyLinearVelocity.Y) < 0.2)
        assert(math.abs(ball.Position.Y - 1) < 0.08 and math.abs(ball.AssemblyLinearVelocity.Y) < 0.2)
        event:FireAllClients("settled", box.Position, box.AssemblyLinearVelocity, material)
        connection:Disconnect()
    end
end)
event.OnServerEvent:Connect(function(player, action)
    if action == "ready" and not ready[player] then
        ready[player] = true; count += 1
        if count == 2 then box.Anchored = false; ball.Shape = Enum.PartType.Ball; ball.Anchored = false end
    elseif action == "done" and ready[player] and not done[player] then
        done[player] = true
        event:FireClient(player, "ack")
        local completed = 0
        for _ in pairs(done) do completed += 1 end
        if completed == 2 then print("physics-server") end
    end
end)
