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

