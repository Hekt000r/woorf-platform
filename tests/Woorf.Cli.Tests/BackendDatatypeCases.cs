static class BackendDatatypeCases
{
    internal static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return ("Color3 converts HSV colors and preserves hue saturation and value", """
            local red = Color3.fromHSV(0, 1, 1)
            assert(red == Color3.new(1, 0, 0))
            local hue, saturation, value = red:ToHSV()
            assert(hue == 0 and saturation == 1 and value == 1)
            local green = Color3.fromHSV(1/3, 1, 1)
            assert(green.G == 1 and green.R == 0 and green.B == 0)
            print("ok")
            """, 0, "ok\n", "");
        yield return ("Random produces seeded normalized unit vectors", """
            local random = Random.new(456)
            local previous
            for index = 1, 20 do
                local value = random:NextUnitVector()
                assert(typeof(value) == "Vector3" and math.abs(value.Magnitude - 1) < 0.00001 and value ~= previous)
                previous = value
            end
            print("ok")
            """, 0, "ok\n", "");
        foreach (var (name, source) in new[]
        {
            ("Vector3Value", "Vector3.new(1, 2, 3)"),
            ("CFrameValue", "CFrame.new(4, 5, 6)"),
            ("Color3Value", "Color3.new(0.1, 0.2, 0.3)"),
        })
        {
            yield return ($"stores typed {name} values with value-bearing signals", $$"""
                local holder = Instance.new("{{name}}")
                local expected = {{source}}
                local received
                holder.Changed:Connect(function(value) received = value end)
                holder.Value = expected
                assert(holder.Value == expected and received == expected)
                assert(holder:Clone().Value == expected)
                assert(not pcall(function() holder.Value = "wrong" end))
                print("ok")
                """, 0, "ok\n", "");
        }
    }
}
