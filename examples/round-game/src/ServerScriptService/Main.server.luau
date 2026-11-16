local Profiles = require(script.Parent.Profiles)
local Round = require(script.Parent.Round)
local players = game:GetService("Players")
local teams = game:GetService("Teams")
local red = Instance.new("Team", teams); red.Name = "Red"
local blue = Instance.new("Team", teams); blue.Name = "Blue"
local query = Instance.new("RemoteFunction", game:GetService("ReplicatedStorage"))
query.Name = "GameRequest"

players.PlayerAdded:Connect(function(player)
    Profiles.load(player, #red:GetPlayers() <= #blue:GetPlayers() and red or blue)
end)
players.PlayerRemoving:Connect(function(player)
    Round.remove(player)
    Profiles.remove(player)
end)
query.OnServerInvoke = function(player, action, item, ...)
    if select("#", ...) ~= 0 or not Profiles.snapshot(player) then return false end
    if action == "ready" and item == nil then return Round.ready(player) end
    if action == "buy" then return Round.buy(player, item, Profiles) end
    if action ~= "status" or item ~= nil then return false end
    local data = Profiles.snapshot(player)
    data.Phase = Round.phase()
    return data
end
Round.start(Profiles)
game:BindToClose(function()
    Round.close()
    task.wait(0.01)
    Profiles.close()
end)
