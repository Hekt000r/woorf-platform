local now = 0
local clock = _woorf_now
local hasRootFailure = false
local timers, deferred = {}, {}
local timerSequence = 0
local contexts = setmetatable({}, {__mode = "k"})
local owners = setmetatable({}, {__mode = "k"})
local ownerStates = setmetatable({}, {__mode = "k"})
local cleanups = setmetatable({}, {__mode = "k"})
local cancellations = setmetatable({}, {__mode = "k"})
local isInternalConnection = false
local currentOwner
local resumeDepth = 0
local maxResumeDepth = 100
local report = _woorf_report
local loadModule = _woorf_load_module
local unpack = table.unpack

local function clearThreadCleanups(thread)
    local threadCleanups = cleanups[thread]
    cleanups[thread] = nil
    if threadCleanups then
        for _, cleanup in threadCleanups do pcall(cleanup) end
    end
end

local function addThreadCleanup(thread, cleanup)
    local threadCleanups = cleanups[thread]
    if not threadCleanups then threadCleanups = {}; cleanups[thread] = threadCleanups end
    table.insert(threadCleanups, cleanup)
end

local function addThreadCancellation(thread, cancellation)
    local threadCancellations = cancellations[thread]
    if not threadCancellations then threadCancellations = {}; cancellations[thread] = threadCancellations end
    table.insert(threadCancellations, cancellation)
end

local function removeThreadCancellation(thread, cancellation)
    local threadCancellations = cancellations[thread]
    if not threadCancellations then return end
    local index = table.find(threadCancellations, cancellation)
    if index then table.remove(threadCancellations, index) end
    if #threadCancellations == 0 then cancellations[thread] = nil end
end

local function runThreadCancellations(thread)
    local threadCancellations = cancellations[thread]
    cancellations[thread] = nil
    if threadCancellations then
        for _, cancellation in threadCancellations do pcall(cancellation) end
    end
end

local function cancelThread(thread)
    if type(thread) ~= "thread" then error("Expected thread", 2) end
    local status = coroutine.status(thread)
    if status == "running" then error("Cannot cancel a running thread", 2) end
    if status == "dead" then error("Cannot cancel a dead thread", 2) end
    runThreadCancellations(thread)
    clearThreadCleanups(thread)
    local succeeded, message = coroutine.close(thread)
    if not succeeded then error(message, 2) end
    contexts[thread] = nil
    local owner = owners[thread]
    owners[thread] = nil
    local ownerState = owner and ownerStates[owner]
    if ownerState then ownerState.threads[thread] = nil end
end

local function trackThread(thread, owner)
    owner = owner or currentOwner
    if not owner then return true end
    local ownerState = ownerStates[owner]
    if not ownerState then
        ownerState = {threads = {}, cancelled = false}
        ownerStates[owner] = ownerState
        isInternalConnection = true
        ownerState.destroyConnection = owner.Destroying:Connect(function()
            ownerState.cancelled = true
            local ownedThreads = {}
            for ownedThread in ownerState.threads do table.insert(ownedThreads, ownedThread) end
            for _, ownedThread in ownedThreads do
                if coroutine.status(ownedThread) == "suspended" then
                    cancelThread(ownedThread)
                end
            end
        end)
        isInternalConnection = false
    end
    if ownerState.cancelled then
        coroutine.close(thread)
        return false
    end
    ownerState.threads[thread] = true
    owners[thread] = owner
    return true
end

local function resumeThread(thread, arguments, context)
    if coroutine.status(thread) ~= "suspended" then return end
    local owner = owners[thread]
    local ownerState = owner and ownerStates[owner]
    if ownerState and ownerState.cancelled then cancelThread(thread); return end
    if resumeDepth >= maxResumeDepth then
        report("Scheduler reentrancy limit exceeded")
        cancelThread(thread)
        return
    end
    clearThreadCleanups(thread)
    contexts[thread] = context or contexts[thread] or "Task failed: "
    local previousOwner = currentOwner
    currentOwner = owner
    resumeDepth += 1
    local result = table.pack(coroutine.resume(thread, unpack(arguments, 1, arguments.n)))
    resumeDepth -= 1
    currentOwner = previousOwner
    if not result[1] then
        if contexts[thread] == "Script failed: " then hasRootFailure = true end
        report(contexts[thread] .. tostring(result[2]) .. "\n" .. debug.traceback(thread))
    end
    if coroutine.status(thread) == "dead" then
        clearThreadCleanups(thread)
        local finishedOwner = owners[thread]
        local finishedState = finishedOwner and ownerStates[finishedOwner]
        if finishedState then finishedState.threads[thread] = nil end
        owners[thread] = nil
    elseif ownerState and ownerState.cancelled then
        cancelThread(thread)
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
    if not trackThread(thread) then return thread end
    resumeThread(thread, table.pack(...))
    return thread
end

