static class PartPivotCases
{
    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return GetOffsetCase();
        yield return GetMovementCase();
        yield return GetNotificationsCase();
        yield return GetValidationCase();
        yield return GetCloneCase();
        yield return GetDescendantsCase();
        yield return GetRecursiveMovementCase();
    }

    private static (string, string?, int, string, string) GetOffsetCase() =>
        ("computes Part pivot from local PivotOffset without moving geometry", """
            local part = Instance.new("Part")
            assert(part.PivotOffset == CFrame.identity and part:GetPivot() == CFrame.identity)
            part.CFrame = CFrame.new(10,20,30, 0,-1,0, 1,0,0, 0,0,1)
            part.PivotOffset = CFrame.new(2,3,4)
            assert(part.Position == Vector3.new(10,20,30))
            assert(part:GetPivot().Position == Vector3.new(7,22,34))
            assert(part:GetPivot().RightVector == Vector3.yAxis)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetMovementCase() =>
        ("moves Part CFrame to place its offset pivot at the target", """
            local part = Instance.new("Part")
            part.Anchored = true
            part.PivotOffset = CFrame.new(2,0,0)
            local target = CFrame.new(10,20,30, 0,-1,0, 1,0,0, 0,0,1)
            part:PivotTo(target)
            assert(part:GetPivot() == target)
            assert(part.Position == Vector3.new(10,18,30))
            assert(part.CFrame.RightVector == Vector3.yAxis)
            assert(part.PivotOffset == CFrame.new(2,0,0) and part.Anchored)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetNotificationsCase() =>
        ("publishes pivot changes and only CFrame notifications during PivotTo", """
            local part = Instance.new("Part")
            local frames, positions, offsets = 0, 0, 0
            local changed = {}
            part.Changed:Connect(function(property) table.insert(changed, property) end)
            part:GetPropertyChangedSignal("CFrame"):Connect(function() frames += 1 end)
            part:GetPropertyChangedSignal("Position"):Connect(function() positions += 1 end)
            part:GetPropertyChangedSignal("PivotOffset"):Connect(function() offsets += 1 end)
            part.PivotOffset = CFrame.new(1,0,0)
            part.PivotOffset = CFrame.new(1,0,0)
            assert(offsets == 1 and frames == 0 and positions == 0)
            part:PivotTo(CFrame.new(5,0,0))
            assert(frames == 1 and positions == 0 and part.Position == Vector3.new(4,0,0))
            part:PivotTo(CFrame.new(5,0,0))
            assert(frames == 1 and positions == 0)
            assert(table.concat(changed, ",") == "PivotOffset,CFrame")
            part.Position = Vector3.new(6,0,0)
            assert(frames == 2 and positions == 1)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetValidationCase() =>
        ("rejects invalid pivot types and nonspatial receivers", """
            local part = Instance.new("Part")
            local changes = 0
            part.Changed:Connect(function() changes += 1 end)
            for _, value in {{}, Vector3.zero, 3, false, "bad", workspace} do
                assert(not pcall(function() part.PivotOffset = value end))
                assert(not pcall(function() part:PivotTo(value) end))
            end
            assert(not pcall(function() part:PivotTo(nil) end))
            assert(not pcall(function() part.PivotOffset = nil end))
            local folder = Instance.new("Folder")
            assert(not pcall(function() folder:GetPivot() end))
            assert(not pcall(function() folder:PivotTo(CFrame.identity) end))
            assert(not pcall(function() folder.PivotOffset = CFrame.identity end))
            assert(not pcall(function() folder:GetPropertyChangedSignal("PivotOffset") end))
            assert(part.CFrame == CFrame.identity and part.PivotOffset == CFrame.identity and changes == 0)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetCloneCase() =>
        ("clones Part PivotOffset independently", """
            local part = Instance.new("Part")
            part.CFrame = CFrame.new(5,6,7)
            part.PivotOffset = CFrame.new(1,2,3)
            local clone = part:Clone()
            assert(clone.PivotOffset == CFrame.new(1,2,3))
            clone:PivotTo(CFrame.new(10,20,30))
            assert(clone.Position == Vector3.new(9,18,27))
            assert(part.Position == Vector3.new(5,6,7) and part:GetPivot().Position == Vector3.new(6,8,10))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetDescendantsCase() =>
        ("moves spatial descendants when pivoting a Part", """
            local part = Instance.new("Part")
            local child = Instance.new("Part", part)
            child.Position = Vector3.new(2,0,0)
            local model = Instance.new("Model", part)
            model.WorldPivot = CFrame.new(0,4,0)
            local nested = Instance.new("Part", model)
            nested.Position = Vector3.new(0,6,0)
            part:PivotTo(CFrame.new(10,0,0, 0,-1,0, 1,0,0, 0,0,1))
            assert(part.Position == Vector3.new(10,0,0))
            assert(child.Position == Vector3.new(10,2,0))
            assert(model:GetPivot().Position == Vector3.new(6,0,0))
            assert(nested.Position == Vector3.new(4,0,0))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetRecursiveMovementCase() =>
        ("preserves a nested Part pivot operation when its callback resumes the outer move", """
            local part = Instance.new("Part")
            local child = Instance.new("Part", part); child.Position = Vector3.new(2,0,0)
            local nestedPosition
            part:GetPropertyChangedSignal("CFrame"):Once(function()
                part:PivotTo(CFrame.new(100,0,0))
                nestedPosition = child.Position
            end)
            part:PivotTo(CFrame.new(10,0,0))
            assert(part.Position == Vector3.new(100,0,0))
            assert(nestedPosition == Vector3.new(92,0,0))
            assert(child.Position == nestedPosition)
            """, 0, "", "");
}
