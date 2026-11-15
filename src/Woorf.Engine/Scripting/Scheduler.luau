local now = 0
local clock = _woorf_now
local hasRootFailure = false
local timers, deferred = {}, {}
local timerSequence = 0
local contexts = setmetatable({}, {__mode = "k"})
local report = _woorf_report
local loadModule = _woorf_load_module
local unpack = table.unpack

local function resumeThread(thread, arguments, context)
    if coroutine.status(thread) ~= "suspended" then return end
    contexts[thread] = context or contexts[thread] or "Task failed: "
    local result = table.pack(coroutine.resume(thread, unpack(arguments, 1, arguments.n)))
    if not result[1] then
        if contexts[thread] == "Script failed: " then hasRootFailure = true end
        report(contexts[thread] .. tostring(result[2]))
    end
end

local function getThread(callback)
    if type(callback) == "function" then return coroutine.create(callback) end
    assert(type(callback) == "thread", "Expected function or coroutine")
    assert(coroutine.status(callback) == "suspended", "Expected suspended coroutine")
    return callback
end

local function getDuration(duration)
    if duration == nil then return 0 end
    assert(type(duration) == "number" and duration == duration and math.abs(duration) ~= math.huge,
        "Expected finite duration")
    return math.max(0, duration)
end

local function queueTimer(duration, action)
    timerSequence += 1
    local timer = {deadline = clock() + duration, sequence = timerSequence, action = action, active = true}
    table.insert(timers, timer)
    return timer
end

local tasks = {}
function tasks.spawn(callback, ...)
    local thread = getThread(callback)
    resumeThread(thread, table.pack(...))
    return thread
end

function tasks.defer(callback, ...)
    local thread = getThread(callback)
    table.insert(deferred, {thread = thread, arguments = table.pack(...)})
    return thread
end

function tasks.delay(duration, callback, ...)
    duration = getDuration(duration)
    local thread, arguments = getThread(callback), table.pack(...)
    queueTimer(duration, function() resumeThread(thread, arguments) end)
    return thread
end

function tasks.wait(duration)
    duration = getDuration(duration)
    local thread, started = coroutine.running(), clock()
    assert(thread, "task.wait requires a coroutine")
    queueTimer(math.max(duration, 1 / 60), function()
        resumeThread(thread, table.pack(clock() - started))
    end)
    return coroutine.yield()
end
task = table.freeze(tasks)

function _woorf_scheduler_failed() return hasRootFailure end

function _woorf_step(time)
    now = time
    local ready = deferred
    deferred = {}
    for _, entry in ready do resumeThread(entry.thread, entry.arguments) end
    local pending = timers
    timers = {}
    table.sort(pending, function(first, second)
        return if first.deadline == second.deadline then first.sequence < second.sequence
            else first.deadline < second.deadline
    end)
    for _, timer in pending do
        if timer.active then
            if timer.deadline <= now then timer.active = false; timer.action()
            else table.insert(timers, timer) end
        end
    end
    if #deferred > 0 then return now end
    local nextDeadline = math.huge
    for _, timer in timers do
        if timer.active then nextDeadline = math.min(nextDeadline, timer.deadline) end
    end
    return if nextDeadline == math.huge then -1 else nextDeadline
end

function _woorf_start(callback)
    local thread = coroutine.create(callback)
    contexts[thread] = "Script failed: "
    local succeeded, message = coroutine.resume(thread)
    if not succeeded then error(message, 0) end
end

function _woorf_callback(callback, ...)
    resumeThread(coroutine.create(callback), table.pack(...), "Signal callback failed: ")
end

function _woorf_signal_wait(signal)
    assert(typeof(signal) == "RBXScriptSignal", "Expected RBXScriptSignal self")
    local thread = coroutine.running()
    signal:Once(function(...) resumeThread(thread, table.pack(...)) end)
    return coroutine.yield()
end

function _woorf_wait_for_child(instance, name, timeout)
    assert(typeof(instance) == "Instance", "Expected Instance self")
    assert(type(name) == "string", "Expected child name")
    if timeout ~= nil then timeout = getDuration(timeout) end
    local child = instance:FindFirstChild(name)
    if child then return child end
    if timeout == 0 then return nil end
    local thread, completed, timer, connection = coroutine.running(), false, nil, nil
    local function finish(value)
        if completed then return end
        completed = true
        connection:Disconnect()
        if timer then timer.active = false end
        resumeThread(thread, table.pack(value))
    end
    connection = instance.ChildAdded:Connect(function(value)
        if value.Name == name then finish(value) end
    end)
    if timeout then timer = queueTimer(timeout, function() finish(nil) end) end
    return coroutine.yield()
end

local moduleCache = {}
local waitingFor = setmetatable({}, {__mode = "k"})

local function awaitModule(entry)
    local thread, owner = coroutine.running(), entry.owner
    while owner do
        if owner == thread then error("Circular module dependency", 0) end
        local dependency = waitingFor[owner]
        owner = dependency and dependency.owner
    end
    waitingFor[thread] = entry
    table.insert(entry.waiters, thread)
    coroutine.yield()
    waitingFor[thread] = nil
end

function require(module)
    assert(typeof(module) == "Instance" and module.ClassName == "ModuleScript", "Expected ModuleScript")
    local entry = moduleCache[module]
    if entry then
        if entry.loading then awaitModule(entry) end
    else
        entry = {loading = true, owner = coroutine.running(), waiters = {}}
        moduleCache[module] = entry
        local outcome = table.pack(pcall(function() return loadModule(module)() end))
        if not outcome[1] then entry.error = tostring(outcome[2])
        elseif outcome.n ~= 2 then entry.error = "ModuleScript must return exactly one value."
        else entry.value = outcome[2] end
        entry.loading, entry.owner = false, nil
        for _, thread in entry.waiters do resumeThread(thread, table.pack()) end
        table.clear(entry.waiters)
    end
    if entry.error then error(entry.error, 0) end
    return entry.value
end
