local model = Instance.new("Model", workspace)
model.Name = "PlatformGroup"

local first = Instance.new("Part", model)
first.Name = "First"
first.Anchored = true
first.Position = Vector3.new(-3, 0, 0)

local second = Instance.new("Part", model)
second.Name = "Second"
second.Anchored = true
second.Position = Vector3.new(3, 0, 0)

print("Initial pivot:", model:GetPivot().Position)
model:PivotTo(CFrame.new(0, 10, 0) * CFrame.Angles(0, math.rad(90), 0))
print(first.Name, first.Position)
print(second.Name, second.Position)

model.PrimaryPart = first
first.PivotOffset = CFrame.new(0, 1, 0)
model:PivotTo(CFrame.new(10, 10, 0))
print("PrimaryPart pivot:", model:GetPivot().Position)
