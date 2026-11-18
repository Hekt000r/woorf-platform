local Roster = {}
Roster.__index = Roster
local colors = {Color3.fromRGB(60,235,220),Color3.fromRGB(235,95,195),Color3.fromRGB(165,125,255),Color3.fromRGB(255,200,70)}
local spawns = {Vector3.new(-12,4,12),Vector3.new(12,4,-12),Vector3.new(-12,4,-12),Vector3.new(12,4,12),Vector3.new(0,4,12),Vector3.new(0,4,-12)}

function Roster.new()
    local folder=Instance.new("Folder",workspace); folder.Name="ArenaBots"
    return setmetatable({actors={},byPlayer={},botFolder=folder,scores={}},Roster)
end

function Roster:attach(actor, character)
    actor.model=character; actor.root=character.PrimaryPart; actor.humanoid=character:FindFirstChild("Humanoid")
    if not actor.root or not actor.humanoid then return end
    actor.root.Position=actor.alive and actor.spawn or Vector3.new((actor.slot%4-1.5)*4,5,38)
    actor.root.AssemblyLinearVelocity=Vector3.zero
    actor.humanoid.WalkSpeed=actor.isBot and 13 or 16
    actor.humanoid.JumpPower=30
    if actor.marker then actor.marker:Destroy() end
    local marker=Instance.new("Part",character); marker.Name="PlayerBeacon"; marker.Shape=Enum.PartType.Ball
    marker.Size=Vector3.new(.65,.65,.65); marker.Anchored=true; marker.CanCollide=false; marker.CanTouch=false
    marker:SetAttribute("RenderColor",actor.color); actor.marker=marker
end

function Roster:addPlayer(player)
    if self.byPlayer[player] then return end
    local actor={player=player,name=player.Name,isBot=false,alive=false,slot=#self.actors+1,wins=0}
    actor.color=colors[(actor.slot-1)%#colors+1]; actor.face=Vector3.new(0,0,-1)
    actor.spawn=spawns[(actor.slot-1)%#spawns+1]
    self.byPlayer[player]=actor; table.insert(self.actors,actor)
    player:SetAttribute("ArenaWins",0); player:SetAttribute("ArenaAlive",false)
    player:SetAttribute("ViewDistance",30); player:SetAttribute("ViewPitch",.82)
    player.CharacterAdded:Connect(function(character) self:attach(actor,character) end)
    if player.Character then self:attach(actor,player.Character) end
end

function Roster:createBot(index)
    local names={"BOLT","GLITCH","NOVA"}
    local actor={name=names[index],isBot=true,alive=true,slot=#self.actors+1,wins=self.scores[names[index]] or 0}
    actor.color=colors[index]; actor.face=Vector3.new(0,0,1)
    actor.spawn=spawns[(actor.slot-1)%#spawns+1]
    local model=Instance.new("Model",self.botFolder); model.Name=actor.name
    local root=Instance.new("Part",model); root.Name="HumanoidRootPart"; root.Size=Vector3.new(2,4,2)
    root.CustomPhysicalProperties=PhysicalProperties.new(.7,0,0); model.PrimaryPart=root
    Instance.new("Humanoid",model)
    self:attach(actor,model); table.insert(self.actors,actor)
    return actor
end

function Roster:beginRound(botsEnabled)
    for index=#self.actors,1,-1 do
        local actor=self.actors[index]
        if actor.isBot or not actor.player.Parent then
            if actor.isBot and actor.model then actor.model:Destroy() end
            table.remove(self.actors,index)
        end
    end
    for index,actor in self.actors do
        actor.slot=index; actor.spawn=spawns[(index-1)%#spawns+1]; actor.alive=true
        actor.readyAt=0; actor.boostReadyAt=0; actor.pushUntil=0; actor.windup=nil
        actor.player:SetAttribute("ArenaAlive",true); actor.player:LoadCharacter()
    end
    if botsEnabled then for index=1,math.max(0,4-#self.actors) do self:createBot(index) end end
end

function Roster:update(time, delta)
    local living={}
    for _,actor in self.actors do
        if actor.alive and (not actor.root or actor.root.Position.Y < -7 or actor.humanoid.Health<=0
            or not actor.model:IsDescendantOf(workspace) or actor.player and not actor.player.Parent) then
            actor.alive=false
            if actor.player and actor.player.Parent then
                actor.player:SetAttribute("ArenaAlive",false); actor.player:LoadCharacter()
            elseif actor.model then actor.model:Destroy() end
        end
        if actor.root and actor.root.Parent then
            if actor.marker and actor.marker.Parent then actor.marker.Position=actor.root.Position+Vector3.new(0,2.8,0) end
            local direction=actor.humanoid.MoveDirection
            if direction.Magnitude>.1 then actor.face=direction end
            if actor.alive and (actor.pushUntil or 0)>time then actor.root.Position+=actor.push*delta end
            if actor.humanoid.JumpPower>30 and time>(actor.boostUntil or 0) then actor.humanoid.JumpPower=30 end
        end
        if actor.alive then table.insert(living,actor) end
        if actor.player and actor.player.Parent then
            actor.player:SetAttribute("ShoveCooldown",math.max(0,math.ceil(((actor.readyAt or 0)-time)*10)/10))
            actor.player:SetAttribute("BoostCooldown",math.max(0,math.ceil(((actor.boostReadyAt or 0)-time)*10)/10))
        end
    end
    return living
end

function Roster:leaderboard()
    local lines={}
    for _,actor in self.actors do table.insert(lines,(actor.isBot and "BOT " or "")..actor.name.."   "..actor.wins.." / 3") end
    return table.concat(lines,"\n")
end

return Roster
