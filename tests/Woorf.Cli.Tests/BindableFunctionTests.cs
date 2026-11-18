using Woorf.Engine;

internal static class BindableFunctionTests
{
    internal static int Run() => LuauRuntime.Execute("""
        local bindable = Instance.new("BindableFunction")
        local done = false
        task.spawn(function()
            local input = setmetatable({count = 8}, {})
            local value, missing, returned = bindable:Invoke(input)
            assert(value == 9 and missing == nil and returned.count == 9)
            assert(returned ~= input and input.count == 8 and getmetatable(returned) == nil)
            done = true
        end)
        assert(not done)
        bindable.OnInvoke = function(input)
            task.wait()
            input.count += 1
            return input.count, nil, input
        end
        task.wait(0.1)
        assert(done)
        assert(not pcall(function() return bindable.OnInvoke end))
        bindable.OnInvoke = function() error("expected callback failure") end
        local success, message = pcall(bindable.Invoke, bindable)
        assert(not success and string.find(message, "expected callback failure", 1, true))
        bindable.OnInvoke = function() end
        assert(select("#", bindable:Invoke()) == 1 and bindable:Invoke() == nil)

        local waiting = Instance.new("BindableFunction")
        local failed = false
        task.spawn(function() failed = not pcall(waiting.Invoke, waiting) end)
        waiting:Destroy()
        assert(failed)

        local active = Instance.new("BindableFunction")
        active.OnInvoke = function() task.wait(1) end
        failed = false
        task.spawn(function() failed = not pcall(active.Invoke, active) end)
        active:Destroy()
        assert(failed)

        local cancelled = Instance.new("BindableFunction")
        local callbackCompleted = false
        cancelled.OnInvoke = function() task.wait(0.1); callbackCompleted = true end
        local caller = task.spawn(function() cancelled:Invoke(); error("Cancelled caller resumed") end)
        task.cancel(caller)
        task.wait(0.2)
        assert(not callbackCompleted)
        print("PASS BindableFunction waiting, tuples, table copying, errors, and cancellation")
        """, "bindable-function.luau");
}
