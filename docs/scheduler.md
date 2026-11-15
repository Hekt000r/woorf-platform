# Headless scheduler

The C# runtime drives a cooperative Luau scheduler using a monotonic clock. Production uses `Stopwatch` and sleeps until the next timer; tests can supply `IRuntimeClock` to advance time without waiting. The coroutine and module continuation glue is embedded as a Luau resource.

Supported APIs:

- `task.spawn(functionOrThread, ...)` resumes immediately and returns the coroutine.
- `task.defer(functionOrThread, ...)` queues work for the next scheduler pass.
- `task.delay(seconds, functionOrThread, ...)` queues a timer and returns the coroutine.
- `task.cancel(thread)` closes a suspended task, removes its queued work, and releases signal, child, and module waits.
- `task.wait(seconds?)` yields and returns the elapsed monotonic time when resumed.
- `signal:Wait()` yields until the next event and returns all event arguments, including nil arguments.
- `instance:WaitForChild(name, timeout?)` returns an existing direct child immediately, otherwise waits for a matching child or returns nil after the timeout.

Project server scripts begin in source-path order. A yielding script allows later scripts to start. Destroying a running Script cancels its suspended tasks and yielding callbacks, and releases callers waiting for a ModuleScript it was executing. Deferred work drains after initial startup. The runtime then drains deferred work and timers, sleeping until the next deadline. Callbacks can yield without blocking other listeners. Modules can yield; concurrent `require()` calls wait for one shared execution and receive the same cached result or error. Dependency cycles still fail explicitly.

The loop shuts down when no deferred work or active timers remain. Signal connections and indefinite signal/child/module waits alone do not keep a headless process alive. This is an initial CLI convention; a future interactive engine loop will continue processing external input. Inactive waits are released when the session closes.

Durations must be finite numbers. Negative durations are clamped to zero. `task.wait` uses a minimum virtual interval of 1/60 second; this is not a simulated renderer or Roblox Heartbeat. Timers use deadline order with registration order as the tie-breaker. `task.defer` scheduled by deferred work runs in a later pass. A zero-delay task runs on a scheduler pass rather than inline.

Errors in scheduled tasks and listeners are reported to stderr and other work continues. Errors in startup scripts, including after a yield, return CLI exit code 1. Nested synchronous task resumption is limited to 100 levels; exceeding the limit reports an error and closes the task. There is no task preemption or CPU quota, so a non-yielding infinite loop still blocks the runtime.

Parallel execution, `ConnectParallel`, deferred Instance event delivery, RunService/Heartbeat, RunContext behavior, and external event-loop hosting are not implemented. Instance events retain immediate delivery until a callback yields. Raw `coroutine.yield()` without a task/signal/module resumption source does not schedule a future resume.

Example:

```sh
dotnet run --project src/Woorf.Cli -- examples/scheduler.luau
```

References: Roblox's [task library](https://create.roblox.com/docs/reference/engine/libraries/task), [RBXScriptSignal](https://create.roblox.com/docs/reference/engine/datatypes/RBXScriptSignal), and [Instance](https://create.roblox.com/docs/reference/engine/classes/Instance). Exact Roblox scheduling phases and timing are not reproduced by this headless implementation.
