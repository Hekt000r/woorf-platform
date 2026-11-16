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

Finite CLI runs drain deferred work and timers. Ordinary signal connections and indefinite signal/child/module waits alone do not keep them alive. Active Heartbeat connections do keep the scheduler alive; disconnect them when a finite run should finish. Inactive waits are released when the session closes. TCP listeners and clients remain alive for external network input; see [TCP transport](socket-transport.md).

Durations must be finite numbers. Negative durations are clamped to zero. `task.wait` uses a minimum virtual interval of 1/60 second; this does not reproduce Roblox simulation phases. Timers use deadline order with registration order as the tie-breaker. `task.defer` scheduled by deferred work runs in a later pass. A zero-delay task runs on a scheduler pass rather than inline.

Errors in scheduled tasks and listeners are reported to stderr and other work continues. Errors in startup scripts, including after a yield, return CLI exit code 1. Nested synchronous task resumption is limited to 100 levels; exceeding the limit reports an error and closes the task. VM execution budgets interrupt runaway code, including protected loops; exceeding the limit terminates the VM with failure. Managed host calls remain synchronous. See [execution budgets](runtime-hardening.md#execution-budgets).

RunService exposes IsServer, IsClient and a headless Heartbeat. Heartbeat fires with positive elapsed time when at least 1/60 second has elapsed between steps; listeners may yield and use Wait. It does not reproduce Roblox simulation phases. LocalNetworkHost supports synchronous host-driven execution between joins and disconnects; see [client/server lifecycle](client-server.md).

BindToClose callbacks run concurrently during session disposal and may yield, with a 30-second bound for cooperative work. Completion tracks registered callbacks rather than unrelated Heartbeat subscriptions. Runaway Luau callbacks are interrupted by the execution budget and return failure; unresolvable waits can drain early. Debris timers survive destruction of the Script that scheduled them. See [backend API limits](backend-apis.md).

Enabled/Disabled and the Legacy/Server/Client RunContext subset support dynamic startup and cancellation at scheduler boundaries; see [script lifecycle](runtime-hardening.md#script-lifecycle). Parallel execution, `ConnectParallel` and deferred Instance event delivery remain unsupported. Instance events retain immediate delivery until a callback yields. Raw `coroutine.yield()` without a task/signal/module resumption source does not schedule a future resume.

Example:

```sh
dotnet run --project src/Woorf.Cli -- examples/scheduler.luau
```

References: Roblox's [task library](https://create.roblox.com/docs/reference/engine/libraries/task), [RBXScriptSignal](https://create.roblox.com/docs/reference/engine/datatypes/RBXScriptSignal), and [Instance](https://create.roblox.com/docs/reference/engine/classes/Instance). Exact Roblox scheduling phases and timing are not reproduced by this headless implementation.

Server-side [physics](physics.md) advances in fixed steps before Heartbeat and
scheduled continuations. Dynamic bodies request fixed-step wakeups while timers
are pending; physics alone does not keep a finite CLI run alive.