function tasks.defer(callback, ...)
    local thread = getThread(callback)
    if not trackThread(thread) then return thread end
    local entry = {thread = thread, arguments = table.pack(...), active = true}
    table.insert(deferred, entry)
    addThreadCleanup(thread, function() entry.active = false end)
    return thread
end

function tasks.delay(duration, callback, ...)
    duration = getDuration(duration)
    local thread, arguments = getThread(callback), table.pack(...)
    if not trackThread(thread) then return thread end
    local timer = queueTimer(duration, function() resumeThread(thread, arguments) end)
    addThreadCleanup(thread, function() timer.active = false end)
    return thread
end

function tasks.wait(duration)
    duration = getDuration(duration)
    assert(_woorf_can_yield(), "Cannot yield in this callback")
    local thread, started = coroutine.running(), clock()
    assert(thread, "task.wait requires a coroutine")
    local timer = queueTimer(math.max(duration, 1 / 60), function()
        resumeThread(thread, table.pack(clock() - started))
    end)
    addThreadCleanup(thread, function() timer.active = false end)
    return coroutine.yield()
end
function tasks.cancel(thread) cancelThread(thread) end
task = table.freeze(tasks)

function spawn(callback)
    assert(type(callback) == "function", "Expected function")
    tasks.defer(callback)
end

function wait(duration)
    local elapsed = tasks.wait(math.max(getDuration(duration), 1 / 30))
    return elapsed, clock()
end

function _woorf_scheduler_failed() return hasRootFailure end

function _woorf_step(time)
    now = time
    local ready = deferred
    deferred = {}
    for _, entry in ready do
        if entry.active then resumeThread(entry.thread, entry.arguments) end
    end
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

function _woorf_start(callback, owner)
    if owner then ownerStates[owner] = nil end
    local thread = coroutine.create(callback)
    contexts[thread] = "Script failed: "
    if not trackThread(thread, owner) then return end
    local previousOwner = currentOwner
    currentOwner = owner
    local succeeded, message = coroutine.resume(thread)
    currentOwner = previousOwner
    if not succeeded then error(message, 0) end
    if coroutine.status(thread) == "dead" and owner then ownerStates[owner].threads[thread] = nil end
end

function _woorf_callback(callback, ...)
    local thread = coroutine.create(callback)
    local environment = getfenv(callback)
    if not trackThread(thread, environment and rawget(environment, "script")) then return end
    resumeThread(thread, table.pack(...), "Signal callback failed: ")
end

function _woorf_signal_wait(signal)
    assert(typeof(signal) == "RBXScriptSignal", "Expected RBXScriptSignal self")
    assert(_woorf_can_yield(), "Cannot yield in this callback")
    local thread = coroutine.running()
    local connection = signal:Once(function(...) resumeThread(thread, table.pack(...)) end)
    addThreadCleanup(thread, function() connection:Disconnect() end)
    return coroutine.yield()
end

function _woorf_wait_for_child(instance, name, timeout)
    assert(typeof(instance) == "Instance", "Expected Instance self")
    assert(type(name) == "string", "Expected child name")
    if timeout ~= nil then timeout = getDuration(timeout) end
    local child = instance:FindFirstChild(name)
    if child then return child end
    if timeout == 0 then return nil end
    assert(_woorf_can_yield(), "Cannot yield in this callback")
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
    addThreadCleanup(thread, function()
        completed = true
        connection:Disconnect()
        if timer then timer.active = false end
    end)
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
    addThreadCleanup(thread, function()
        waitingFor[thread] = nil
        local index = table.find(entry.waiters, thread)
        if index then table.remove(entry.waiters, index) end
    end)
    coroutine.yield()
    waitingFor[thread] = nil
end

function require(module)
    assert(typeof(module) == "Instance" and module.ClassName == "ModuleScript", "Expected ModuleScript")
    local entry = moduleCache[module]
    if entry then
        if entry.loading then awaitModule(entry) end
    else
        local ownerThread = coroutine.running()
        entry = {loading = true, owner = ownerThread, waiters = {}}
        moduleCache[module] = entry
        local function cancelLoad()
            if not entry.loading then return end
            entry.error = "ModuleScript execution was cancelled."
            entry.loading, entry.owner = false, nil
            local waiters = entry.waiters
            entry.waiters = {}
            for _, thread in waiters do resumeThread(thread, table.pack()) end
        end
        addThreadCancellation(ownerThread, cancelLoad)
        local outcome = table.pack(xpcall(function() return loadModule(module)() end, debug.traceback))
        removeThreadCancellation(ownerThread, cancelLoad)
        if not outcome[1] then entry.error = tostring(outcome[2])
        elseif outcome.n ~= 2 then entry.error = "ModuleScript must return exactly one value."
        else entry.value = outcome[2] end
        entry.loading, entry.owner = false, nil
        local waiters = entry.waiters
        entry.waiters = {}
        for _, thread in waiters do resumeThread(thread, table.pack()) end
    end
    if entry.error then error(entry.error, 0) end
    return entry.value
end
