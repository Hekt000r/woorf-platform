local guiMethods = {{}, {}, {}, {}}
local vectorMethods, dimensionMethods = guiMethods[1], guiMethods[3]
local function values(self, kind) return _woorf_gui_get(self, kind) end
local function construct(kind, a, b, c, d) return _woorf_gui_new(kind, a or 0, b or 0, c or 0, d or 0) end
local function vector2(x, y) return construct(0, x, y) end
local function udim(scale, offset) return construct(1, scale, math.floor(offset or 0)) end
local function udim2(a, b, c, d)
    if typeof(a) == "UDim" then
        assert(typeof(b) == "UDim", "Expected two UDim values")
        return construct(2, a.Scale, a.Offset, b.Scale, b.Offset)
    end
    return construct(2, a, math.floor(b or 0), c, math.floor(d or 0))
end
local guiEnumDefinitions = _woorf_gui_enum_definitions
local guiEnumTables = {}
local enumTypes = table.clone(Enum)
local function getField(self, kind, key)
    local a, b, c, d = values(self, kind)
    if kind == 0 then
        if key == "X" then return a elseif key == "Y" then return b end
        if key == "Magnitude" then return math.sqrt(a*a+b*b) end
        if key == "Unit" then local length = math.sqrt(a*a+b*b); return vector2(a/length, b/length) end
    elseif kind == 1 then
        if key == "Scale" then return a elseif key == "Offset" then return b end
    elseif kind == 2 then
        if key == "X" or key == "Width" then return udim(a,b) end
        if key == "Y" or key == "Height" then return udim(c,d) end
    else
        if key == "Name" then return guiEnumDefinitions[a][2][b+1] end
        if key == "Value" then return guiEnumDefinitions[a][3][b+1] end
        if key == "EnumType" then return enumTypes[guiEnumDefinitions[a][1]] end
    end
    if guiMethods[kind+1][key] then return guiMethods[kind+1][key] end
    error("Unsupported GUI datatype member: " .. tostring(key), 2)
end
local function equals(kind, left, right)
    if typeof(left) ~= typeof(right) then return false end
    local a,b,c,d = values(left,kind)
    local e,f,g,h = values(right,kind)
    return a == e and b == f and c == g and d == h
end
local function combine(kind, left, right, sign)
    local a,b,c,d = values(left,kind)
    local e,f,g,h = values(right,kind)
    return construct(kind,a+e*sign,b+f*sign,c+g*sign,d+h*sign)
end
local function multiply(left, right, divide)
    if type(left) == "number" then left,right = right,left end
    local a,b = values(left,0)
    local c,d = right,right
    if typeof(right) == "Vector2" then c,d = values(right,0) end
    if divide then return vector2(a/c,b/d) end
    return vector2(a*c,b*d)
end
for index, name in {"Vector2", "UDim", "UDim2", "EnumItem"} do
    local kind = index - 1
    local meta = {
        __type = name, __metatable = "The metatable is locked",
        __index = function(self,key) return getField(self,kind,key) end,
        __newindex = function() error(name .. " is read-only",2) end,
        __eq = function(a,b) return equals(kind,a,b) end,
        __tostring = function(self)
            local a,b,c,d = values(self,kind)
            if kind == 3 then return "Enum." .. guiEnumDefinitions[a][1] .. "." .. guiEnumDefinitions[a][2][b+1] end
            return if kind == 2 then a .. ", " .. b .. ", " .. c .. ", " .. d else a .. ", " .. b
        end,
    }
    if kind < 3 then
        meta.__add = function(a,b) return combine(kind,a,b,1) end
        meta.__sub = function(a,b) return combine(kind,a,b,-1) end
        meta.__unm = function(self) local a,b,c,d = values(self,kind); return construct(kind,-a,-b,-c,-d) end
    end
    if kind == 0 then
        meta.__mul = function(a,b) return multiply(a,b,false) end
        meta.__div = function(a,b) return multiply(a,b,true) end
    end
    _woorf_gui_metatable(kind,meta)
