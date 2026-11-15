static class HierarchyCases
{
    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return GetServiceRootCase();
        yield return GetDataModelCase();
        yield return GetHierarchyCase();
        yield return GetValidationCase();
        yield return GetLifecycleCase();
    }

    private static (string, string?, int, string, string) GetServiceRootCase() =>
        ("keeps service roots and singleton references stable", """
            local storage = game:GetService("ServerStorage")
            for _, service in {workspace, storage} do
                local child = Instance.new("Folder", service)
                assert(not pcall(function() service.Parent = nil end))
                assert(not pcall(function() service.Parent = child end))
                assert(not pcall(function() service:Destroy() end))
                assert(service.Parent == game and child.Parent == service)
                assert(game:GetService(service.ClassName) == service)
            end
            assert(game.Workspace == workspace)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetDataModelCase() =>
        ("exposes DataModel and singleton services", """
            assert(typeof(game) == "Instance" and game.ClassName == "DataModel")
            assert(game.Parent == nil and game:IsA("ServiceProvider") and game:IsA("Instance"))
            assert(workspace == game.Workspace and workspace == game:GetService("Workspace"))
            assert(workspace:IsA("Model") and workspace:IsA("PVInstance"))
            assert(workspace.Parent == game and game:FindService("Workspace") == workspace)
            assert(game:FindService("ReplicatedStorage") == nil)
            local storage = game:GetService("ReplicatedStorage")
            storage.Name = "Shared"
            assert(game:GetService("ReplicatedStorage") == storage)
            assert(game:FindService("ReplicatedStorage") == storage and storage.Parent == game)
            assert(game:FindService("Folder") == nil)
            assert(not pcall(function() game:GetService("Folder") end))
            assert(not pcall(function() Instance.new("DataModel") end))
            assert(not pcall(function() Instance.new("Instance") end))
            assert(not pcall(function() Instance.new("Workspace") end))
            assert(not pcall(function() Instance.new("ImaginaryClass") end))
            assert(game:IsLoaded())
            """, 0, "", "");

    private static (string, string?, int, string, string) GetHierarchyCase() =>
        ("maintains hierarchy and lookup identity", """
            local a = Instance.new("Folder", workspace)
            local b = Instance.new("Folder", a)
            local c = Instance.new("Model", b)
            a.Name, b.Name, c.Name = "A", "B", "C"
            assert(tostring(a) == "A" and a.ClassName == "Folder" and a.Archivable)
            assert(a.Parent == workspace and a.B == b and a["B"] == b)
            assert(#a:GetChildren() == 1 and a:GetChildren()[1] == b)
            local snapshot = a:GetChildren(); snapshot[1] = nil
            assert(#a:GetChildren() == 1 and #a:GetDescendants() == 2)
            assert(a:FindFirstChild("C") == nil and a:FindFirstChild("C", true) == c)
            assert(a:FindFirstChildOfClass("Folder") == b)
            assert(a:FindFirstChildWhichIsA("PVInstance", true) == c)
            assert(c:FindFirstAncestor("A") == a)
            assert(c:FindFirstAncestorOfClass("Folder") == b)
            assert(c:FindFirstAncestorWhichIsA("DataModel") == game)
            assert(c:IsDescendantOf(a) and a:IsAncestorOf(c) and not a:IsAncestorOf(a))
            assert(c:GetFullName() == "Workspace.A.B.C")
            c.Parent = a
            assert(#b:GetChildren() == 0 and #a:GetChildren() == 2)
            c.Parent = nil
            assert(c.Parent == nil and not c:IsDescendantOf(game))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetValidationCase() =>
        ("rejects invalid writes without corrupting hierarchy", """
            local a = Instance.new("Folder", workspace)
            local b = Instance.new("Folder", a)
            assert(not pcall(function() a.Parent = b end))
            assert(not pcall(function() a.Parent = a end))
            assert(not pcall(function() a.Parent = {} end))
            assert(not pcall(function() a.Name = {} end))
            assert(not pcall(function() a.Archivable = 1 end))
            assert(not pcall(function() a.ClassName = "Model" end))
            assert(not pcall(function() a.Unknown = true end))
            assert(not pcall(function() return a.Unknown end))
            assert(not pcall(function() game.Parent = a end))
            assert(a.Parent == workspace and b.Parent == a and a.ClassName == "Folder")
            """, 0, "", "");

    private static (string, string?, int, string, string) GetLifecycleCase() =>
        ("clones archivable subtrees and locks destroyed objects", """
            local a = Instance.new("Folder", workspace)
            a.Name = "Original"
            local b = Instance.new("Configuration", a)
            local hidden = Instance.new("Folder", a); hidden.Archivable = false
            local clone = a:Clone()
            assert(clone ~= a and clone.Parent == nil and clone.Name == "Original")
            assert(#clone:GetChildren() == 1 and clone:GetChildren()[1].ClassName == "Configuration")
            assert(hidden:Clone() == nil)
            a:Destroy(); a:Destroy()
            assert(a.Parent == nil and b.Parent == nil and hidden.Parent == nil)
            assert(not pcall(function() b.Parent = workspace end))
            assert(not pcall(function() Instance.new("Folder", a) end))
            clone:ClearAllChildren()
            assert(#clone:GetChildren() == 0)
            """, 0, "", "");
}
