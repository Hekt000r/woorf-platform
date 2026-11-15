static class InstanceEventCases
{
    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return GetChildCase();
        yield return GetAncestryCase();
        yield return GetDescendantCase();
        yield return GetRejectedWriteCase();
        yield return GetDestroyCase();
        yield return GetDestroyReentrancyCase();
        yield return GetOverlappingRemovalCase();
        yield return GetDestroyedDuringMoveCase();
        yield return GetDestroyedDuringDescendantAddedCase();
    }

    private static (string, string?, int, string, string) GetChildCase() =>
        ("fires child and Parent events with updated hierarchy", """
            local a, b, child = Instance.new("Folder"), Instance.new("Folder"), Instance.new("Folder")
            local addedA, removedA, addedB, changed = 0, 0, 0, 0
            a.ChildAdded:Connect(function(value)
                assert(value == child and child.Parent == a and a:GetChildren()[1] == child)
                addedA += 1
            end)
            a.ChildRemoved:Connect(function(value)
                assert(value == child and child.Parent == b and #a:GetChildren() == 0)
                removedA += 1
            end)
            b.ChildAdded:Connect(function(value) assert(value == child and child.Parent == b); addedB += 1 end)
            child:GetPropertyChangedSignal("Parent"):Connect(function(...) assert(select("#", ...) == 0); changed += 1 end)
            child.Parent = a; child.Parent = a; child.Parent = b
            assert(addedA == 1 and removedA == 1 and addedB == 1 and changed == 2)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetAncestryCase() =>
        ("notifies descendants when an ancestor changes parent", """
            local parent = Instance.new("Folder", workspace)
            local child = Instance.new("Folder", parent)
            local destination = Instance.new("Folder", workspace)
            local parentCalls, childCalls, childParentChanges = 0, 0, 0
            parent.AncestryChanged:Connect(function(...)
                local moved, newParent = ...
                assert(select("#", ...) == 2)
                assert(moved == parent and newParent == destination and parent.Parent == destination)
                parentCalls += 1
            end)
            child.AncestryChanged:Connect(function(...)
                local moved, newParent = ...
                assert(select("#", ...) == 2)
                assert(moved == parent and newParent == destination and child.Parent == parent)
                childCalls += 1
            end)
            child:GetPropertyChangedSignal("Parent"):Connect(function() childParentChanges += 1 end)
            parent.Parent = destination
            parent.Parent = destination
            destination = nil
            parent.Parent = nil
            assert(parentCalls == 2 and childCalls == 2 and childParentChanges == 0)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetDestroyCase() =>
        ("fires Destroying before detachment and disconnects subtree signals", """
            local folder = Instance.new("Folder", workspace)
            local child = Instance.new("Folder", folder)
            local destroying = 0
            local rootConnection = folder.Destroying:Connect(function(...)
                assert(select("#", ...) == 0 and folder.Parent == workspace and child.Parent == folder)
                destroying += 1
            end)
            local childConnection = child.Destroying:Connect(function(...)
                assert(select("#", ...) == 0 and folder.Parent == workspace and child.Parent == folder)
                destroying += 1
            end)
            local changed = 0
            local propertyConnection = child.Changed:Connect(function() changed += 1 end)
            folder:Destroy(); folder:Destroy()
            assert(destroying == 2 and folder.Parent == nil and child.Parent == nil)
            assert(not rootConnection.Connected and not childConnection.Connected and not propertyConnection.Connected)
            local previous = changed
            child.Name = "After destruction"
            assert(changed == previous)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetDestroyReentrancyCase() =>
        ("guards reentrant destruction and preserves clone signal independence", """
            local folder = Instance.new("Folder", workspace)
            local calls = 0
            folder.Destroying:Connect(function() calls += 1; folder:Destroy() end)
            folder:Destroy()
            assert(calls == 1 and folder.Parent == nil)
            local source = Instance.new("Folder")
            local sourceCalls = 0
            source.Changed:Connect(function() sourceCalls += 1 end)
            local clone = source:Clone()
            clone.Name = "Clone"
            assert(sourceCalls == 0 and clone.Changed ~= source.Changed)
            source.Name = "Source"
            assert(sourceCalls == 1)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetDescendantCase() =>
        ("fires subtree descendant events around the parent change", """
            local a, b = Instance.new("Folder"), Instance.new("Folder")
            local moved = Instance.new("Folder", a)
            local child = Instance.new("Folder", moved)
            local removed, added = {}, {}
            a.DescendantRemoving:Connect(function(value)
                assert(moved.Parent == a and child.Parent == moved and value:IsDescendantOf(a))
                assert(not pcall(function() value.Parent = nil end))
                table.insert(removed, value)
            end)
            b.DescendantAdded:Connect(function(value)
                assert(moved.Parent == b and child.Parent == moved and value:IsDescendantOf(b))
                table.insert(added, value)
            end)
            moved.Parent = b
            assert(#removed == 2 and removed[1] == moved and removed[2] == child)
            assert(#added == 2 and table.find(added, moved) and table.find(added, child))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetRejectedWriteCase() =>
        ("does not publish changes for rejected property writes", """
            local folder = Instance.new("Folder", workspace)
            local child = Instance.new("Folder", folder)
            local calls = 0
            folder.Changed:Connect(function() calls += 1 end)
            folder.AncestryChanged:Connect(function() calls += 1 end)
            folder.ChildRemoved:Connect(function() calls += 1 end)
            assert(not pcall(function() folder.Parent = child end))
            assert(not pcall(function() folder.Name = {} end))
            assert(not pcall(function() folder.Archivable = 1 end))
            assert(calls == 0 and folder.Parent == workspace and child.Parent == folder)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetOverlappingRemovalCase() =>
        ("rejects overlapping ancestor moves during descendant removal", """
            local root = Instance.new("Folder", workspace)
            local moved = Instance.new("Folder", root)
            local descendant = Instance.new("Folder", moved)
            local attempted, succeeded = false, nil
            workspace.DescendantRemoving:Connect(function(value)
                if value == moved and not attempted then
                    attempted = true
                    succeeded = pcall(function() root.Parent = nil end)
                end
            end)
            moved.Parent = nil
            assert(attempted and succeeded == false)
            assert(root.Parent == workspace and moved.Parent == nil and descendant.Parent == moved)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetDestroyedDuringMoveCase() =>
        ("does not announce stale additions after removal callback destroys a child", """
            local old, destination = Instance.new("Folder", workspace), Instance.new("Folder", workspace)
            local child = Instance.new("Folder", old)
            local additions = 0
            old.ChildRemoved:Connect(function(value) assert(value == child); value:Destroy() end)
            destination.ChildAdded:Connect(function(value)
                assert(value.Parent == destination)
                additions += 1
            end)
            child.Parent = destination
            assert(child.Parent == nil and #destination:GetChildren() == 0 and additions == 0)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetDestroyedDuringDescendantAddedCase() =>
        ("skips stale descendant additions after a callback destroys the subtree", """
            local destination = Instance.new("Folder")
            local root = Instance.new("Folder")
            local child = Instance.new("Folder", root)
            local calls = 0
            destination.DescendantAdded:Connect(function(value)
                assert(value:IsDescendantOf(destination))
                calls += 1
                if value == root then root:Destroy() end
            end)
            root.Parent = destination
            assert(calls == 1 and root.Parent == nil and child.Parent == nil)
            """, 0, "", "");
}
