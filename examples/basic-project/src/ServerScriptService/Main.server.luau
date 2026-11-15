print("hello world")
print(game.Name)
print(script:GetFullName())
print(workspace.Scene.ClassName)

local folder = Instance.new("Folder")
folder.Name = "RuntimeObjects"
folder.Parent = workspace
assert(workspace.RuntimeObjects == folder)
