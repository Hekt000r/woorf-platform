workspace.Gravity = 80
workspace:SetAttribute("CharacterSpawn", Vector3.new(0, 3, 8))

local function box(name, size, frame)
    local part = Instance.new("Part")
    part.Name = name
    part.Size = size
    part.CFrame = frame
    part.Anchored = true
    part.CustomPhysicalProperties = PhysicalProperties.new(0.7, 0.5, 0)
    part.Parent = workspace
    return part
end

box("Ground", Vector3.new(80, 1, 80), CFrame.new(0, -0.5, 0))
box("SpawnPad", Vector3.new(6, 0.2, 6), CFrame.new(0, 0.1, 8))
box("LeftWall", Vector3.new(5, 5, 1), CFrame.new(-5, 2.5, -1))
box("RightWall", Vector3.new(5, 5, 1), CFrame.new(5, 2.5, -1))
box("FirstJump", Vector3.new(6, 1, 6), CFrame.new(0, 0.5, -10))
box("SecondJump", Vector3.new(6, 2, 6), CFrame.new(0, 1, -20))
box("Ramp", Vector3.new(6, 1, 14), CFrame.new(-14, 2.7, -12) * CFrame.Angles(math.rad(-20), 0, 0))
box("RampLanding", Vector3.new(8, 1, 8), CFrame.new(-14, 4.5, -22))
box("ArchLeft", Vector3.new(1, 7, 1), CFrame.new(9, 3.5, -12))
box("ArchRight", Vector3.new(1, 7, 1), CFrame.new(17, 3.5, -12))
box("ArchCeiling", Vector3.new(9, 1, 2), CFrame.new(13, 7.5, -12))
for _, x in {-3, 3} do
    local marker = box("FinishMarker", Vector3.new(2, 2, 2), CFrame.new(x, 1, -32))
    marker.Shape = Enum.PartType.Ball
end
