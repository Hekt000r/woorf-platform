local Round = {}
local players = game:GetService("Players")
local ready, purchased = {}, {}
local elapsed = 0
local phase = "waiting"
local heartbeat

function Round.start(profiles)
    heartbeat = game:GetService("RunService").Heartbeat:Connect(function(delta)
        if phase ~= "running" then return end
        elapsed += delta
        if elapsed < 0.1 then return end
        phase = "complete"
        for _, player in ipairs(players:GetPlayers()) do
            if ready[player] then profiles.reward(player) end
        end
    end)
end

function Round.ready(player)
    if phase ~= "waiting" or ready[player] then return false end
    ready[player] = true
    local count = 0
    for _ in pairs(ready) do count += 1 end
    if count == 2 then phase = "running" end
    return true
end

function Round.buy(player, item, profiles)
    if phase ~= "complete" or not ready[player] or purchased[player] or item ~= "potion" then
        return false
    end
    if not profiles.buy(player) then return false end
    purchased[player] = true
    return true
end

function Round.phase() return phase end

function Round.remove(player)
    ready[player] = nil
    purchased[player] = nil
    if phase == "running" then phase = "waiting"; elapsed = 0 end
end

function Round.close() heartbeat:Disconnect() end

return Round
