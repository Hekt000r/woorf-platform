local bindableStates = setmetatable({}, {__mode = "k"})

local function getBindableState(bindable)
    assert(typeof(bindable) == "Instance" and bindable.ClassName == "BindableFunction", "Expected BindableFunction")
    local state = bindableStates[bindable]
    if state then return state end
    state = {waiting = {}, active = {}}
    bindableStates[bindable] = state
    local wasInternalConnection = isInternalConnection
    isInternalConnection = true
    local destruction = bindable.Destroying:Connect(function()
        state.destroyed = true
        state.callback = nil
        local waiting, active = state.waiting, state.active
        state.waiting, state.active = {}, {}
        for _, thread in waiting do
            resumeThread(thread, table.pack(false, "BindableFunction was destroyed"))
        end
        for thread in active do
            if coroutine.status(thread) == "suspended" then cancelThread(thread) end
        end
    end)
    isInternalConnection = wasInternalConnection
    if not destruction.Connected then state.destroyed = true end
    return state
end

local function copyBindableValue(value, visited, depth)
    if type(value) ~= "table" then return value end
    assert(depth < 64 and not visited[value], "BindableFunction cannot transfer cyclic or deeply nested tables")
    visited[value] = true
    local copy = {}
    for key, item in next, value do
        local keyType = type(key)
        local normalized = if keyType == "string" or keyType == "number" then key else tostring(key)
        copy[normalized] = copyBindableValue(item, visited, depth + 1)
    end
    visited[value] = nil
    return copy
end

local function copyBindableTuple(...)
    local tuple = table.pack(...)
    local visited = {}
    for index = 1, tuple.n do
        tuple[index] = copyBindableValue(tuple[index], visited, 0)
    end
    return unpack(tuple, 1, tuple.n)
end

function _woorf_bindable_get(bindable, property)
    getBindableState(bindable)
    error(property .. " is a callback member; you can only set its value", 2)
end

function _woorf_bindable_set(bindable, _, callback)
    local state = getBindableState(bindable)
    assert(not state.destroyed, "BindableFunction was destroyed")
    assert(callback == nil or type(callback) == "function", "Expected function or nil")
    state.callback = callback
    if not callback then return end
    local waiting = state.waiting
    state.waiting = {}
    for _, thread in waiting do
        resumeThread(thread, table.pack(true, callback))
    end
end

local function waitForBindableCallback(state, caller)
    if state.callback then return state.callback end
    table.insert(state.waiting, caller)
    addThreadCleanup(caller, function()
        local index = table.find(state.waiting, caller)
        if index then table.remove(state.waiting, index) end
    end)
    local success, callback = coroutine.yield()
    if not success then error(callback, 3) end
    return callback
end

local function runBindableCallback(state, callback, caller, arguments)
    local outcome
    local isCallerCancelled = false
    local function complete(result)
        outcome = result
        if not isCallerCancelled and coroutine.status(caller) == "suspended" then resumeThread(caller, result) end
    end
    local worker = coroutine.create(function()
        local thread = coroutine.running()
        state.active[thread] = true
        local function cancel()
            state.active[thread] = nil
            complete(table.pack(false, "BindableFunction callback was cancelled"))
        end
        addThreadCancellation(thread, cancel)
        local result = table.pack(pcall(function()
            return copyBindableTuple(callback(unpack(arguments, 1, arguments.n)))
        end))
        state.active[thread] = nil
        removeThreadCancellation(thread, cancel)
        complete(result)
    end)
    local environment = getfenv(callback)
    local owner = environment and rawget(environment, "script")
    if not trackThread(worker, owner) then error("BindableFunction callback was cancelled", 3) end
    resumeThread(worker, table.pack())
    if outcome then return unpack(outcome, 1, outcome.n) end
    addThreadCleanup(caller, function()
        if outcome then return end
        isCallerCancelled = true
        if coroutine.status(worker) == "suspended" then cancelThread(worker) end
    end)
    return coroutine.yield()
end

function _woorf_invoke_bindable(bindable, ...)
    assert(_woorf_can_yield(), "Cannot yield in this callback")
    local state = getBindableState(bindable)
    assert(not state.destroyed, "BindableFunction was destroyed")
    local arguments = table.pack(copyBindableTuple(...))
    local caller = coroutine.running()
    local callback = waitForBindableCallback(state, caller)
    assert(not state.destroyed, "BindableFunction was destroyed")
    local outcome = table.pack(runBindableCallback(state, callback, caller, arguments))
    if not outcome[1] then error(outcome[2], 2) end
    if outcome.n == 1 then return nil end
    return unpack(outcome, 2, outcome.n)
end
