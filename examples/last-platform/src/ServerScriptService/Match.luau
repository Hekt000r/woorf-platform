local Arena=require(script.Parent.Arena)
local Roster=require(script.Parent.Roster)
local Combat=require(script.Parent.Combat)
local Bots=require(script.Parent.Bots)
local Match={}
Match.__index=Match

function Match.new()
    local self=setmetatable({time=0,round=0,phase="Countdown",untilTime=6,botsEnabled=true,roundStarted=0},Match)
    workspace.Gravity=80; workspace:SetAttribute("CharacterSpawn",Vector3.new(0,5,38))
    local players=game:GetService("Players"); players.RespawnTime=1; players.CharacterAutoLoads=true
    self.state=Instance.new("Folder",game:GetService("ReplicatedStorage")); self.state.Name="ArenaState"
    self.arena=Arena.new(); self.roster=Roster.new(); self.combat=Combat.new(self.arena,self.roster)
    self.bots=Bots.new(self.arena,self.roster,self.combat)
    self.message="The island is breaking. Be the last one standing!"
    self:publish()
    return self
end

function Match:addPlayer(player)
    self.roster:addPlayer(player)
    self:publish()
end

function Match:startRound()
    self.arena:reset(); self.roster:beginRound(self.botsEnabled)
    self.round+=1; self.phase="Fight"; self.roundStarted=self.time; self.untilTime=self.time+65
    self.contestants=#self.roster.actors
    self.message=self.contestants>1 and "Shove rivals off. Orange tiles are about to fall!" or "PRACTICE · Bots are off. Survive the collapse!"
end

function Match:finish(winner)
    self.phase="Result"; self.untilTime=self.time+5
    self.message=winner and (winner.name.." WINS THE ROUND!") or "DOUBLE KNOCKOUT · No winner!"
    if winner then
        winner.wins+=1
        if winner.player then winner.player:SetAttribute("ArenaWins",winner.wins) else self.roster.scores[winner.name]=winner.wins end
        if winner.wins>=3 then
            self.message=winner.name.." IS THE SKY CHAMPION!"; self.untilTime=self.time+8; self.champion=true
        end
        self.arena:effect(winner.root.Position-Vector3.new(0,1.8,0),Color3.fromRGB(255,210,65),8,3)
    end
end

function Match:request(player,action,...)
    if select("#",...)~=0 or type(action)~="string" then return false end
    local actor=self.roster.byPlayer[player]
    if not actor or player.Parent~=game:GetService("Players") then return false end
    if action=="Bots" then
        if game:GetService("Players"):GetPlayers()[1]~=player then return false end
        if (actor.toggleAt or 0)>self.time then return false end
        actor.toggleAt=self.time+1; self.botsEnabled=not self.botsEnabled
        self:publish(); return true
    end
    if self.phase~="Fight" or not actor.alive then return false end
    if action=="Shove" then return self.combat:shove(actor,self.time) end
    if action=="Boost" then return self.combat:boost(actor,self.time) end
    return false
end

function Match:publish()
    local alive=0
    for _,actor in self.roster.actors do if actor.alive then alive+=1 end end
    local host=game:GetService("Players"):GetPlayers()[1]
    local values={Phase=self.phase,TimeLeft=math.max(0,math.ceil(self.untilTime-self.time)),Round=self.round,
        Alive=alive,BotsEnabled=self.botsEnabled,Leaderboard=self.roster:leaderboard(),Message=self.message,HostId=host and host.UserId or 0}
    for name,value in values do self.state:SetAttribute(name,value) end
end

function Match:update(delta)
    self.time+=delta
    local living=self.roster:update(self.time,delta)
    if self.phase=="Fight" then
        self.arena:update(self.time-self.roundStarted,delta)
        self.combat:update(self.time); self.bots:update(self.time)
        if #living<=1 and self.contestants>1 then self:finish(living[1])
        elseif self.time>=self.untilTime or #living==0 then self:finish(nil) end
    elseif self.time>=self.untilTime and #game:GetService("Players"):GetPlayers()>0 then
        if self.champion then
            for _,actor in self.roster.actors do actor.wins=0; if actor.player then actor.player:SetAttribute("ArenaWins",0) end end
            self.roster.scores={}; self.champion=false; self.round=0
        end
        self:startRound()
    end
    self:publish()
end

return Match
