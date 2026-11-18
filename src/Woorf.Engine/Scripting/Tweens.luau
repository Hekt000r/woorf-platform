local tweenInfo = setmetatable({}, {__mode = "k"})
local easingStyle, easingDirection, playback = Enum.EasingStyle, Enum.EasingDirection, Enum.PlaybackState

TweenInfo = table.freeze({new = function(time, style, direction, repeatCount, reverses, delayTime)
    time, style, direction = time or 1, style or easingStyle.Quad, direction or easingDirection.Out
    repeatCount, delayTime = repeatCount or 0, delayTime or 0
    assert(type(time) == "number" and time >= 0 and time < math.huge, "Invalid tween duration")
    assert(type(delayTime) == "number" and delayTime >= 0 and delayTime < math.huge, "Invalid tween delay")
    assert(type(repeatCount) == "number" and repeatCount >= -1 and repeatCount % 1 == 0, "Invalid repeat count")
    assert(typeof(style) == "EnumItem" and style.EnumType == easingStyle, "Expected Enum.EasingStyle")
    assert(typeof(direction) == "EnumItem" and direction.EnumType == easingDirection, "Expected Enum.EasingDirection")
    assert(reverses == nil or type(reverses) == "boolean", "Expected boolean")
    local info = table.freeze({Time = time, EasingStyle = style, EasingDirection = direction,
        RepeatCount = repeatCount, Reverses = reverses == true, DelayTime = delayTime})
    local proxy = newproxy(true)
    local meta = getmetatable(proxy)
    meta.__type, meta.__index, meta.__metatable = "TweenInfo", info, "The metatable is locked"
    tweenInfo[proxy] = info
    return proxy
end})

local function bounceOut(x)
    if x < 1 / 2.75 then return 7.5625 * x * x end
    if x < 2 / 2.75 then x -= 1.5 / 2.75; return 7.5625 * x * x + 0.75 end
    if x < 2.5 / 2.75 then x -= 2.25 / 2.75; return 7.5625 * x * x + 0.9375 end
    x -= 2.625 / 2.75
    return 7.5625 * x * x + 0.984375
end

local curves = {
    Linear = function(x) return x end,
    Sine = function(x) return 1 - math.cos(x * math.pi / 2) end,
    Back = function(x) return x * x * (2.70158 * x - 1.70158) end,
    Quad = function(x) return x ^ 2 end,
    Cubic = function(x) return x ^ 3 end,
    Quart = function(x) return x ^ 4 end,
    Quint = function(x) return x ^ 5 end,
    Bounce = function(x) return 1 - bounceOut(1 - x) end,
    Elastic = function(x) return -(2 ^ (10 * x - 10)) * math.sin((x * 10 - 10.75) * 2 * math.pi / 3) end,
    Exponential = function(x) return 2 ^ (10 * x - 10) end,
    Circular = function(x) return 1 - math.sqrt(1 - x * x) end,
}

local function getValue(x, style, direction)
    assert(typeof(style) == "EnumItem" and style.EnumType == easingStyle, "Expected Enum.EasingStyle")
    assert(typeof(direction) == "EnumItem" and direction.EnumType == easingDirection, "Expected Enum.EasingDirection")
    x = math.clamp(x, 0, 1)
    if x == 0 or x == 1 then return x end
    local curve = curves[style.Name]
    if direction == easingDirection.In then return curve(x) end
    if direction == easingDirection.Out then return 1 - curve(1 - x) end
    if x < 0.5 then return curve(2 * x) / 2 end
    return 1 - curve(2 - 2 * x) / 2
end

function _woorf_TweenGetValue(_, alpha, style, direction)
    return getValue(alpha, style, direction)
end