end
for kind, definition in guiEnumDefinitions do
    local items = {}
    for index, name in definition[2] do items[name] = construct(3, kind, index - 1) end
    guiEnumTables[kind] = items
    enumTypes[definition[1]] = table.freeze(items)
end
Enum = table.freeze(enumTypes)
function vectorMethods.Dot(self,other) return self.X*other.X+self.Y*other.Y end
function vectorMethods.Cross(self,other) return self.X*other.Y-self.Y*other.X end
function vectorMethods.Lerp(self,other,alpha) return self+(other-self)*alpha end
function vectorMethods.Abs(self) return vector2(math.abs(self.X),math.abs(self.Y)) end
function vectorMethods.Ceil(self) return vector2(math.ceil(self.X),math.ceil(self.Y)) end
function vectorMethods.Floor(self) return vector2(math.floor(self.X),math.floor(self.Y)) end
function vectorMethods.Sign(self) return vector2(math.sign(self.X),math.sign(self.Y)) end
function vectorMethods.Min(self,...)
    local x,y = self.X,self.Y
    for _,other in {...} do x,y = math.min(x,other.X),math.min(y,other.Y) end
    return vector2(x,y)
end
function vectorMethods.Max(self,...)
    local x,y = self.X,self.Y
    for _,other in {...} do x,y = math.max(x,other.X),math.max(y,other.Y) end
    return vector2(x,y)
end
function vectorMethods.FuzzyEq(self,other,epsilon)
    epsilon = epsilon or 1e-5
    return (self-other).Magnitude <= epsilon * math.max(1,self.Magnitude,other.Magnitude)
end
function vectorMethods.Angle(self,other,isSigned)
    local angle = math.atan2(self:Cross(other),self:Dot(other))
    return if isSigned then angle else math.abs(angle)
end
function dimensionMethods.Lerp(self,other,alpha)
    local a,b,c,d = values(self,2)
    local e,f,g,h = values(other,2)
    return udim2(a+(e-a)*alpha,b+(f-b)*alpha,c+(g-c)*alpha,d+(h-d)*alpha)
end
Vector2 = table.freeze({new = vector2, zero = vector2(), one = vector2(1,1), xAxis = vector2(1,0), yAxis = vector2(0,1)})
UDim = table.freeze({new = udim})
UDim2 = table.freeze({new = udim2, fromScale = function(x,y) return udim2(x,0,y,0) end,
    fromOffset = function(x,y) return udim2(0,x,0,y) end})


_woorf_gui_metatable(4, {
    __type = "BrickColor",
    __metatable = "The metatable is locked",
    __index = _woorf_brickcolor_get,
    __newindex = function() error("BrickColor is read-only", 2) end,
    __eq = function(a, b) return equals(4, a, b) end,
    __tostring = function(value) return value.Name end,
})

_woorf_gui_metatable(5, {
    __type = "Font",
    __metatable = "The metatable is locked",
    __index = _woorf_font_get,
    __newindex = function() error("Font is read-only", 2) end,
    __eq = function(a, b)
        return typeof(b) == "Font" and a.Family == b.Family and a.Weight == b.Weight and a.Style == b.Style
    end,
})

local newSequence, getSequence = _woorf_sequence_new, _woorf_sequence_get
_woorf_sequence_new, _woorf_sequence_get = nil, nil

local names = {"ColorSequenceKeypoint", "NumberSequenceKeypoint", "ColorSequence", "NumberSequence"}
local function makeKeypoint(kind, time, value, envelope)
    assert(type(time) == "number" and time >= 0 and time <= 1, "Keypoint time must be between 0 and 1")
    if kind == 0 then
        assert(typeof(value) == "Color3", "Expected Color3")
        return newSequence(kind, time, value.R, value.G, value.B)
    end
    envelope = if envelope == nil then 0 else envelope
    local maxFloat = 3.4028234663852886e38
    assert(type(value) == "number" and math.abs(value) <= maxFloat, "Expected finite number")
    assert(type(envelope) == "number" and envelope >= 0 and envelope <= maxFloat, "Expected nonnegative envelope")
    return newSequence(kind, time, value, envelope)
