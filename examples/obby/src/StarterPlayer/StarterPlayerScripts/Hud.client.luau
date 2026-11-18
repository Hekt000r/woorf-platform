local player = game:GetService("Players").LocalPlayer
local screen = Instance.new("ScreenGui")
screen.Name = "ObbyHud"
screen.ResetOnSpawn = false
screen.IgnoreGuiInset = true
screen.Parent = player:WaitForChild("PlayerGui")

local panel = Instance.new("Frame", screen)
panel.Name = "Panel"
panel.Position = UDim2.fromOffset(18, 18)
panel.Size = UDim2.new(0, 300, 0, 178)
panel.BackgroundColor3 = Color3.fromRGB(20, 28, 43)
panel.BackgroundTransparency = 0.12
panel.BorderSizePixel = 0
local corner = Instance.new("UICorner", panel)
corner.CornerRadius = UDim.new(0, 10)

local function label(name, y, height, size)
    local text = Instance.new("TextLabel", panel)
    text.Name = name
    text.Position = UDim2.fromOffset(14, y)
    text.Size = UDim2.new(1, -28, 0, height)
    text.BackgroundTransparency = 1
    text.TextColor3 = Color3.fromRGB(235, 243, 255)
    text.TextSize = size
    text.TextXAlignment = Enum.TextXAlignment.Left
    return text
end

local title = label("Title", 10, 27, 22)
title.Text = "SPINNER OBBY"
local status = label("Status", 43, 24, 17)
local stats = label("Stats", 70, 23, 16)
local hint = label("Hint", 97, 20, 12)
hint.Text = "WASD move  |  Space jump  |  R reset"
local restart = Instance.new("TextButton", panel)
restart.Name = "Restart"
restart.Position = UDim2.fromOffset(14, 130)
restart.Size = UDim2.new(1, -28, 0, 34)
restart.BackgroundColor3 = Color3.fromRGB(47, 184, 162)
restart.BorderSizePixel = 0
restart.TextColor3 = Color3.fromRGB(10, 27, 35)
restart.TextSize = 17
restart.Text = "Restart course"
Instance.new("UICorner", restart).CornerRadius = UDim.new(0, 6)

local function refresh()
    local isFinished = player:GetAttribute("ObbyFinished") == true
    status.Text = isFinished and "FINISHED! Well done." or
        (player:GetAttribute("ObbyStage") == 2 and "Stage 2 / 2 - checkpoint saved" or "Stage 1 / 2 - reach the green pad")
    status.TextColor3 = isFinished and Color3.fromRGB(95, 240, 165) or Color3.fromRGB(235, 243, 255)
    local elapsed = player:GetAttribute("ObbyElapsed") or 0
    stats.Text = string.format("Time %02d:%02d  |  Deaths %d", math.floor(elapsed / 60), math.floor(elapsed % 60),
        player:GetAttribute("ObbyDeaths") or 0)
end

for _, name in {"ObbyFinished", "ObbyStage", "ObbyElapsed", "ObbyDeaths"} do
    player:GetAttributeChangedSignal(name):Connect(refresh)
end
refresh()
local remote = game:GetService("ReplicatedStorage"):WaitForChild("ObbyRestart")
restart.Activated:Connect(function() remote:FireServer() end)
