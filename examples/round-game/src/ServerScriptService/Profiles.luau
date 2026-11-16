local Profiles = {}
local store = game:GetService("DataStoreService"):GetDataStore("RoundGameProfiles")
local active = {}

local function apply(entry, data)
    entry.Profile.Coins.Value = data.Coins
    entry.Profile.Rounds.Value = data.Rounds
    entry.Profile.Inventory.Potion.Value = data.Items
end

function Profiles.load(player, team)
    local identity = "profile-" .. player.UserId
    local saved = store:GetAsync(identity) or {Coins = 0, Rounds = 0, Items = 0}
    local profile = Instance.new("Folder", player); profile.Name = "Profile"
    profile:SetAttribute("ProfileKey", identity); profile:AddTag("PlayerProfile")
    for _, name in ipairs({"Coins", "Rounds"}) do
        local value = Instance.new("IntValue", profile); value.Name = name
    end
    local inventory = Instance.new("Folder", profile); inventory.Name = "Inventory"
    local item = Instance.new("IntValue", inventory); item.Name = "Potion"
    item:SetAttribute("ItemId", "potion"); item:AddTag("InventoryItem")
    player.Team = team
    active[player] = {Identity = identity, Profile = profile, LastSave = saved.LastSave}
    apply(active[player], saved)
end

function Profiles.snapshot(player)
    local entry = active[player]
    if not entry then return nil end
    local profile = entry.Profile
    return {ProfileKey = entry.Identity, Coins = profile.Coins.Value, LastSave = entry.LastSave,
        Rounds = profile.Rounds.Value, Items = profile.Inventory.Potion.Value, Profile = profile}
end

function Profiles.reward(player)
    local entry = active[player]
    local data = store:UpdateAsync(entry.Identity, function(old)
        old = old or {Coins = 0, Rounds = 0, Items = 0}
        old.Coins += 10; old.Rounds += 1
        return old
    end)
    apply(entry, data)
end

function Profiles.buy(player)
    local entry = active[player]
    local data = store:UpdateAsync(entry.Identity, function(old)
        if not old or old.Coins < 3 then return nil end
        old.Coins -= 3; old.Items += 1
        return old
    end)
    if not data then return false end
    apply(entry, data)
    return true
end

function Profiles.save(player, reason)
    local entry = active[player]
    if not entry then return end
    store:UpdateAsync(entry.Identity, function(old)
        old = old or {Coins = 0, Rounds = 0, Items = 0}
        old.LastSave = reason
        return old
    end)
    print("SAVED " .. reason .. " " .. entry.Identity)
end

function Profiles.remove(player) Profiles.save(player, "leave"); active[player] = nil end
function Profiles.close() for player in pairs(active) do Profiles.save(player, "shutdown") end end
return Profiles
