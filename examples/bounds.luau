local model = Instance.new("Model", workspace)
model.Name = "MeasuredParts"

local left = Instance.new("Part", model)
left.Size = Vector3.new(2, 4, 2)
left.Position = Vector3.new(-4, -2, 1)

local nested = Instance.new("Model", model)
local right = Instance.new("Part", nested)
right.Size = Vector3.new(6, 2, 4)
right.Position = Vector3.new(4, 3, -2)

local bounds, size = model:GetBoundingBox()
print("Center", bounds.Position)
print("Size", size)
assert(bounds.Position == Vector3.new(1, 0, -1))
assert(size == Vector3.new(12, 8, 6))
assert(model:GetExtentsSize() == size)

model.WorldPivot = CFrame.new(0, 0, 0, 0, -1, 0, 1, 0, 0, 0, 0, 1)
local rotatedBounds, rotatedSize = model:GetBoundingBox()
print("Pivot-aligned size", rotatedSize)
assert(rotatedBounds.Position == Vector3.new(1, 0, -1))
assert(rotatedSize == Vector3.new(8, 12, 6))
