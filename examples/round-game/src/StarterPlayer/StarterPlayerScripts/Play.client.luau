local player = game:GetService("Players").LocalPlayer
local query = game:GetService("ReplicatedStorage"):WaitForChild("GameRequest")
local initial = query:InvokeServer("status")
local profile = initial.Profile
if initial.Rounds > 0 then
    assert(initial.LastSave == (initial.ProfileKey == "profile-1" and "leave" or "shutdown"))
end
assert(profile == player.Profile and profile:HasTag("PlayerProfile"))
assert(player.Team ~= nil and not player.Neutral)
assert(profile:GetAttribute("ProfileKey") == initial.ProfileKey)
assert(query:InvokeServer("buy", "potion") == false)
assert(query:InvokeServer("award", 1000000) == false)
assert(query:InvokeServer("ready", {ProfileKey = "other"}) == false)
assert(query:InvokeServer("status", nil, "forged") == false)
print("JOINED " .. initial.ProfileKey)
assert(query:InvokeServer("ready") == true)
assert(query:InvokeServer("ready") == false)
local completed
repeat
    task.wait(0.02)
    completed = query:InvokeServer("status")
until completed.Phase == "complete"
assert(completed.Coins == initial.Coins + 10 and completed.Rounds == initial.Rounds + 1)
assert(query:InvokeServer("buy", "unknown") == false)
assert(query:InvokeServer("buy", {Item = "potion", Price = -100}) == false)
assert(query:InvokeServer("buy", "potion") == true)
assert(query:InvokeServer("buy", "potion") == false)
local final = query:InvokeServer("status")
assert(final.Coins == initial.Coins + 7 and final.Items == initial.Items + 1)
assert(profile.Coins.Value == final.Coins and profile.Rounds.Value == final.Rounds)
assert(profile.Inventory.Potion.Value == final.Items)
assert(profile.Inventory.Potion:GetAttribute("ItemId") == "potion")
assert(profile.Inventory.Potion:HasTag("InventoryItem"))
assert(#game:GetService("CollectionService"):GetTagged("InventoryItem") >= 1)
print("COMPLETE " .. final.ProfileKey .. " " .. final.Coins .. " " .. final.Rounds .. " " .. final.Items)
if initial.ProfileKey == "profile-1" then player:Kick("Demo complete") end