local function lerp(a, b, t)
    if t == 0 then return a end
    if t == 1 then return b end
    local kind = typeof(a)
    if kind == "number" or kind == "Vector3" then return a + (b - a) * t end
    if kind == "Color3" or kind == "UDim2" or kind == "Vector2" then return a:Lerp(b, t) end
    if kind == "UDim" then return UDim.new(a.Scale + (b.Scale - a.Scale) * t, a.Offset + (b.Offset - a.Offset) * t) end
    if kind == "Rect" then return Rect.new(a.Min:Lerp(b.Min, t), a.Max:Lerp(b.Max, t)) end
    if kind == "boolean" then return a end
    error("Unsupported tween value " .. kind)
end

local tweens = setmetatable({}, {__mode = "k"})
local active = setmetatable({}, {__mode = "k"})

function _woorf_TweenCreate(service, target, info, goals)
    local options = assert(tweenInfo[info], "Expected TweenInfo")
    assert(type(goals) == "table", "Expected tween property table")
    local tween = service:_CreateTween(target)
    local properties = {}
    for property, goal in pairs(goals) do
        local start = target[property]
        assert(typeof(start) == typeof(goal), "Incompatible tween goal for " .. property)
        lerp(start, goal, 0.5)
        properties[property] = goal
    end
    tweens[tween] = {target = target, info = options, goals = properties, generation = 0}
    return tween
end

local function release(tween, data)
    local properties = active[data.target]
    if properties then
        for name in pairs(data.goals) do
            if properties[name] == tween then properties[name] = nil end
        end
        if next(properties) == nil then active[data.target] = nil end
    end
end

function _woorf_TweenCancel(tween)
    local data = assert(tweens[tween], "Expected Tween")
    data.generation += 1
    data.elapsed = nil
    release(tween, data)
    tween:_SetTweenState(playback.Cancelled)
end

function _woorf_TweenPause(tween)
    local data = assert(tweens[tween], "Expected Tween")
    if tween.PlaybackState ~= playback.Playing then return end
    data.generation += 1
    tween:_SetTweenState(playback.Paused)
end

local function playTimeline(tween, data, generation)
    local options = data.info
    local span = options.Time * (options.Reverses and 2 or 1)
    local cycle = span + options.DelayTime
    local total = cycle * (options.RepeatCount + 1)
    while data.generation == generation do
        local elapsed = data.elapsed
        local done = options.RepeatCount >= 0 and elapsed >= total
        local position = if done or cycle == 0 then span else elapsed % cycle - options.DelayTime
        if position < 0 then
            tween:_SetTweenState(playback.Delayed)
        else
            tween:_SetTweenState(playback.Playing)
            local alpha = if options.Time == 0 then 1 else position / options.Time
            if options.Reverses and alpha > 1 then alpha = 2 - alpha end
            if done and options.Reverses then alpha = 0 end
            local weight = getValue(alpha, options.EasingStyle, options.EasingDirection)
            for name, goal in pairs(data.goals) do data.target[name] = lerp(data.starts[name], goal, weight) end
        end
        if done then
            data.elapsed = nil
            release(tween, data)
            tween:_SetTweenState(playback.Completed)
            return
        end
        local delta = task.wait()
        if data.generation ~= generation then return end
        data.elapsed += delta
    end
end

function _woorf_TweenPlay(tween)
    local data = assert(tweens[tween], "Expected Tween")
    local state = tween.PlaybackState
    if state == playback.Playing or state == playback.Delayed then return end
    if state ~= playback.Paused then
        data.starts = {}
        for name in pairs(data.goals) do
            local previous = active[data.target] and active[data.target][name]
            if previous and previous ~= tween then _woorf_TweenCancel(previous) end
            if not active[data.target] then active[data.target] = {} end
            active[data.target][name] = tween
            data.starts[name] = data.target[name]
        end
        data.elapsed = 0
    end
    data.generation += 1
    tween:_SetTweenState(data.elapsed < data.info.DelayTime and playback.Delayed or playback.Playing)
    task.defer(playTimeline, tween, data, data.generation)
end
