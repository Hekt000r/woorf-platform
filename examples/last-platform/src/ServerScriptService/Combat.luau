local Combat = {}
Combat.__index = Combat

function Combat.new(arena,roster) return setmetatable({arena=arena,roster=roster},Combat) end

function Combat:shove(actor,time)
    if not actor.alive or not actor.root or time<(actor.readyAt or 0) then return false end
    actor.readyAt=time+1.6
    actor.windup={at=time+.3,face=actor.face}
    local position=actor.root.Position+actor.face*3-Vector3.new(0,1.9,0)
    self.arena:effect(position,Color3.fromRGB(255,175,55),6,.3)
    return true
end

function Combat:boost(actor,time)
    if not actor.alive or not actor.humanoid or time<(actor.boostReadyAt or 0) then return false end
    actor.boostReadyAt=time+6; actor.boostUntil=time+.2
    actor.humanoid.JumpPower=43; actor.humanoid.Jump=true
    self.arena:effect(actor.root.Position-Vector3.new(0,1.9,0),Color3.fromRGB(60,235,220),4,.4)
    return true
end

function Combat:resolve(actor,windup,time)
    if not actor.alive then return end
    local origin=actor.root.Position
    self.arena:effect(origin+windup.face*3-Vector3.new(0,1.8,0),Color3.fromRGB(255,245,210),7,.16)
    for _,target in self.roster.actors do
        if target~=actor and target.alive then
            local offset=target.root.Position-origin
            local flat=Vector3.new(offset.X,0,offset.Z)
            if flat.Magnitude<7.5 and math.abs(offset.Y)<5 and (flat.Magnitude<.1 or flat.Unit:Dot(windup.face)>.15) then
                target.push=flat.Magnitude>.1 and flat.Unit*42 or windup.face*42
                target.pushUntil=time+.34
                target.root.AssemblyLinearVelocity+=Vector3.new(0,9,0)
                self.arena:effect(target.root.Position-Vector3.new(0,1.7,0),Color3.fromRGB(255,75,130),3,.25)
            end
        end
    end
end

function Combat:update(time)
    for _,actor in self.roster.actors do
        if actor.windup and actor.windup.at<=time then
            local windup=actor.windup; actor.windup=nil; self:resolve(actor,windup,time)
        end
    end
end

return Combat
