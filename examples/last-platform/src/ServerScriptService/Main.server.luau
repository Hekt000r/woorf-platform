local match=require(script.Parent.Match).new()
local players=game:GetService("Players")
players.PlayerAdded:Connect(function(player) match:addPlayer(player) end)
players.PlayerRemoving:Connect(function(player)
    local actor=match.roster.byPlayer[player]
    if actor then actor.alive=false end
    match.roster.byPlayer[player]=nil
end)
for _,player in players:GetPlayers() do match:addPlayer(player) end
local action=Instance.new("RemoteEvent",game:GetService("ReplicatedStorage")); action.Name="ArenaAction"
action.OnServerEvent:Connect(function(player,...) match:request(player,...) end)
game:GetService("RunService").Heartbeat:Connect(function(delta) match:update(delta) end)
Instance.new("Sky",game:GetService("Lighting"))
print("LAST PLATFORM STANDING · E shove · Q charged jump · Space jump · Last survivor wins!")
