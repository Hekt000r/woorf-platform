local Course = {}
Course.__index = Course

local palette = {
    Start = Color3.fromRGB(35, 195, 210),
    JumpPad1 = Color3.fromRGB(75, 140, 255),
    JumpPad2 = Color3.fromRGB(160, 95, 245),
    JumpPad3 = Color3.fromRGB(245, 120, 185),
    HurdleRun = Color3.fromRGB(60, 100, 165),
    Checkpoint = Color3.fromRGB(70, 225, 115),
    SpinnerDeck1 = Color3.fromRGB(70, 140, 215),
    SpinnerDeck2 = Color3.fromRGB(125, 90, 200),
    SpinnerHub1 = Color3.fromRGB(255, 195, 55),
    SpinnerHub2 = Color3.fromRGB(255, 195, 55),
    BalanceBeam = Color3.fromRGB(255, 170, 45),
    Finish = Color3.fromRGB(65, 220, 140),
    FinishPost = Color3.fromRGB(255, 205, 55),
    FinishArch = Color3.fromRGB(255, 205, 55),
}

local function box(parent, name, size, position, isHazard)
    local part = Instance.new("Part")
    part.Name = name
    part.Size = size
    part.Position = position
    part.Anchored = true
    part.CanCollide = not isHazard
    part.CustomPhysicalProperties = PhysicalProperties.new(0.7, 0.5, 0)
    part:SetAttribute("KillPart", isHazard == true)
    part:SetAttribute("RenderColor", isHazard and Color3.fromRGB(255, 55, 65) or palette[name])
    part.Parent = parent
    return part
end

-- All course boxes rotate only about Y; test both boxes' horizontal axes
-- so rotated spinner tips and the character's full collider count as contact.
local function overlaps(a, b)
    local delta = b.Position - a.Position
    if math.abs(delta.Y) > (a.Size.Y + b.Size.Y) / 2 then return false end
    local ar, az = a.CFrame.RightVector, a.CFrame.ZVector
    local br, bz = b.CFrame.RightVector, b.CFrame.ZVector
    for _, axis in {ar, az, br, bz} do
        local extentA = (math.abs(axis:Dot(ar)) * a.Size.X + math.abs(axis:Dot(az)) * a.Size.Z) / 2
        local extentB = (math.abs(axis:Dot(br)) * b.Size.X + math.abs(axis:Dot(bz)) * b.Size.Z) / 2
        if math.abs(delta:Dot(axis)) > extentA + extentB then return false end
    end
    return true
end

local function buildFirstStage(folder, hazards)
    box(folder, "Start", Vector3.new(16, 1, 16), Vector3.new(0, 0, 6))
    for index = 1, 3 do
        box(folder, "JumpPad" .. index, Vector3.new(6, 1, 6), Vector3.new(index % 2 == 1 and -2 or 2, 0, -8 - index * 7))
    end
    box(folder, "HurdleRun", Vector3.new(12, 1, 16), Vector3.new(0, 0, -39))
    for index = 1, 3 do
        table.insert(hazards, box(folder, "KillStrip" .. index, Vector3.new(12, 0.8, 1), Vector3.new(0, 0.9, -32 - index * 4), true))
    end
    return box(folder, "Checkpoint", Vector3.new(14, 1, 8), Vector3.new(0, 0, -52))
end

local function buildSecondStage(folder, hazards)
    local spinners = {}
    for index = 1, 2 do
        local z = -55 - index * 14
        box(folder, "SpinnerDeck" .. index, Vector3.new(14, 1, 12), Vector3.new(0, 0, z))
        local arm = box(folder, "Spinner" .. index, Vector3.new(13, 0.8, 1), Vector3.new(0, 1.3, z), true)
        box(folder, "SpinnerHub" .. index, Vector3.new(1.5, 2, 1.5), Vector3.new(0, 1.5, z))
        table.insert(spinners, arm)
        table.insert(hazards, arm)
    end
    box(folder, "BalanceBeam", Vector3.new(3, 1, 12), Vector3.new(0, 0, -96))
    box(folder, "Finish", Vector3.new(14, 1, 10), Vector3.new(0, 0, -109))
    for _, x in {-5, 5} do
        box(folder, "FinishPost", Vector3.new(1, 7, 1), Vector3.new(x, 4, -111))
    end
    box(folder, "FinishArch", Vector3.new(11, 1, 1), Vector3.new(0, 7.5, -111))
    return spinners
end

function Course.build()
    workspace.Gravity = 80
    workspace:SetAttribute("CharacterSpawn", Vector3.new(0, 3, 8))
    local folder = Instance.new("Folder", workspace)
    folder.Name = "Obby"
    local self = setmetatable({folder = folder, hazards = {}, time = 0}, Course)
    self.checkpoint = buildFirstStage(folder, self.hazards)
    self.spinners = buildSecondStage(folder, self.hazards)
    self.finish = folder.Finish
    self.players = game:GetService("Players")
    self.players.RespawnTime = 1
    return self
end

function Course:addPlayer(player)
    player:SetAttribute("ObbyStage", 1)
    player:SetAttribute("ObbyFinished", false)
    player.CharacterAdded:Connect(function(character)
        if player:GetAttribute("ObbyStage") == 2 then
            character.PrimaryPart.Position = self.checkpoint.Position + Vector3.new(0, 3, 0)
            character.PrimaryPart.AssemblyLinearVelocity = Vector3.zero
        end
    end)
end

function Course:checkPlayer(player)
    local character = player.Character
    if not character then return end
    local root, humanoid = character.PrimaryPart, character:FindFirstChild("Humanoid")
    if not root or not humanoid or humanoid.Health <= 0 then return end
    if root.Position.Y < -12 then humanoid.Health = 0 return end
    for _, hazard in self.hazards do
        if overlaps(root, hazard) then humanoid.Health = 0 return end
    end
    if overlaps(root, self.checkpoint) and player:GetAttribute("ObbyStage") ~= 2 then
        player:SetAttribute("ObbyStage", 2)
        print(player.Name .. " reached the checkpoint!")
    end
    if overlaps(root, self.finish) and not player:GetAttribute("ObbyFinished") then
        player:SetAttribute("ObbyFinished", true)
        print(player.Name .. " finished the obby! Press R to return to the checkpoint.")
    end
end

function Course:update(delta)
    -- Bound each angular sweep to reduce missed contacts on slow viewer frames.
    local steps = math.max(1, math.ceil(delta / (1 / 120)))
    for _ = 1, steps do
        self.time += delta / steps
        for index, arm in self.spinners do
            local speed = index == 1 and 1.5 or -2
            arm.CFrame = CFrame.new(arm.Position) * CFrame.Angles(0, self.time * speed, 0)
        end
        for _, player in self.players:GetPlayers() do self:checkPlayer(player) end
    end
end

return Course
