local function makeReadOnly(kind, values, methods, equals)
    local meta = {}
    meta.__type = kind
    meta.__index = function(_, key)
        local value = values[key]
        if value ~= nil then return value end
        if methods and methods[key] then return methods[key] end
        error(key .. " is not a valid member of " .. kind, 2)
    end
    meta.__newindex = function() error(kind .. " is read-only", 2) end
    meta.__eq = equals
    meta.__metatable = "The metatable is locked"
    return _woorf_new_proxy(meta)
end

local colorMethods = {}
local function colorEquals(a, b) return a.R == b.R and a.G == b.G and a.B == b.B end
local colorMeta = {
    __type = "Color3",
    __eq = colorEquals,
    __metatable = "The metatable is locked",
    __newindex = function() error("Color3 is read-only", 2) end,
    __index = function(self, key)
        local red, green, blue = _woorf_color_get(self)
        if key == "R" then return red elseif key == "G" then return green elseif key == "B" then return blue end
        if colorMethods[key] then return colorMethods[key] end
        error(key .. " is not a valid member of Color3", 2)
    end,
}
_woorf_set_color_metatable(colorMeta)
local function colorNew(red, green, blue) return _woorf_color_new(red, green, blue) end

function colorMethods.Lerp(self, other, alpha)
    assert(typeof(other) == "Color3" and type(alpha) == "number", "Expected Color3 and number")
    return colorNew(self.R + (other.R-self.R)*alpha, self.G + (other.G-self.G)*alpha, self.B + (other.B-self.B)*alpha)
end

local function colorHSV(hue, saturation, value)
    local segment = (hue % 1) * 6
    local blend = segment % 1
    local low = value * (1 - saturation)
    local falling = value * (1 - saturation * blend)
    local rising = value * (1 - saturation * (1 - blend))
    local colors = {
        {value, rising, low}, {falling, value, low}, {low, value, rising},
        {low, falling, value}, {rising, low, value}, {value, low, falling},
    }
    return colorNew(table.unpack(colors[math.floor(segment) + 1]))
end

function colorMethods.ToHSV(self)
    local maximum, minimum = math.max(self.R, self.G, self.B), math.min(self.R, self.G, self.B)
    local difference, hue = maximum - minimum, 0
    if difference ~= 0 then
        if maximum == self.R then hue = ((self.G - self.B) / difference) % 6
        elseif maximum == self.G then hue = (self.B - self.R) / difference + 2
        else hue = (self.R - self.G) / difference + 4 end
    end
    return hue / 6, if maximum == 0 then 0 else difference / maximum, maximum
end

Color3 = table.freeze({new = colorNew, fromRGB = function(r,g,b) return colorNew(r/255,g/255,b/255) end, fromHSV = colorHSV})

local function randomNew(seed)
    seed = seed or math.random(1, 2147483646)
    assert(type(seed) == "number" and seed == seed and math.abs(seed) < math.huge, "Expected finite seed")
    local state = math.floor(seed) % 2147483647
    if state == 0 then state = 1 end
    local methods = {}
    local function nextUnit() state = (state*48271)%2147483647; return (state-1)/2147483646 end
    function methods.NextNumber(_, minimum, maximum)
        minimum, maximum = minimum or 0, maximum or 1
        assert(type(minimum) == "number" and type(maximum) == "number" and minimum <= maximum, "Invalid range")
        return minimum + (maximum-minimum)*nextUnit()
    end
    function methods.NextInteger(_, minimum, maximum)
        assert(type(minimum) == "number" and type(maximum) == "number" and minimum%1 == 0 and maximum%1 == 0
            and minimum <= maximum and math.abs(minimum) <= 2147483647 and math.abs(maximum) <= 2147483647, "Invalid integer range")
        return minimum + math.floor(nextUnit()*(maximum-minimum+1))
    end
    function methods.Clone() return randomNew(state) end
    function methods.NextUnitVector()
        local z = nextUnit() * 2 - 1
        local angle = nextUnit() * math.pi * 2
        local radius = math.sqrt(1 - z * z)
        return Vector3.new(math.cos(angle) * radius, math.sin(angle) * radius, z)
    end
    return makeReadOnly("Random", {}, methods)
end
Random = table.freeze({new = randomNew})

function _woorf_add_item(service, instance, lifetime)
    assert(service.ClassName == "Debris" and typeof(instance) == "Instance", "Expected Debris and Instance")
    lifetime = lifetime or 10
    assert(type(lifetime) == "number" and lifetime >= 0 and lifetime < math.huge, "Invalid lifetime")
    local previousOwner = currentOwner
    currentOwner = nil
    task.delay(lifetime, function() instance:Destroy() end)
    currentOwner = previousOwner
end

function warn(...)
    local arguments = table.pack(...)
    for index = 1, arguments.n do arguments[index] = tostring(arguments[index]) end
    _woorf_report(table.concat(arguments, "\t", 1, arguments.n))
end

local queryParameters = setmetatable({}, {__mode = "k"})
RaycastParams = table.freeze({new = function()
    local values = {FilterDescendantsInstances = {}, FilterType = Enum.RaycastFilterType.Exclude, RespectCanCollide = false}
    local proxy = _woorf_new_proxy({
        __type = "RaycastParams", __metatable = "The metatable is locked",
        __index = function(_, key)
            assert(values[key] ~= nil, "Unsupported RaycastParams member: " .. tostring(key))
            return if key == "FilterDescendantsInstances" then table.clone(values[key]) else values[key]
        end,
        __newindex = function(_, key, value)
            if key == "FilterDescendantsInstances" then
                assert(type(value) == "table", "Expected Instance array")
                local copy = {}
                for index, instance in value do
                    assert(type(index) == "number" and index >= 1 and index%1 == 0 and index <= #value
                        and typeof(instance) == "Instance", "Expected Instance array")
                    copy[index] = instance
                end
                values[key] = copy
            elseif key == "FilterType" then
                assert(value == Enum.RaycastFilterType.Exclude or value == Enum.RaycastFilterType.Include, "Expected RaycastFilterType")
                values[key] = value
            elseif key == "RespectCanCollide" then
                assert(type(value) == "boolean", "Expected boolean")
                values[key] = value
            else error("Unsupported RaycastParams member: " .. tostring(key), 2) end
        end,
    })
    queryParameters[proxy] = values
    return proxy
end})
local function queryOptions(params)
    if params == nil then return nil end
    local values = queryParameters[params]
    assert(values, "Expected RaycastParams")
    return {Instances = values.FilterDescendantsInstances, Include = values.FilterType == Enum.RaycastFilterType.Include,
        RespectCanCollide = values.RespectCanCollide}
end
function _woorf_raycast(self, origin, direction, params)
    local result = self:_Raycast(origin, direction, queryOptions(params))
    if result == nil then return nil end
    return makeReadOnly("RaycastResult", result)
end
function _woorf_spherecast(self, origin, radius, direction, params)
    local result = self:_Spherecast(origin, radius, direction, queryOptions(params))
    if result == nil then return nil end
    return makeReadOnly("RaycastResult", result)
end
local shutdownPending = 0
function _woorf_wrap_shutdown(callback)
    return function()
        shutdownPending += 1
        local outcome = table.pack(pcall(callback))
        shutdownPending -= 1
        if not outcome[1] then error(outcome[2], 0) end
    end
end
function _woorf_shutdown_pending() return shutdownPending end
