static class SchedulerCases
{
    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return GetSpawnCase();
        yield return GetDeferCase();
        yield return GetWaitCase();
        yield return GetSignalWaitCase();
        yield return GetYieldingListenerCase();
        yield return GetWaitForChildCase();
        yield return GetValidationCase();
        yield return GetErrorCase();
        yield return GetIdleCase();
    }

    private static (string, string?, int, string, string) GetSpawnCase() =>
        ("spawns functions and threads immediately with arguments", """
            local calls = 0
            local thread = task.spawn(function(a, b, c)
                assert(a == "value" and b == nil and c == 3)
                calls += 1
            end, "value", nil, 3)
            assert(type(thread) == "thread" and calls == 1)
            local suspended = coroutine.create(function(value) assert(value == 7); calls += 1 end)
            assert(task.spawn(suspended, 7) == suspended and calls == 2)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetDeferCase() =>
        ("runs deferred work after the current script", """
            local thread = task.defer(function(a, b) assert(a == "deferred" and b == nil); print(a) end, "deferred", nil)
            assert(type(thread) == "thread")
            print("current")
            """, 0, "current\ndeferred\n", "");

    private static (string, string?, int, string, string) GetWaitCase() =>
        ("resumes timed tasks and returns elapsed wait duration", """
            local delayed = false
            local thread = task.delay(0.001, function(value) assert(value == 9); delayed = true end, 9)
            assert(type(thread) == "thread" and not delayed)
            local elapsed = task.wait(0.003)
            assert(type(elapsed) == "number" and elapsed >= 0.003 and delayed)
            local nextTick = task.wait()
            assert(type(nextTick) == "number" and nextTick > 0)
            print("timers complete")
            """, 0, "timers complete\n", "");

    private static (string, string?, int, string, string) GetSignalWaitCase() =>
        ("resumes signal waits with complete event arguments", """
            local folder = Instance.new("Folder", workspace)
            local observed = false
            task.spawn(function()
                local child, parent = folder.AncestryChanged:Wait()
                assert(child == folder and parent == nil)
                observed = true
            end)
            assert(not observed)
            folder.Parent = nil
            assert(observed)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetYieldingListenerCase() =>
        ("allows event listeners to yield without blocking other listeners", """
            local folder = Instance.new("Folder")
            local finished, later = false, false
            folder.Changed:Connect(function(property)
                assert(property == "Name")
                task.wait(0.001)
                finished = true
            end)
            folder.Changed:Connect(function() later = true end)
            folder.Name = "Changed"
            assert(later and not finished)
            task.wait(0.003)
            assert(finished)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetWaitForChildCase() =>
        ("waits for named children and handles timeouts", """
            local folder = Instance.new("Folder")
            local existing = Instance.new("Folder", folder); existing.Name = "Existing"
            assert(folder:WaitForChild("Existing", 0) == existing)
            assert(folder:WaitForChild("Missing", 0.001) == nil)
            task.delay(0.001, function()
                local wrong = Instance.new("Folder"); wrong.Name = "Other"; wrong.Parent = folder
                local child = Instance.new("Folder"); child.Name = "Expected"; child.Parent = folder
            end)
            local arrived = folder:WaitForChild("Expected", 1)
            assert(arrived and arrived.Name == "Expected" and arrived.Parent == folder)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetValidationCase() =>
        ("rejects invalid scheduler arguments", """
            assert(type(task) == "table")
            assert(not pcall(function() task.spawn({}) end))
            assert(not pcall(function() task.defer(nil) end))
            assert(not pcall(function() task.delay("later", function() end) end))
            assert(not pcall(function() task.wait({}) end))
            assert(not pcall(function() task.wait(math.huge) end))
            assert(not pcall(function() workspace:WaitForChild({}) end))
            assert(not pcall(function() workspace:WaitForChild("Missing", "soon") end))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetErrorCase() =>
        ("reports scheduled task failures while continuing other tasks", """
            task.defer(function() error("intentional scheduled task failure") end)
            task.defer(function() print("survived") end)
            """, 0, "survived\n", "intentional scheduled task failure");

    private static (string, string?, int, string, string) GetIdleCase() =>
        ("shuts down when only unsignalled waiters remain", """
            task.spawn(function() workspace.ChildAdded:Wait(); error("must stay suspended") end)
            print("idle")
            """, 0, "idle\n", "");
}
