local part = Instance.new("Part", workspace)
part.Name = "RotatedPlatform"
part.Anchored = true
part.Size = Vector3.new(12, 1, 8)
part.CFrame = CFrame.new(0, 5, 0) * CFrame.Angles(0, math.rad(45), 0)

print("Position:", part.Position)
print("Forward:", part.CFrame.LookVector)
print("Local front point:", part.CFrame:PointToWorldSpace(Vector3.new(0, 0, -4)))

local rotation = part.CFrame.Rotation
part.Position = Vector3.new(10, 5, 0)
assert(part.CFrame.Rotation == rotation)

local clone = part:Clone()
clone.Name = "SecondPlatform"
clone.CFrame = part.CFrame * CFrame.new(0, 0, 10)
clone.Parent = workspace
print(clone.Name, clone.Position)
