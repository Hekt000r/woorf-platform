local Bots = {}
Bots.__index = Bots

function Bots.new(arena,roster,combat) return setmetatable({arena=arena,roster=roster,combat=combat,nextThink=0},Bots) end

function Bots:think(actor,time)
    local position=actor.root.Position
    local target,nearest
    for _,other in self.roster.actors do
        if other~=actor and other.alive then
            local distance=(other.root.Position-position).Magnitude
            if not nearest or distance<nearest then target=other; nearest=distance end
        end
    end
    local safe=self.arena:nearestSafe(position)
    if not safe then actor.humanoid:Move(Vector3.zero); return end
    local destination=safe.center
    local standing=self.arena:safeAt(position)
    if standing and target and self.arena:safeAt(target.root.Position) then destination=target.root.Position end
    local offset=Vector3.new(destination.X-position.X,0,destination.Z-position.Z)
    local direction=offset.Magnitude>.6 and offset.Unit or Vector3.zero
    if standing and not self.arena:safeAt(position+direction*3) then
        local center=self.arena:nearestSafe(Vector3.zero)
        direction=center and Vector3.new(center.center.X-position.X,0,center.center.Z-position.Z) or Vector3.zero
        direction=direction.Magnitude>.1 and direction.Unit or Vector3.zero
    end
    if (actor.pushUntil or 0)>time then direction=Vector3.zero end
    actor.humanoid:Move(direction)
    if target and nearest<7 and standing then
        local face=target.root.Position-position; face=Vector3.new(face.X,0,face.Z)
        if face.Magnitude>.1 then actor.face=face.Unit end
        self.combat:shove(actor,time)
    end
    if not standing and offset.Magnitude>3 then self.combat:boost(actor,time) end
end

function Bots:update(time)
    if time<self.nextThink then return end
    self.nextThink=time+.18
    for _,actor in self.roster.actors do
        if actor.isBot and actor.alive then self:think(actor,time) end
    end
end

return Bots
