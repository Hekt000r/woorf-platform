static class PartCases
{
    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return GetDefaultsCase();
        yield return GetRoundTripCase();
        yield return GetValidationCase();
        yield return GetSizeBoundsCase();
        yield return GetNotificationsCase();
        yield return GetCloneCase();
    }

    private static (string, string?, int, string, string) GetDefaultsCase() =>
        ("creates Part with spatial defaults and Roblox ancestry", """
            local part = Instance.new("Part", workspace)
            assert(part.ClassName == "Part" and part.Name == "Part" and part.Parent == workspace)
            for _, class in {"Part", "FormFactorPart", "BasePart", "PVInstance", "Instance", "Object"} do
                assert(part:IsA(class), class)
            end
            assert(not part:IsA("Model"))
            assert(not pcall(Instance.new, "BasePart"))
            assert(not pcall(Instance.new, "FormFactorPart"))
            assert(part.Position.X == 0 and part.Position.Y == 0 and part.Position.Z == 0)
            assert(part.Size.X == 4 and math.abs(part.Size.Y - 1.2) < 1e-6 and part.Size.Z == 2)
            assert(part.Anchored == false)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetRoundTripCase() =>
        ("round trips spatial values through managed Part properties", """
            local part = Instance.new("Part")
            part.Position = Vector3.new(3, 2, 6)
            part.Size = Vector3.new(8, 2, 6)
            part.Anchored = true
            assert(part.Position == Vector3.new(3, 2, 6))
            assert(part.Size == Vector3.new(8, 2, 6) and part.Anchored)
            local snapshot = part.Position
            part.Position += Vector3.xAxis
            assert(snapshot == Vector3.new(3, 2, 6) and part.Position == Vector3.new(4, 2, 6))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetValidationCase() =>
        ("rejects invalid Part writes without changing state", """
            local part = Instance.new("Part")
            for _, value in {false, 3, "bad", {}, workspace} do
                assert(not pcall(function() part.Position = value end))
                assert(not pcall(function() part.Size = value end))
            end
            assert(not pcall(function() part.Position = nil end))
            assert(not pcall(function() part.Size = nil end))
            assert(not pcall(function() part.Anchored = 1 end))
            assert(part.Position == Vector3.zero and not part.Anchored)
            assert(part.Size == Vector3.new(4, 1.2, 2))
            local folder = Instance.new("Folder")
            assert(not pcall(function() folder.Position = Vector3.zero end))
            assert(not pcall(function() return folder.Size end))
            assert(not pcall(function() folder:GetPropertyChangedSignal("Position") end))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetSizeBoundsCase() =>
        ("clamps Part size components to Roblox bounds", """
            local part = Instance.new("Part")
            part.Size = Vector3.new(-2, 0, 3000)
            assert(part.Size == Vector3.new(0.001, 0.001, 2048))
            part.Size = Vector3.new(0.01, 0.025, 12)
            assert(part.Size == Vector3.new(0.01, 0.025, 12))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetNotificationsCase() =>
        ("publishes spatial changes only after effective value changes", """
            local part = Instance.new("Part")
            local changed, positions, sizes, anchors = {}, 0, 0, 0
            part.Changed:Connect(function(property) table.insert(changed, property) end)
            part:GetPropertyChangedSignal("Position"):Connect(function(...)
                assert(select("#", ...) == 0 and part.Position == Vector3.new(1, 2, 3))
                positions += 1
            end)
            part:GetPropertyChangedSignal("Size"):Connect(function() sizes += 1 end)
            part:GetPropertyChangedSignal("Anchored"):Connect(function() anchors += 1 end)
            part.Position = Vector3.new(1, 2, 3)
            part.Position = Vector3.new(1, 2, 3)
            part.Size = Vector3.new(0, 0, 3000)
            part.Size = Vector3.new(-1, -2, 4000)
            part.Anchored = true; part.Anchored = true
            assert(not pcall(function() part.Size = {} end))
            assert(positions == 1 and sizes == 1 and anchors == 1)
            assert(table.concat(changed, ",") == "CFrame,Position,Size,Anchored")
            """, 0, "", "");

    private static (string, string?, int, string, string) GetCloneCase() =>
        ("clones Part state independently through an Instance subtree", """
            local model = Instance.new("Model", workspace)
            local part = Instance.new("Part", model)
            part.Position = Vector3.new(4, 5, 6)
            part.Size = Vector3.new(7, 8, 9)
            part.Anchored = true
            local calls = 0
            part.Changed:Connect(function() calls += 1 end)
            local copy = model:Clone().Part
            assert(copy:IsA("BasePart") and copy.Position == Vector3.new(4, 5, 6))
            assert(copy.Size == Vector3.new(7, 8, 9) and copy.Anchored)
            copy.Position = Vector3.zero; copy.Size = Vector3.one; copy.Anchored = false
            assert(part.Position == Vector3.new(4, 5, 6) and part.Anchored and calls == 0)
            assert(part.Size == Vector3.new(7, 8, 9))
            """, 0, "", "");
}

