static class SkyCases
{
    internal static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return ("Sky under Lighting exposes six faces orientation signals and independent clones", """
            local lighting = game:GetService("Lighting")
            assert(lighting == game:GetService("Lighting") and lighting:IsA("Instance"))
            local sky = Instance.new("Sky", lighting)
            local changed = 0
            sky:GetPropertyChangedSignal("SkyboxFt"):Connect(function() changed += 1 end)
            sky.SkyboxFt = "rbxasset://textures/sky/front.png"
            sky.SkyboxFt = "rbxasset://textures/sky/front.png"
            assert(changed == 1 and sky.SkyboxFt == "rbxasset://textures/sky/front.png")
            for _, name in {"SkyboxBk", "SkyboxDn", "SkyboxLf", "SkyboxRt", "SkyboxUp"} do
                sky[name] = name .. ".png"
                assert(sky[name] == name .. ".png")
            end
            sky.SkyboxOrientation = Vector3.new(20, 45, -10)
            local clone = sky:Clone()
            sky.SkyboxFt = "different.png"
            assert(clone.SkyboxFt == "rbxasset://textures/sky/front.png")
            assert(clone.SkyboxOrientation == Vector3.new(20, 45, -10))
            assert(not pcall(function() sky.SkyboxFt = 12 end))
            assert(not pcall(function() sky.SkyboxOrientation = Vector3.new(0/0, 0, 0) end))
            """, 0, "", "");
    }
}
