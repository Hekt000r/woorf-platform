static class PartCFrameCases
{
    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return GetDefaultsCase();
        yield return GetSynchronizationCase();
        yield return GetNotificationsCase();
        yield return GetValidationCase();
        yield return GetCloneCase();
        yield return GetOrthonormalizationCase();
    }

    private static (string, string?, int, string, string) GetDefaultsCase() =>
        ("starts Parts with identity CFrame", """
            local part = Instance.new("Part")
            assert(typeof(part.CFrame) == "CFrame")
            assert(part.CFrame.Position == Vector3.zero)
            assert(part.CFrame.RightVector == Vector3.xAxis)
            assert(part.CFrame.UpVector == Vector3.yAxis)
            assert(part.CFrame.LookVector == Vector3.new(0, 0, -1))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetSynchronizationCase() =>
        ("synchronizes Part Position and CFrame while retaining rotation", """
            local part = Instance.new("Part")
            local cf = CFrame.new(2, 3, 4) * CFrame.Angles(0, math.pi / 2, 0)
            part.CFrame = cf
            assert(part.Position == Vector3.new(2, 3, 4))
            assert((part.CFrame.LookVector - Vector3.new(-1, 0, 0)).Magnitude < 1e-5)
            local storedRotation = part.CFrame.Rotation
            part.Position = Vector3.new(8, 9, 10)
            assert(part.CFrame.Position == Vector3.new(8, 9, 10))
            assert(part.CFrame.Rotation == storedRotation)
            assert(cf.Position == Vector3.new(2, 3, 4))
            part.Anchored = true
            part.CFrame = CFrame.new(5, 6, 7)
            assert(part.Position == Vector3.new(5, 6, 7) and part.Anchored)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetNotificationsCase() =>
        ("publishes coupled Part transform changes after updating state", """
            local part = Instance.new("Part")
            local frames, positions, changedFrames, changedPositions = 0, 0, 0, 0
            part.Changed:Connect(function(property)
                if property == "CFrame" then changedFrames += 1 end
                if property == "Position" then changedPositions += 1 end
                assert(part.Position == part.CFrame.Position)
            end)
            part:GetPropertyChangedSignal("CFrame"):Connect(function(...)
                assert(select("#", ...) == 0)
                frames += 1
            end)
            part:GetPropertyChangedSignal("Position"):Connect(function() positions += 1 end)
            part.CFrame = CFrame.new(1, 2, 3)
            assert(frames == 1 and positions == 1)
            part.CFrame = CFrame.new(1, 2, 3)
            part.Position = Vector3.new(1, 2, 3)
            assert(frames == 1 and positions == 1)
            part.CFrame = CFrame.new(1, 2, 3) * CFrame.Angles(0, 1, 0)
            assert(frames == 2 and positions == 1)
            part.Position = Vector3.new(4, 5, 6)
            assert(frames == 3 and positions == 2)
            assert(changedFrames == 3 and changedPositions == 2)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetValidationCase() =>
        ("rejects invalid CFrame writes without publishing changes", """
            local part = Instance.new("Part")
            local initial = part.CFrame
            local changes = 0
            part.Changed:Connect(function() changes += 1 end)
            for _, value in {Vector3.zero, {}, false, 4, "bad", workspace} do
                assert(not pcall(function() part.CFrame = value end))
            end
            assert(not pcall(function() part.CFrame = nil end))
            assert(not pcall(function() Instance.new("Folder").CFrame = initial end))
            assert(part.CFrame == initial and part.Position == Vector3.zero and changes == 0)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetCloneCase() =>
        ("clones Part orientation independently of original transform", """
            local part = Instance.new("Part")
            part.CFrame = CFrame.new(4, 5, 6) * CFrame.Angles(0.2, 0.4, 0.6)
            local before = part.CFrame
            local changes = 0
            part.Changed:Connect(function() changes += 1 end)
            local clone = part:Clone()
            assert(clone.CFrame == before and clone.Position == Vector3.new(4, 5, 6))
            clone.Position = Vector3.zero
            assert(clone.CFrame.Rotation == before.Rotation)
            clone.CFrame = CFrame.identity
            assert(part.CFrame == before and changes == 0)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetOrthonormalizationCase() =>
        ("orthonormalizes rotation matrices assigned to Part CFrame", """
            local part = Instance.new("Part")
            part.CFrame = CFrame.new(1,2,3, 2,0,0, 0,3,0, 0,0,4)
            assert(part.CFrame.Position == Vector3.new(1,2,3))
            assert(part.CFrame.RightVector == Vector3.xAxis)
            assert(part.CFrame.UpVector == Vector3.yAxis)
            assert(part.CFrame.ZVector == Vector3.zAxis)
            """, 0, "", "");
}