end

local function makeSequence(kind, first, last)
    local keyKind = kind - 2
    local points = first
    if type(first) ~= "table" then
        points = {makeKeypoint(keyKind, 0, first), makeKeypoint(keyKind, 1, if last == nil then first else last)}
    end
    assert(#points >= 2 and #points <= 20, "Expected between 2 and 20 keypoints")
    local flat = {}
    local previous = 0
    for i = 1, #points do
        local point = points[i]
        assert(typeof(point) == names[keyKind + 1], "Invalid keypoint type")
        local parts = getSequence(point, keyKind)
        local time = parts[1]
        assert(time >= previous and (i ~= 1 or time == 0) and (i ~= #points or time == 1),
            "Keypoints must be ordered and span 0 to 1")
        previous = time
        for _, component in ipairs(parts) do table.insert(flat, component) end
    end
    return newSequence(kind, table.unpack(flat))
end

local function getProperty(value, kind, key)
    local parts = getSequence(value, kind)
    if kind < 2 then
        if key == "Time" then return parts[1] end
        if key == "Value" then
            return if kind == 0 then Color3.new(parts[2], parts[3], parts[4]) else parts[2]
        end
        if kind == 1 and key == "Envelope" then return parts[3] end
    elseif key == "Keypoints" then
        local points = {}
        local stride = if kind == 2 then 4 else 3
        for i = 1, #parts, stride do
            table.insert(points, newSequence(kind - 2, table.unpack(parts, i, i + stride - 1)))
        end
        return points
    end
    error("Unknown sequence member " .. tostring(key), 2)
end

for index, name in ipairs(names) do
    local kind = index - 1
    _woorf_gui_metatable(6 + kind, {
        __type = name,
        __metatable = "The metatable is locked",
        __index = function(value, key) return getProperty(value, kind, key) end,
        __newindex = function() error(name .. " is read-only", 2) end,
        __eq = function(a, b)
            if typeof(b) ~= name then return false end
            local left, right = getSequence(a, kind), getSequence(b, kind)
            if #left ~= #right then return false end
            for i, value in ipairs(left) do
                if value ~= right[i] then return false end
            end
            return true
        end,
    })
end

ColorSequenceKeypoint = table.freeze({new = function(t, c) return makeKeypoint(0, t, c) end})
NumberSequenceKeypoint = table.freeze({new = function(t, v, e) return makeKeypoint(1, t, v, e) end})
ColorSequence = table.freeze({new = function(a, b) return makeSequence(2, a, b) end})
NumberSequence = table.freeze({new = function(a, b) return makeSequence(3, a, b) end})

local function rectangle(a, b, c, d)
    if typeof(a) == "Vector2" then
        assert(typeof(b) == "Vector2", "Expected two Vector2 values")
        a, b, c, d = a.X, a.Y, b.X, b.Y
    end
    a, b, c, d = a or 0, b or 0, c or 0, d or 0
    return construct(10, math.min(a, c), math.min(b, d), math.max(a, c), math.max(b, d))
end

_woorf_gui_metatable(10, {
    __type = "Rect", __metatable = "The metatable is locked",
    __eq = function(a, b) return equals(10, a, b) end,
    __index = function(value, key)
        local x0, y0, x1, y1 = values(value, 10)
        if key == "Min" then return vector2(x0, y0) end
        if key == "Max" then return vector2(x1, y1) end
        if key == "Width" then return x1 - x0 end
        if key == "Height" then return y1 - y0 end
        error("Unknown Rect member " .. tostring(key), 2)
    end,
})
Rect = table.freeze({new = rectangle})
