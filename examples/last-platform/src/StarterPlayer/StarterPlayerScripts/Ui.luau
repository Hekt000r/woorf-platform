local Ui={}
Ui.ink=Color3.fromRGB(15,22,44)
Ui.white=Color3.fromRGB(235,245,255)
Ui.cyan=Color3.fromRGB(65,230,220)
Ui.gold=Color3.fromRGB(255,205,80)

function Ui.round(item,radius)
    local corner=Instance.new("UICorner",item); corner.CornerRadius=UDim.new(0,radius or 10)
end

function Ui.panel(parent,name,position,size)
    local frame=Instance.new("Frame",parent); frame.Name=name; frame.Position=position; frame.Size=size
    frame.BackgroundColor3=Ui.ink; frame.BackgroundTransparency=.08; frame.BorderSizePixel=0
    Ui.round(frame)
    return frame
end

function Ui.label(parent,name,position,size,text,textSize,color)
    local label=Instance.new("TextLabel",parent); label.Name=name; label.Position=position; label.Size=size
    label.BackgroundTransparency=1; label.BorderSizePixel=0
    label.Text=text; label.TextSize=textSize or 16; label.TextColor3=color or Ui.white
    label.TextXAlignment=Enum.TextXAlignment.Left; label.Font=Enum.Font.Gotham
    return label
end

function Ui.button(parent,name,position,size,text,color)
    local button=Instance.new("TextButton",parent); button.Name=name; button.Position=position; button.Size=size
    button.Text=text; button.TextSize=18; button.Font=Enum.Font.GothamBold
    button.TextColor3=Ui.ink; button.BackgroundColor3=color or Ui.cyan; button.BorderSizePixel=0
    Ui.round(button)
    return button
end

return Ui
