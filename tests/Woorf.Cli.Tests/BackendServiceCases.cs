static class BackendServiceCases
{
    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        foreach (var test in GetCoreCases()) yield return test;
        foreach (var test in GetLifecycleCases()) yield return test;
        foreach (var test in GetSpatialCases()) yield return test;
    }

    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCoreCases()
    {
        yield return ("Random maintains seeded independent state and validates ranges", """
            local first, second = Random.new(123), Random.new(123)
            assert(typeof(first) == "Random")
            for index = 1, 20 do assert(first:NextInteger(1, 10) == second:NextInteger(1, 10)) end
            local clone = first:Clone(); assert(first:NextNumber() == clone:NextNumber())
            assert(not pcall(function() first:NextInteger(10, 1) end))
            print("ok")
            """, 0, "ok\n", "");
        yield return ("HttpService JSON round trips objects arrays unicode and rejects cycles", """
            local http = game:GetService("HttpService")
            local decoded = http:JSONDecode(http:JSONEncode({Coins = 10, Text = "雪", Items = {1, false, "x"}}))
            assert(decoded.Coins == 10 and decoded.Text == "雪" and decoded.Items[2] == false)
            local cycle = {}; cycle.Self = cycle
            assert(not pcall(function() http:JSONEncode(cycle) end))
            assert(not pcall(function() http:JSONDecode("{bad") end))
            print("ok")
            """, 0, "ok\n", "");
        yield return ("Debris destroys scheduled instances without blocking", """
            local part = Instance.new("Part", workspace)
            game:GetService("Debris"):AddItem(part, 0.01)
            assert(part.Parent == workspace)
            task.wait(0.03)
            assert(part.Parent == nil)
            print("ok")
            """, 0, "ok\n", "");
        yield return ("Heartbeat supplies elapsed time and supports signal waits", """
            local run = game:GetService("RunService")
            local elapsed = run.Heartbeat:Wait()
            assert(type(elapsed) == "number" and elapsed > 0)
            print("ok")
            """, 0, "ok\n", "");
    }

    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetLifecycleCases()
    {
        yield return ("data stores preserve JSON values and scoped keys across handles", """
            local service = game:GetService("DataStoreService")
            local store = service:GetDataStore("BackendTests")
            local key = "roundtrip"
            store:SetAsync(key, {Coins = 10})
            assert(service:GetDataStore("BackendTests"):GetAsync(key).Coins == 10)
            local value = store:GetAsync(key); value.Coins = 99
            assert(store:GetAsync(key).Coins == 10)
            assert(store:IncrementAsync("counter", 2) ~= nil)
            assert(store:RemoveAsync(key).Coins == 10 and store:GetAsync(key) == nil)
            local first = service:GetDataStore("one\0two", "three")
            local second = service:GetDataStore("one", "two\0three")
            first:SetAsync("isolation", 1); second:SetAsync("isolation", 2)
            assert(first:GetAsync("isolation") == 1 and second:GetAsync("isolation") == 2)
            assert(not pcall(function() service:GetDataStore("name", 42) end))
            print("ok")
            """, 0, "ok\n", "");
        yield return ("BindToClose invokes yielding callbacks during shutdown", """
            game:BindToClose(function() task.wait(0.01); print("closed") end)
            print("running")
            """, 0, "running\nclosed\n", "");
        yield return ("shutdown waits for callbacks rather than unrelated heartbeat subscriptions", """
            game:BindToClose(function()
                game:GetService("RunService").Heartbeat:Connect(function() end)
                print("closed")
            end)
            """, 0, "closed\n", "");
        yield return ("Teams enumerate teams and their assigned players", """
            local teams = game:GetService("Teams")
            local team = Instance.new("Team", teams)
            team.Name = "Blue"
            assert(#teams:GetTeams() == 1 and teams:GetTeams()[1] == team)
            assert(#team:GetPlayers() == 0)
            print("ok")
            """, 0, "ok\n", "");
        yield return ("Humanoid clamps health and publishes damage death state", """
            local humanoid = Instance.new("Humanoid")
            assert(humanoid.Health == 100 and humanoid.MaxHealth == 100)
            local deaths = 0
            humanoid.Died:Connect(function() deaths += 1 end)
            humanoid:TakeDamage(25); assert(humanoid.Health == 75)
            humanoid:TakeDamage(100); assert(humanoid.Health == 0 and deaths == 1)
            humanoid:TakeDamage(1); assert(deaths == 1)
            assert(not pcall(function() humanoid.Health = "bad" end))
            print("ok")
            """, 0, "ok\n", "");
    }

    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetSpatialCases()
    {
        yield return ("Workspace raycasts intersect parts and report nearest hit geometry", """
            local part = Instance.new("Part", workspace)
            part.Position = Vector3.new(0, 0, -10); part.Size = Vector3.new(4, 4, 4)
            local hit = workspace:Raycast(Vector3.zero, Vector3.new(0, 0, -20))
            assert(typeof(hit) == "RaycastResult" and not pcall(function() hit.Distance = 0 end))
            assert(hit and hit.Instance == part and math.abs(hit.Distance - 8) < 0.001)
            assert(hit.Position == Vector3.new(0, 0, -8) and hit.Normal == Vector3.zAxis)
            assert(workspace:Raycast(Vector3.zero, Vector3.xAxis) == nil)
            print("ok")
            """, 0, "ok\n", "");
        yield return ("Color3 supports immutable color values interpolation and RGB conversion", """
            local red = Color3.fromRGB(255, 0, 0)
            assert(typeof(red) == "Color3" and red == Color3.new(1, 0, 0))
            assert(red.R == 1 and red.G == 0 and red.B == 0)
            local mix = red:Lerp(Color3.new(0, 0, 1), 0.5)
            assert(mix.R == 0.5 and mix.B == 0.5)
            assert(not pcall(function() red.R = 0 end))
            local color = Color3.new(0.1, 0.2, 0.3)
            local holder = Instance.new("Folder")
            holder:SetAttribute("Color", color)
            assert(holder:GetAttribute("Color") == color)
            print("ok")
            """, 0, "ok\n", "");
    }
}
