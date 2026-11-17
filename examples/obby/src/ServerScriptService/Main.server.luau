local course = require(script.Parent.Course).build()
local players = game:GetService("Players")
players.PlayerAdded:Connect(function(player) course:addPlayer(player) end)
for _, player in players:GetPlayers() do course:addPlayer(player) end
players.CharacterAutoLoads = true
game:GetService("RunService").Heartbeat:Connect(function(delta) course:update(delta) end)
print("Spinner Obby: jump the pads and kill strips, save at the checkpoint, then dodge the spinners and cross the beam!")
