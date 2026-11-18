local Arena = {}
Arena.__index = Arena

local function part(parent, name, size, position, color, canCollide)
    local p = Instance.new("Part", parent)
    p.Name = name; p.Size = size; p.Position = position; p.Anchored = true
    p.CanCollide = canCollide ~= false; p.CanTouch = false
    p.CustomPhysicalProperties = PhysicalProperties.new(.7, .5, 0)
    p:SetAttribute("RenderColor", color)
    return p
end

function Arena.new()
    local self = setmetatable({tiles = {}, effects = {}, clock = 0}, Arena)
    self.folder = Instance.new("Folder", workspace); self.folder.Name = "SkyArena"
    self:buildScenery()
    self:reset()
    return self
end

function Arena:buildScenery()
    local dark = Color3.fromRGB(22, 28, 54)
    local cyan = Color3.fromRGB(40, 220, 235)
    part(self.folder, "SpectatorDeck", Vector3.new(38, 2, 13), Vector3.new(0, 1, 38), dark)
    part(self.folder, "DeckStripe", Vector3.new(38, .15, .4), Vector3.new(0, 2.1, 32), cyan, false)
    for _, x in {-19, 19} do
        part(self.folder, "DeckRail", Vector3.new(.6, 4, 13), Vector3.new(x, 4, 38), cyan)
        part(self.folder, "BeaconTower", Vector3.new(2, 10, 2), Vector3.new(x, -3, 38), dark, false)
        part(self.folder, "TowerLight", Vector3.new(2.2, .6, 2.2), Vector3.new(x, 2.5, 38), cyan, false)
    end
    part(self.folder, "RearRail", Vector3.new(38, 4, .6), Vector3.new(0, 4, 44.5), dark)
    for index = 1, 8 do
        local angle = index * math.pi / 4
        local center = Vector3.new(math.cos(angle)*65, -10-index%3*4, math.sin(angle)*65)
        part(self.folder, "FloatingIsland", Vector3.new(13, 3, 13), center, dark, false)
        part(self.folder, "IslandCap", Vector3.new(14, .5, 14), center+Vector3.new(0, 1.7, 0), Color3.fromRGB(105, 75, 205), false)
        local pillar = part(self.folder, "Obelisk", Vector3.new(2, 13, 2), center+Vector3.new(0, 8, 0), cyan, false)
        pillar.CFrame *= CFrame.Angles(0, angle, .15)
    end
end

function Arena:reset()
    if self.tileFolder then self.tileFolder:Destroy() end
    for _, effect in self.effects do effect.part:Destroy() end
    self.effects = {}; self.tiles = {}; self.clock = 0
    self.tileFolder = Instance.new("Folder", self.folder); self.tileFolder.Name = "Tiles"
    for x = -3, 3 do
        for z = -3, 3 do
            local position = Vector3.new(x*6, 0, z*6)
            local color = (x+z)%2 == 0 and Color3.fromRGB(55, 115, 175) or Color3.fromRGB(75, 145, 190)
            if x == 0 and z == 0 then color = Color3.fromRGB(140, 95, 240) end
            local p = part(self.tileFolder, "Tile_"..x.."_"..z, Vector3.new(5.8, 1.2, 5.8), position, color)
            table.insert(self.tiles, {part=p, center=position, ring=math.max(math.abs(x), math.abs(z)), state="Solid", color=color})
        end
    end
end

function Arena:safeAt(position)
    for _, tile in self.tiles do
        if tile.state == "Solid" and math.abs(position.X-tile.center.X)<3 and math.abs(position.Z-tile.center.Z)<3 then return tile end
    end
    return nil
end

function Arena:nearestSafe(position)
    local best, score
    for _, tile in self.tiles do
        if tile.state == "Solid" then
            local distance = (Vector3.new(position.X,0,position.Z)-tile.center).Magnitude
            if not score or distance<score then best=tile; score=distance end
        end
    end
    return best
end

function Arena:effect(position, color, size, lifetime)
    for index = 1, 4 do
        local horizontal = index<=2
        local offset = horizontal and Vector3.new(0,0,(index==1 and -1 or 1)*size/2)
            or Vector3.new((index==3 and -1 or 1)*size/2,0,0)
        local shape = horizontal and Vector3.new(size,.18,.22) or Vector3.new(.22,.18,size)
        local p = part(self.folder,"Pulse",shape,position+offset,color,false)
        table.insert(self.effects,{part=p,untilTime=self.clock+lifetime})
    end
end

function Arena:update(elapsed, delta)
    self.clock += delta
    for index = #self.effects, 1, -1 do
        if self.clock>=self.effects[index].untilTime then self.effects[index].part:Destroy(); table.remove(self.effects,index) end
    end
    local ring = 3-math.floor(math.max(0,elapsed-10)/14)
    for _, tile in self.tiles do
        if elapsed>=10 and tile.ring>=ring and tile.state=="Solid" then
            tile.state="Warning"; tile.warnedAt=elapsed
        end
        if tile.state=="Warning" then
            local pulse = math.sin(elapsed*18)>0
            tile.part:SetAttribute("RenderColor",pulse and Color3.fromRGB(255,75,45) or Color3.fromRGB(255,190,45))
            if elapsed-tile.warnedAt>=1.6 then tile.state="Falling"; tile.fellAt=elapsed; tile.part.CanCollide=false end
        elseif tile.state=="Falling" and not tile.part:IsDescendantOf(workspace) then
            tile.state="Gone"
        elseif tile.state=="Falling" then
            local age=elapsed-tile.fellAt
            tile.part.Position=tile.center-Vector3.new(0,age*age*15,0)
            if age>1.4 then tile.part:Destroy(); tile.state="Gone" end
        end
    end
end

return Arena
