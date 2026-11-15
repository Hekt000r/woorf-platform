static class ModelBoundsCases
{
    private const string Near = """
        local function near(a,b) assert((a-b).Magnitude < 0.0001, tostring(a).." differs from "..tostring(b)) end
        
        """;

    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return GetDescendantBoundsCase();
        yield return GetRotatedPartCase();
        yield return GetWorldPivotOrientationCase();
        yield return GetPrimaryPivotOrientationCase();
        yield return GetUpdatedBoundsCase();
        yield return GetReadOnlyCase();
        yield return GetEmptyAndReceiverCase();
    }

    private static (string, string?, int, string, string) GetDescendantBoundsCase() =>
        ("measures descendant Part geometry through nested Model and Folder containers", Near + """
            local model = Instance.new("Model")
            local folder = Instance.new("Folder", model)
            local a = Instance.new("Part", folder)
            a.Position = Vector3.new(-4,-2,1); a.Size = Vector3.new(2,4,2)
            local nested = Instance.new("Model", folder)
            local b = Instance.new("Part", nested)
            b.Position = Vector3.new(4,3,-2); b.Size = Vector3.new(6,2,4)
            local frame, size = model:GetBoundingBox()
            near(frame.Position, Vector3.new(1,0,-1))
            near(size, Vector3.new(12,8,6))
            assert(frame.Rotation == CFrame.identity and model:GetExtentsSize() == size)
            assert(select('#', model:GetBoundingBox()) == 2 and select('#', model:GetExtentsSize()) == 1)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetRotatedPartCase() =>
        ("includes rotated single Part corners in world aligned Model bounds", Near + """
            local model = Instance.new("Model")
            local part = Instance.new("Part", model)
            part.Size = Vector3.new(2,4,6)
            part.CFrame = CFrame.new(3,5,7) * CFrame.Angles(0,0,math.pi/4)
            local frame, size = model:GetBoundingBox()
            near(frame.Position, Vector3.new(3,5,7))
            near(size, Vector3.new(4.242640687,4.242640687,6))
            assert(frame.Rotation == CFrame.identity)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetWorldPivotOrientationCase() =>
        ("aligns Model bounds to WorldPivot rotation without using its position as box center", Near + """
            local model = Instance.new("Model")
            local part = Instance.new("Part", model)
            part.Position = Vector3.new(3,5,7); part.Size = Vector3.new(2,4,6)
            model.WorldPivot = CFrame.new(100,200,300, 0.6,0,0.8, 0,1,0, -0.8,0,0.6)
            local frame, size = model:GetBoundingBox()
            near(frame.Position, Vector3.new(3,5,7))
            near(size, Vector3.new(6,4,5.2))
            assert(frame.Rotation == model.WorldPivot.Rotation)
            near(model:GetExtentsSize(), Vector3.new(6,4,5.2))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetPrimaryPivotOrientationCase() =>
        ("aligns Model bounds to PrimaryPart pivot including PivotOffset rotation", Near + """
            local model = Instance.new("Model")
            local part = Instance.new("Part", model)
            part.Position = Vector3.new(3,5,7); part.Size = Vector3.new(2,4,6)
            part.PivotOffset = CFrame.new(100,200,300, 0,-1,0, 1,0,0, 0,0,1)
            model.WorldPivot = CFrame.Angles(0,math.pi/4,0)
            model.PrimaryPart = part
            local frame, size = model:GetBoundingBox()
            near(frame.Position, Vector3.new(3,5,7)); near(size, Vector3.new(4,2,6))
            assert(frame.RightVector == Vector3.yAxis and frame.UpVector == -Vector3.xAxis)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetUpdatedBoundsCase() =>
        ("recomputes Model bounds after descendant geometry and hierarchy edits", Near + """
            local model = Instance.new("Model")
            local a = Instance.new("Part", model); a.Size = Vector3.new(2,2,2)
            local _, firstSize = model:GetBoundingBox(); near(firstSize, Vector3.new(2,2,2))
            local nested = Instance.new("Model", model)
            local b = Instance.new("Part", nested); b.Size = Vector3.new(2,2,2); b.Position = Vector3.new(10,0,0)
            local frame, size = model:GetBoundingBox()
            near(frame.Position, Vector3.new(5,0,0)); near(size, Vector3.new(12,2,2))
            a.Size = Vector3.new(4,2,2)
            frame, size = model:GetBoundingBox()
            near(frame.Position, Vector3.new(4.5,0,0)); near(size, Vector3.new(13,2,2))
            nested.Parent = nil
            frame, size = model:GetBoundingBox()
            near(frame.Position, Vector3.zero); near(size, Vector3.new(4,2,2))
            nested.Parent = model; b:Destroy()
            near(model:GetExtentsSize(), Vector3.new(4,2,2))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetReadOnlyCase() =>
        ("keeps automatic Model pivots unfrozen and emits no changes when reading bounds", Near + """
            local model = Instance.new("Model")
            local a = Instance.new("Part", model); a.Position = Vector3.new(-5,0,0)
            local b = Instance.new("Part", model); b.Position = Vector3.new(5,0,0)
            local changes = 0
            model.Changed:Connect(function() changes += 1 end)
            a.Changed:Connect(function() changes += 1 end)
            model:GetBoundingBox(); model:GetExtentsSize()
            assert(changes == 0)
            b.Position = Vector3.new(15,0,0)
            near(model:GetPivot().Position, Vector3.new(5,0,0))
            near(a.Position, Vector3.new(-5,0,0))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetEmptyAndReceiverCase() =>
        ("returns empty Model bounds by convention and restricts bounds methods to Models", """
            local model = Instance.new("Model")
            model.WorldPivot = CFrame.new(3,5,7) * CFrame.Angles(0,math.pi/2,0)
            local frame, size = model:GetBoundingBox()
            assert(frame == model:GetPivot() and size == Vector3.zero and model:GetExtentsSize() == Vector3.zero)
            assert(not pcall(function() Instance.new("Part"):GetBoundingBox() end))
            assert(not pcall(function() Instance.new("Folder"):GetExtentsSize() end))
            assert(not pcall(function() model.GetBoundingBox(Instance.new("Part")) end))
            workspace.WorldPivot = CFrame.identity
            local part = Instance.new("Part", workspace); part.Size = Vector3.new(2,4,6)
            local worldFrame, worldSize = workspace:GetBoundingBox()
            assert(worldFrame == CFrame.identity and worldSize == Vector3.new(2,4,6))
            """, 0, "", "");
}
