function _woorf_track_connection(connection, callback)
    if isInternalConnection then return end
    local environment = getfenv(callback)
    local owner = currentOwner or (environment and rawget(environment, "script"))
    local state = owner and ownerStates[owner]
    if not state then return end
    state.connections = state.connections or {}
    table.insert(state.connections, connection)
end

function _woorf_stop_script(owner)
    local state = ownerStates[owner]
    if not state then return end
    state.cancelled = true
    if state.destroyConnection then state.destroyConnection:Disconnect() end
    for _, connection in state.connections or {} do connection:Disconnect() end
    local threads = {}
    for thread in state.threads do table.insert(threads, thread) end
    for _, thread in threads do
        if coroutine.status(thread) == "suspended" then cancelThread(thread) end
    end
end

local contextItems = {}
local contextByItem = {}
for value, name in {[0] = "Legacy", [1] = "Server", [2] = "Client", [3] = "Plugin"} do
    local proxy = _woorf_new_proxy({
        __type = "EnumItem", __metatable = "The metatable is locked",
        __index = function(_, key)
            if key == "Name" then return name elseif key == "Value" then return value end
            error("Unsupported EnumItem member", 2)
        end,
        __newindex = function() error("EnumItem is read-only", 2) end,
        __tostring = function() return "Enum.RunContext." .. name end,
    })
    contextItems[name] = proxy
    contextByItem[proxy] = value
end
Enum = table.freeze({RunContext = table.freeze(contextItems)})
function _woorf_context_get(script)
    local value = script._RunContext
    for item, context in contextByItem do if context == value then return item end end
end
function _woorf_context_set(script, item)
    local value = contextByItem[item]
    assert(value ~= nil and value ~= 3, "Expected supported Enum.RunContext item")
    script._RunContext = value
end
