using Woorf.Engine.DataModel;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Scripting;

internal static class CameraTests
{
    internal static int Run()
    {
        var game = new DataModel();
        using var session = new LuauSession(game, new MonotonicClock());
        var status = session.Execute("assert(workspace.CurrentCamera == nil)", "server-camera");
        if (status != 0) return status;
        ((RunService)game.GetService("RunService")).IsClientRuntime = true;
        return session.Execute("""
            local original = workspace.CurrentCamera
            assert(original:IsA("Camera") and original.Parent == workspace)
            assert(original.ViewportSize == Vector2.new(1, 1))
            assert(not pcall(function() original.ViewportSize = Vector2.new(3, 4) end))
            local replacement = Instance.new("Camera", workspace)
            local changes = 0
            workspace:GetPropertyChangedSignal("CurrentCamera"):Connect(function() changes += 1 end)
            workspace.CurrentCamera = replacement
            assert(workspace.CurrentCamera == replacement and original.Parent == nil)
            replacement.CameraType = Enum.CameraType.Scriptable
            replacement.CFrame = CFrame.new(4, 5, 6)
            replacement.FieldOfView = 140
            assert(replacement.CameraType == Enum.CameraType.Scriptable)
            assert(replacement.CFrame.Position == Vector3.new(4, 5, 6))
            assert(replacement.FieldOfView == 120)
            replacement:Destroy()
            assert(workspace.CurrentCamera ~= replacement and changes == 2)
            """, "client-camera");
    }
}
