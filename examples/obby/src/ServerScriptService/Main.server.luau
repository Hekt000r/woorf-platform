local course = require(script.Parent.Course).build()
local players = game:GetService("Players")
players.PlayerAdded:Connect(function(player) course:addPlayer(player) end)
for _, player in players:GetPlayers() do course:addPlayer(player) end
players.CharacterAutoLoads = true

local restart = Instance.new("RemoteEvent", game:GetService("ReplicatedStorage"))
restart.Name = "ObbyRestart"
local lastRestart = {}
restart.OnServerEvent:Connect(function(player, ...)
    if select("#", ...) ~= 0 then return end
    if lastRestart[player] and course.time - lastRestart[player] < 1 then return end
    lastRestart[player] = course.time
    course:restartPlayer(player)
end)
players.PlayerRemoving:Connect(function(player) lastRestart[player] = nil end)
game:GetService("RunService").Heartbeat:Connect(function(delta) course:update(delta) end)
print("Spinner Obby: jump the pads and kill strips, save at the checkpoint, then dodge the spinners and cross the beam!")
