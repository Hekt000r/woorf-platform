local added = workspace.ChildAdded:Connect(function(child)
    print("Added:", child.Name)
end)

local folder = Instance.new("Folder")
folder.Name = "Hello"
folder:GetPropertyChangedSignal("Name"):Connect(function()
    print("Renamed:", folder.Name)
end)
folder.Destroying:Once(function()
    print("Destroying:", folder.Name)
end)

folder.Parent = workspace
folder.Name = "Woorf"
folder:Destroy()
added:Disconnect()
