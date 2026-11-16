local available, blocked = 0, 0
local function probe(name, callback)
    local canRun, message = pcall(callback)
    if canRun then
        available += 1
        print("AVAILABLE", name)
    else
        blocked += 1
        print("BLOCKED", name, tostring(message))
    end
end

probe("hierarchy", function()
    local folder = Instance.new("Folder", workspace)
    local child = Instance.new("Folder", folder)
    child.Name = "Child"
    assert(folder:FindFirstChild("Child") == child)
    folder:Destroy()
end)

probe("spatial_state", function()
    local part = Instance.new("Part", workspace)
    part.Position = Vector3.new(1, 2, 3)
    assert(part.CFrame.Position == part.Position)
    part:Destroy()
end)

probe("task_spawn", function()
    local completed = false
    task.spawn(function() completed = true end)
    assert(completed)
end)

probe("warn_global", function()
    assert(type(warn) == "function", "warn global is unavailable")
end)

probe("attributes", function()
    local folder = Instance.new("Folder")
    folder:SetAttribute("Coins", 10)
    assert(folder:GetAttribute("Coins") == 10)
end)

probe("collection_tags", function()
    local service = game:GetService("CollectionService")
    local folder = Instance.new("Folder")
    service:AddTag(folder, "Interactable")
    assert(service:HasTag(folder, "Interactable"))
end)

probe("value_objects", function()
    local coins = Instance.new("IntValue")
    coins.Value = 10
    assert(coins.Value == 10)
end)

probe("bindable_events", function()
    local event = Instance.new("BindableEvent")
    local received
    event.Event:Connect(function(value) received = value end)
    event:Fire("ready")
    assert(received == "ready")
end)

probe("random", function()
    local generator = Random.new(123)
    local value = generator:NextInteger(1, 10)
    assert(value >= 1 and value <= 10)
end)

probe("json", function()
    local service = game:GetService("HttpService")
    assert(service:JSONDecode(service:JSONEncode({Coins = 10})).Coins == 10)
end)

probe("debris_service_entry", function()
    game:GetService("Debris"):AddItem(Instance.new("Folder"), 0)
end)

probe("heartbeat", function()
    local connection = game:GetService("RunService").Heartbeat:Connect(function() end)
    connection:Disconnect()
end)

probe("persistence_service_entry", function()
    game:GetService("DataStoreService"):GetDataStore("Profiles")
end)

probe("shutdown_hook_registration", function()
    game:BindToClose(function() end)
end)

probe("teams", function()
    local team = Instance.new("Team", game:GetService("Teams"))
    team.Name = "Blue"
end)

probe("humanoid", function()
    local humanoid = Instance.new("Humanoid")
    assert(humanoid.Health > 0)
end)

probe("raycast", function()
    workspace:Raycast(Vector3.zero, Vector3.new(0, -10, 0))
end)

probe("color3", function()
    local color = Color3.new(1, 0, 0)
    assert(color.R == 1)
end)

probe("remote_function_has_no_event_methods", function()
    local remote = Instance.new("RemoteFunction")
    assert(not pcall(function() return remote.FireServer end), "RemoteFunction exposes RemoteEvent methods")
end)

print("SUMMARY", available, blocked)
