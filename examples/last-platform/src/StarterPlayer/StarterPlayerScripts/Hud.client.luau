local Ui=require(script.Parent.Ui)
local player=game:GetService("Players").LocalPlayer
local storage=game:GetService("ReplicatedStorage")
local state=storage:WaitForChild("ArenaState")
local action=storage:WaitForChild("ArenaAction")
local screen=Instance.new("ScreenGui",player:WaitForChild("PlayerGui"))
screen.Name="ArenaHud"; screen.ResetOnSpawn=false; screen.IgnoreGuiInset=true

local header=Ui.panel(screen,"Header",UDim2.fromOffset(20,18),UDim2.fromOffset(310,112))
Ui.label(header,"Kicker",UDim2.fromOffset(16,10),UDim2.fromOffset(270,18),"SURVIVE THE SKY",12,Ui.cyan)
local title=Ui.label(header,"Title",UDim2.fromOffset(16,31),UDim2.fromOffset(280,31),"LAST PLATFORM",25)
title.Font=Enum.Font.GothamBold
local round=Ui.label(header,"Round",UDim2.fromOffset(16,71),UDim2.fromOffset(280,25),"ROUND 1 · GET READY",16)
local timerPanel=Ui.panel(screen,"TimerPanel",UDim2.new(.5,-65,0,18),UDim2.fromOffset(130,86))
Ui.label(timerPanel,"Caption",UDim2.fromOffset(14,9),UDim2.fromOffset(102,18),"TIME LEFT",12,Ui.cyan)
local timer=Ui.label(timerPanel,"Time",UDim2.fromOffset(14,28),UDim2.fromOffset(104,45),"06",34)
timer.Font=Enum.Font.GothamBold; timer.TextXAlignment=Enum.TextXAlignment.Center

local message=Ui.label(screen,"Message",UDim2.new(.5,-340,1,-160),UDim2.fromOffset(680,36),"",21,Ui.gold)
message.TextXAlignment=Enum.TextXAlignment.Center
local status=Ui.label(screen,"Status",UDim2.new(.5,-330,1,-126),UDim2.fromOffset(660,22),"",14)
status.TextXAlignment=Enum.TextXAlignment.Center
local shove=Ui.button(screen,"Shove",UDim2.new(.5,-226,1,-90),UDim2.fromOffset(218,60),"[ E ]  SHOVE",Ui.gold)
local boost=Ui.button(screen,"Boost",UDim2.new(.5,8,1,-90),UDim2.fromOffset(218,60),"[ Q ]  SKY JUMP",Ui.cyan)
local bots=Ui.button(screen,"Bots",UDim2.new(0,20,1,-66),UDim2.fromOffset(165,36),"BOTS: ON",Ui.white)
bots.TextSize=14
Ui.label(screen,"BotHint",UDim2.new(0,20,1,-25),UDim2.fromOffset(170,17),"Next round · host only",11)
local controls=Ui.label(screen,"Controls",UDim2.new(.5,-245,1,-23),UDim2.fromOffset(490,18),"WASD move · SPACE jump · RMB look · SCROLL zoom · E shove · Q sky jump",12)
controls.TextXAlignment=Enum.TextXAlignment.Center

local function refresh()
    local phase=state:GetAttribute("Phase") or "Countdown"
    local isAlive=player:GetAttribute("ArenaAlive")==true
    round.Text=string.format("ROUND %d  ·  %s",math.max(1,state:GetAttribute("Round") or 1),phase=="Fight" and ((state:GetAttribute("Alive") or 0).." REMAIN") or string.upper(phase))
    timer.Text=string.format("%02d",state:GetAttribute("TimeLeft") or 0)
    message.Text=state:GetAttribute("Message") or ""
    status.Text=phase=="Fight" and (isAlive and "Stay on blue tiles. Dodge the amber wind-up, then strike back." or "ELIMINATED · Watch the survivors. You return next round.")
        or "Shove rivals off the island. Cracking orange tiles fall after a short warning."
    local shoveWait=player:GetAttribute("ShoveCooldown") or 0
    local boostWait=player:GetAttribute("BoostCooldown") or 0
    shove.Text=shoveWait>0 and string.format("SHOVE  %.1fs",shoveWait) or "[ E ]  SHOVE"
    boost.Text=boostWait>0 and string.format("SKY JUMP  %.1fs",boostWait) or "[ Q ]  SKY JUMP"
    shove.BackgroundColor3=isAlive and shoveWait<=0 and Ui.gold or Color3.fromRGB(110,110,125)
    boost.BackgroundColor3=isAlive and boostWait<=0 and Ui.cyan or Color3.fromRGB(85,110,125)
    bots.Text=state:GetAttribute("BotsEnabled") and "BOTS: ON" or "BOTS: OFF"
    bots.Visible=state:GetAttribute("HostId")==player.UserId
end

shove.Activated:Connect(function() action:FireServer("Shove") end)
boost.Activated:Connect(function() action:FireServer("Boost") end)
bots.Activated:Connect(function() action:FireServer("Bots") end)
game:GetService("UserInputService").InputBegan:Connect(function(input,processed)
    if processed then return end
    if input.KeyCode==Enum.KeyCode.E then action:FireServer("Shove") end
    if input.KeyCode==Enum.KeyCode.Q then action:FireServer("Boost") end
    if input.KeyCode==Enum.KeyCode.B then action:FireServer("Bots") end
end)
for _,name in {"Phase","TimeLeft","Round","Alive","BotsEnabled","Message","HostId"} do
    state:GetAttributeChangedSignal(name):Connect(refresh)
end
for _,name in {"ArenaAlive","ShoveCooldown","BoostCooldown"} do player:GetAttributeChangedSignal(name):Connect(refresh) end
refresh()
