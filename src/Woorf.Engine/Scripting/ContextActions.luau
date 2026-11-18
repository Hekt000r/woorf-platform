local contextStates = setmetatable({}, {__mode = "k"})

local function runContextAction(action, input)
    local result
    local thread = coroutine.create(function()
        result = action.callback(action.name, input.UserInputState, input)
    end)
    if not trackThread(thread, action.owner) then return Enum.ContextActionResult.Pass end
    resumeThread(thread, table.pack(), "Context action failed: ")
    return result
end

local function matchesContextInput(action, input)
    return action.inputs[input.KeyCode] or action.inputs[input.UserInputType]
end

local function dispatchContextInput(service, state, input)
    local matches = {}
    local identity = input.UserInputType.Name .. ":" .. input.KeyCode.Name
    for _, action in state.actions do
        if matchesContextInput(action, input) then table.insert(matches, action) end
    end
    table.sort(matches, function(left, right)
        if left.priority == right.priority then return left.sequence > right.sequence end
        return left.priority > right.priority
    end)
    for _, action in matches do
        if state.actions[action.name] ~= action then continue end
        action.active[identity] = if input.UserInputState == Enum.UserInputState.Begin then input else nil
        if runContextAction(action, input) ~= Enum.ContextActionResult.Pass then
            service:_SinkInput()
            break
        end
    end
    if input.UserInputState == Enum.UserInputState.End then
        for _, action in state.actions do action.active[identity] = nil end
    end
end

local function getContextState(service)
    assert(typeof(service) == "Instance" and service.ClassName == "ContextActionService", "Expected ContextActionService")
    local state = contextStates[service]
    if state then return state end
    local input = service._ActionInput
    state = {actions = {}, sequence = 0}
    local wasInternalConnection = isInternalConnection
    isInternalConnection = true
    input:Connect(function(value) dispatchContextInput(service, state, value) end)
    isInternalConnection = wasInternalConnection
    contextStates[service] = state
    return state
end

local function cancelContextAction(service, action)
    if next(action.active) == nil then return end
    table.clear(action.active)
    runContextAction(action, service:_CancelInput())
end

local function bindContextAction(service, name, callback, createTouchButton, priority, ...)
    assert(type(name) == "string" and type(callback) == "function", "Expected an action name and callback")
    assert(type(createTouchButton) == "boolean", "Expected createTouchButton boolean")
    assert(type(priority) == "number" and priority % 1 == 0, "Expected integer priority")
    local inputs = {}
    local arguments = table.pack(...)
    for index = 1, arguments.n do
        local input = arguments[index]
        assert(typeof(input) == "EnumItem"
            and (input.EnumType == Enum.KeyCode or input.EnumType == Enum.UserInputType), "Expected an input EnumItem")
        inputs[input] = true
    end
    local state = getContextState(service)
    if state.actions[name] then _woorf_context_UnbindAction(service, name) end
    state.sequence += 1
    local environment = getfenv(callback)
    local owner = environment and rawget(environment, "script")
    local action = {name = name, callback = callback, inputs = inputs, active = {},
        priority = priority, sequence = state.sequence, owner = owner}
    state.actions[name] = action
    local displaced = {}
    for _, bound in state.actions do
        if bound ~= action and priority >= bound.priority then
            for _, input in bound.active do
                if matchesContextInput(action, input) then table.insert(displaced, bound); break end
            end
        end
    end
    for _, bound in displaced do cancelContextAction(service, bound) end
    if owner then
        local wasInternalConnection = isInternalConnection
        isInternalConnection = true
        local connection
        connection = owner.Destroying:Connect(function()
            if state.actions[name] == action then state.actions[name] = nil end
        end)
        isInternalConnection = wasInternalConnection
        action.ownerConnection = connection
    end
end

function _woorf_context_BindAction(service, name, callback, createTouchButton, ...)
    bindContextAction(service, name, callback, createTouchButton, 2000, ...)
end

_woorf_context_BindActionToInputTypes = _woorf_context_BindAction

function _woorf_context_BindActionAtPriority(service, name, callback, createTouchButton, priority, ...)
    bindContextAction(service, name, callback, createTouchButton, priority, ...)
end

function _woorf_context_UnbindAction(service, name)
    assert(type(name) == "string", "Expected action name")
    local state = getContextState(service)
    local action = state.actions[name]
    if not action then return end
    state.actions[name] = nil
    if action.ownerConnection then action.ownerConnection:Disconnect() end
    cancelContextAction(service, action)
end

function _woorf_context_UnbindAllActions(service)
    local state = getContextState(service)
    local names = {}
    for name in state.actions do table.insert(names, name) end
    for _, name in names do _woorf_context_UnbindAction(service, name) end
end
