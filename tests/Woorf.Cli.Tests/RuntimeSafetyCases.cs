static class RuntimeSafetyCases
{
    internal static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return ("execution budget interrupts an infinite startup loop", "while true do end", 1, "", "Execution budget exceeded");
        yield return ("execution budget cannot be bypassed with protected calls", """
            while true do pcall(function() while true do end end) end
            """, 1, "", "Execution budget exceeded");
        yield return ("execution budget interrupts a delayed callback", """
            task.delay(0.01, function() while true do end end)
            """, 1, "", "Execution budget exceeded");
        yield return ("execution budget interrupts a property callback", """
            local part = Instance.new("Part", workspace)
            part.Changed:Connect(function() while true do end end)
            part.Name = "changed"
            """, 1, "", "Execution budget exceeded");
        yield return ("execution budget reports a runaway shutdown callback", """
            game:BindToClose(function() while true do end end)
            """, 1, "", "Execution budget exceeded");
        yield return ("execution budget reports a runaway yielding shutdown callback", """
            game:BindToClose(function() task.wait(0.01); while true do end end)
            """, 1, "", "Execution budget exceeded");
        yield return ("atomic updates commit cancel and roll back errors", """
            local store = game:GetService("DataStoreService"):GetDataStore("UpdateTests")
            store:RemoveAsync("profile")
            local value = store:UpdateAsync("profile", function(old)
                assert(old == nil)
                return {Coins = 10}
            end)
            assert(value.Coins == 10)
            assert(store:UpdateAsync("profile", function(old) old.Coins = 99; return nil end) == nil)
            assert(store:GetAsync("profile").Coins == 10)
            assert(not pcall(function() store:UpdateAsync("profile", function() error("abort") end) end))
            assert(not pcall(function() store:UpdateAsync("profile", function() task.wait() end) end))
            assert(not pcall(function() store:UpdateAsync("profile", function() return workspace end) end))
            assert(not pcall(function() store:UpdateAsync("profile", function() store:GetAsync("profile"); return 0 end) end))
            local elapsed = task.wait(0.06)
            assert(elapsed >= 0.05, "failed transform left a timer")
            assert(not pcall(function() store:UpdateAsync("profile", function() return 1, 2 end) end))
            assert(not pcall(function() store:UpdateAsync("profile", function() end) end))
            assert(store:GetAsync("profile").Coins == 10)
            assert(store:UpdateAsync("profile", function(old) return {Coins = old.Coins + 5} end).Coins == 15)
            store:RemoveAsync("profile")
            print("updated")
            """, 0, "updated\n", "");
    }
}
