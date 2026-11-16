workspace.Gravity = 45

local function box(name, size, frame, anchored)
    local part = Instance.new("Part")
    part.Name = name
    part.Size = size
    part.CFrame = frame
    part.Anchored = anchored
    part.CustomPhysicalProperties = PhysicalProperties.new(0.7, 0.55, 0.15)
    part.Parent = workspace
    return part
end

box("Foundation", Vector3.new(38, 1, 32), CFrame.new(0, -0.5, 0), true)
for index = 1, 5 do
    box("Step" .. index, Vector3.new(5, index, 2), CFrame.new(-10, index / 2, 6 - index * 2), true)
end
box("Ramp", Vector3.new(5, 1, 10), CFrame.new(9, 2.1, -5) * CFrame.Angles(math.rad(-22), 0, 0), true)
for _, x in {-16, 16} do
    box("Pillar", Vector3.new(1, 6, 1), CFrame.new(x, 3, -12), true)
end

local display = box("RotatingDisplay", Vector3.new(3, 3, 3), CFrame.new(0, 4, -7), false)
display.CanCollide = false
local time = 0
local elapsedSpawn = 2
local sequence = 0
game:GetService("RunService").Heartbeat:Connect(function(elapsed)
    time += elapsed
    display.CFrame = CFrame.new(0, 4 + math.sin(time) * 0.4, -7) * CFrame.Angles(time * 0.4, time * 0.7, time * 0.2)
    display.AssemblyLinearVelocity = Vector3.zero
    elapsedSpawn += elapsed
    if elapsedSpawn < 1.1 then return end
    elapsedSpawn = 0
    sequence += 1
    local isBall = sequence % 2 == 0
    local x = ((sequence * 3) % 13) - 6
    local part = box("Falling" .. sequence, Vector3.new(2, 2, 2), CFrame.new(x, 12, 2), false)
    if isBall then part.Shape = Enum.PartType.Ball end
    part.AssemblyAngularVelocity = Vector3.new(0.4, 0.7, 0.3)
    game:GetService("Debris"):AddItem(part, 16)
end)

for index = 1, 4 do
    local part = box("Initial" .. index, Vector3.new(2, 2, 2), CFrame.new(index * 3 - 7.5, 3 + index, 2), false)
    if index % 2 == 0 then part.Shape = Enum.PartType.Ball end
end
