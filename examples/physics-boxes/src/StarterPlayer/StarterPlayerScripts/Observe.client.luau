local player = game:GetService("Players").LocalPlayer
local storage = game:GetService("ReplicatedStorage")
local box = workspace.Box
local ball = workspace.Ball
assert(ball.Shape == Enum.PartType.Block and ball.Anchored)
assert(workspace.Gravity == 50 and box.CanCollide and box.Anchored)
assert(box.CustomPhysicalProperties ~= nil and box.CustomPhysicalProperties.Elasticity == 0)
local sawFall = false
storage.Motion.OnClientEvent:Connect(function(phase, position, velocity, material)
    if phase == "ack" then print("physics-client"); player:Kick("Physics scene complete"); return end
    assert((box.Position - position).Magnitude < 0.0001)
    assert((box.AssemblyLinearVelocity - velocity).Magnitude < 0.0001)
    assert(not box.Anchored and box.CanCollide)
    assert(ball.Shape == Enum.PartType.Ball and not ball.Anchored)
    if position.Y < 9 and position.Y > 2 and velocity.Y < -1 then sawFall = true end
    if phase == "settled" then
        assert(sawFall and math.abs(position.Y - 1) < 0.08)
        assert(material == box.CustomPhysicalProperties)
        assert(math.abs(ball.Position.Y - 1) < 0.08 and math.abs(ball.AssemblyLinearVelocity.Y) < 0.2)
        assert(ball.CustomPhysicalProperties == material)
        storage.Motion:FireServer("done")
    end
end)
storage.Motion:FireServer("ready")
