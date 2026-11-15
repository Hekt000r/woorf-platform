static class ModelPivotCases
{
    private const string Near = """
        local function near(a,b) assert((a-b).Magnitude < 0.00001, tostring(a).." differs from "..tostring(b)) end
        
        """;

    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return GetAutomaticPivotCase();
        yield return GetPrimaryPartCase();
        yield return GetMovementCase();
        yield return GetCacheCase();
        yield return GetCloneCase();
        yield return GetWorkspaceCase();
        yield return GetCallbackEditCase();
        yield return GetRecursiveMovementCase();
    }

    private static (string, string?, int, string, string) GetAutomaticPivotCase() =>
        ("derives initial Model pivot from oriented part bounds then fixes explicit pivots", Near + """
            local model = Instance.new("Model")
            assert(model.PrimaryPart == nil and model:GetPivot() == CFrame.identity)
            local a = Instance.new("Part", model); a.Size = Vector3.new(2,2,2); a.Position = Vector3.new(-4,0,0)
            local b = Instance.new("Part", model); b.Size = Vector3.new(6,2,2); b.Position = Vector3.new(4,0,0)
            near(model.WorldPivot.Position, Vector3.new(1,0,0))
            b.CFrame = CFrame.new(4,4,0) * CFrame.Angles(0,0,math.pi/2)
            near(model:GetPivot().Position, Vector3.new(0,3,0))
            b.Position = Vector3.new(8,4,0)
            near(model:GetPivot().Position, Vector3.new(2,3,0))
            model.WorldPivot = CFrame.new(20,0,0)
            assert(a.Position == Vector3.new(-4,0,0))
            a.Position = Vector3.new(100,0,0)
            assert(model:GetPivot() == CFrame.new(20,0,0))
            local empty = Instance.new("Model")
            empty:PivotTo(CFrame.new(5,0,0))
            local part = Instance.new("Part", empty); part.Position = Vector3.new(100,0,0)
            assert(empty:GetPivot() == CFrame.new(5,0,0))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetPrimaryPartCase() =>
        ("follows PrimaryPart pivot and clears removed or destroyed primary references", """
            local model = Instance.new("Model")
            model.WorldPivot = CFrame.new(99,0,0)
            local folder = Instance.new("Folder", model)
            local part = Instance.new("Part", folder)
            part.Position = Vector3.new(3,0,0); part.PivotOffset = CFrame.new(2,0,0)
            model.PrimaryPart = part
            assert(model:GetPivot() == CFrame.new(5,0,0))
            assert(model.WorldPivot == CFrame.new(99,0,0))
            part.Position = Vector3.new(10,0,0)
            assert(model:GetPivot() == CFrame.new(12,0,0))
            assert(not pcall(function() model.PrimaryPart = folder end))
            assert(model.PrimaryPart == part)
            folder.Parent = nil
            assert(model.PrimaryPart == nil and model:GetPivot() == CFrame.new(99,0,0))
            local external = Instance.new("Part")
            model.PrimaryPart = external
            assert(model.PrimaryPart == external)
            external:Destroy()
            assert(model.PrimaryPart == nil)
            assert(not pcall(function() model.WorldPivot = Vector3.zero end))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetMovementCase() =>
        ("moves descendant Parts and nested Model pivots with a shared rigid transform", Near + """
            local model = Instance.new("Model"); model.WorldPivot = CFrame.identity
            local a = Instance.new("Part", model); a.Position = Vector3.new(1,0,0)
            local nested = Instance.new("Model", model); nested.WorldPivot = CFrame.new(5,0,0)
            local b = Instance.new("Part", nested); b.Position = Vector3.new(0,2,0)
            local cframeChanges, positionChanges = 0,0
            a.Changed:Connect(function(property)
                if property == "CFrame" then cframeChanges += 1 end
                if property == "Position" then positionChanges += 1 end
            end)
            local target = CFrame.new(10,0,0) * CFrame.Angles(0,0,math.pi/2)
            model:PivotTo(target)
            near(a.Position, Vector3.new(10,1,0)); near(b.Position, Vector3.new(8,0,0))
            near(nested.WorldPivot.Position, Vector3.new(10,5,0))
            assert(model:GetPivot() == target and cframeChanges == 1 and positionChanges == 0)
            assert(not pcall(function() model:PivotTo(Vector3.zero) end))
            assert(not pcall(function() Instance.new("Folder"):GetPivot() end))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetCacheCase() =>
        ("preserves Model part offsets across repeated pivots and accepts external edits", """
            local model = Instance.new("Model"); model.WorldPivot = CFrame.identity
            local part = Instance.new("Part", model)
            local original = CFrame.new(1.234,2.345,3.456)
            part.CFrame = original
            for i = 1,200 do
                model:PivotTo(CFrame.new(i,0,0) * CFrame.Angles(0,i/100,0))
                model:PivotTo(CFrame.identity)
            end
            assert(part.CFrame == original)
            part.Position = Vector3.new(5,6,7)
            model:PivotTo(CFrame.new(2,0,0))
            assert(part.Position == Vector3.new(7,6,7))
            local extra = Instance.new("Part", model); extra.Position = Vector3.new(20,0,0)
            model:PivotTo(CFrame.new(4,0,0))
            assert(extra.Position == Vector3.new(22,0,0))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetCloneCase() =>
        ("remaps cloned Model PrimaryPart references within the cloned hierarchy", """
            local model = Instance.new("Model"); model.WorldPivot = CFrame.new(9,8,7)
            local nested = Instance.new("Model", model)
            local part = Instance.new("Part", nested); part.Name = "Root"; part.PivotOffset = CFrame.new(2,0,0)
            model.PrimaryPart = part; nested.PrimaryPart = part
            local clone = model:Clone()
            local copied = clone:FindFirstChild("Root", true)
            assert(copied ~= part and clone.PrimaryPart == copied and clone.Model.PrimaryPart == copied)
            assert(clone.WorldPivot == CFrame.new(9,8,7))
            clone:PivotTo(CFrame.new(20,0,0))
            assert(copied.Position == Vector3.new(18,0,0) and part.Position == Vector3.zero)
            local external = Instance.new("Part")
            model.PrimaryPart = external
            assert(model:Clone().PrimaryPart == external)
            part.Archivable = false; model.PrimaryPart = part
            local excluded = model:Clone()
            assert(excluded.PrimaryPart == part and excluded:FindFirstChild("Root", true) == nil)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetWorkspaceCase() =>
        ("exposes inherited Model pivot APIs on Workspace", """
            assert(workspace.PrimaryPart == nil)
            workspace.WorldPivot = CFrame.identity
            local part = Instance.new("Part", workspace); part.Position = Vector3.new(2,0,0)
            workspace:PivotTo(CFrame.new(3,0,0))
            assert(part.Position == Vector3.new(5,0,0) and workspace:GetPivot() == CFrame.new(3,0,0))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetCallbackEditCase() =>
        ("preserves callback edits when rebuilding Model pivot offsets", """
            local model = Instance.new("Model"); model.WorldPivot = CFrame.identity
            local part = Instance.new("Part", model); part.Position = Vector3.xAxis
            part:GetPropertyChangedSignal("CFrame"):Once(function() part.Position = Vector3.new(50,0,0) end)
            model:PivotTo(CFrame.new(10,0,0))
            assert(part.Position == Vector3.new(50,0,0))
            model:PivotTo(CFrame.new(20,0,0))
            assert(part.Position == Vector3.new(60,0,0))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetRecursiveMovementCase() =>
        ("allows a recursive Model pivot after a movement callback removes a part", """
            local model = Instance.new("Model"); model.WorldPivot = CFrame.identity
            local a = Instance.new("Part", model); a.Position = Vector3.xAxis
            local b = Instance.new("Part", model); b.Position = Vector3.new(2,0,0)
            b:GetPropertyChangedSignal("CFrame"):Once(function()
                a:Destroy()
                model:PivotTo(CFrame.new(30,0,0))
            end)
            model:PivotTo(CFrame.new(10,0,0))
            assert(model:GetPivot() == CFrame.new(30,0,0) and #model:GetChildren() == 1)
            """, 0, "", "");
}
