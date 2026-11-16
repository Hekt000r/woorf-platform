local remoteStates = setmetatable({}, {__mode = "k"})
local isRemoteClient = game:GetService("RunService"):IsClient()

local disconnectedPlayers = setmetatable({}, {__mode = "k"})

local function runRemoteCallback(state, player, id, arguments, send)
    if player and disconnectedPlayers[player] then return end
    local thread = coroutine.running()
    local callback = state.callback
    local environment = getfenv(callback)
    local owner = environment and rawget(environment, "script")
    if owner then
        local ownerState = ownerStates[owner]
        if ownerState and ownerState.cancelled then
            send(player, "reply", id, false, "RemoteFunction callback was cancelled")
            return
        end
        trackThread(thread, owner)
    end
    state.active[thread] = player
    local function cancel()
        state.active[thread] = nil
        if not state.destroyed and not disconnectedPlayers[player] then send(player, "reply", id, false, "RemoteFunction callback was cancelled") end
    end
    addThreadCancellation(thread, cancel)
    local outcome
    if isRemoteClient then
        outcome = table.pack(pcall(callback, unpack(arguments, 1, arguments.n)))
    else
        outcome = table.pack(pcall(callback, player, unpack(arguments, 1, arguments.n)))
    end
    state.active[thread] = nil
    removeThreadCancellation(thread, cancel)
    if not state.destroyed and not disconnectedPlayers[player] then
        local canSend, message = pcall(send, player, "reply", id, unpack(outcome, 1, outcome.n))
        if not canSend then send(player, "reply", id, false, tostring(message)) end
    end
end

local function getRemoteState(remote)
    assert(typeof(remote) == "Instance" and remote.ClassName == "RemoteFunction", "Expected RemoteFunction")
    local state = remoteStates[remote]
    if state then return state end
    state = {pending = {}, waiting = {}, active = {}, sequence = 0}
    remoteStates[remote] = state
    local function send(player, ...)
        if isRemoteClient then remote:_SendServer(...)
        else remote:_SendClient(player, ...) end
    end
    local function receive(player, kind, id, ...)
        if kind == "reply" then
            local pending = state.pending[id]
            if pending and (isRemoteClient or pending.player == player) then
                state.pending[id] = nil
                resumeThread(pending.thread, table.pack(...))
            end
            return
        end
        assert(kind == "request", "Invalid remote request")
        local arguments = table.pack(...)
        local function execute()
            runRemoteCallback(state, player, id, arguments, send)
        end
        if state.callback then execute()
        else table.insert(state.waiting, {execute = execute, player = player}) end
    end
    if isRemoteClient then
        remote._ClientEvent:Connect(function(...) receive(nil, ...) end)
    else remote._ServerEvent:Connect(receive) end
    remote.Destroying:Connect(function()
        state.destroyed = true
        state.waiting = {}
        for id, pending in state.pending do
            state.pending[id] = nil
            resumeThread(pending.thread, table.pack(false, "RemoteFunction was destroyed"))
        end
    end)
    return state
end

function _woorf_remote_init(remote) getRemoteState(remote) end

function _woorf_remote_get(remote, property)
    getRemoteState(remote)
    error(property .. " is a callback member; you can only set its value", 2)
end

function _woorf_remote_set(remote, property, callback)
    local state = getRemoteState(remote)
    assert(property == (if isRemoteClient then "OnClientInvoke" else "OnServerInvoke"),
        "Callback belongs to the other execution side")
    assert(callback == nil or type(callback) == "function", "Expected function or nil")
    state.callback = callback
    if callback then
        local waiting = state.waiting
        state.waiting = {}
        for _, request in waiting do task.spawn(request.execute) end
    end
end

local function invokeRemote(remote, player, ...)
    assert(_woorf_can_yield(), "Cannot yield in this callback")
    local state = getRemoteState(remote)
    assert(not state.destroyed, "RemoteFunction was destroyed")
    state.sequence += 1
    local id, thread = state.sequence, coroutine.running()
    if isRemoteClient then remote:_SendServer("request", id, ...)
    else remote:_SendClient(player, "request", id, ...) end
    state.pending[id] = {thread = thread, player = player}
    addThreadCleanup(thread, function() state.pending[id] = nil end)
    local outcome = table.pack(coroutine.yield())
    if not outcome[1] then error(outcome[2], 2) end
    return unpack(outcome, 2, outcome.n)
end

function _woorf_invoke_server(remote, ...)
    assert(isRemoteClient, "InvokeServer can only be called from a client")
    return invokeRemote(remote, nil, ...)
end

function _woorf_invoke_client(remote, player, ...)
    assert(not isRemoteClient, "InvokeClient can only be called from the server")
    assert(typeof(player) == "Instance" and player:IsA("Player"), "Expected Player")
    return invokeRemote(remote, player, ...)
end
game:GetService("Players").PlayerRemoving:Connect(function(player)
    if isRemoteClient then return end
    disconnectedPlayers[player] = true
    for _, state in remoteStates do
        for index = #state.waiting, 1, -1 do
            if state.waiting[index].player == player then table.remove(state.waiting, index) end
        end
        for thread, sender in state.active do
            if sender == player and coroutine.status(thread) == "suspended" then cancelThread(thread) end
        end
        for id, pending in state.pending do
            if pending.player == player then
                state.pending[id] = nil
                resumeThread(pending.thread, table.pack(false, "RemoteFunction target disconnected"))
            end
        end
    end
end)
