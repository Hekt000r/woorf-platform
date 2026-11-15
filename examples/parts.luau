local part = Instance.new("Part")
part.Name = "Platform"
part.Anchored = true
part.Size = Vector3.new(12, 1, 8)
part.Parent = workspace

part:GetPropertyChangedSignal("Position"):Connect(function()
    print("Platform position:", part.Position)
end)

part.Position = Vector3.new(0, 5, 0) + Vector3.xAxis * 3

local clone = part:Clone()
clone.Name = "SecondPlatform"
clone.Position += Vector3.new(0, 0, 10)
clone.Parent = workspace
print(clone.Name, clone.Position, clone.Size, clone.Anchored)
