local folder = Instance.new("Folder", workspace)

task.spawn(function()
    local child = folder:WaitForChild("Ready", 1)
    print("Child arrived:", child.Name)
end)

task.defer(function()
    print("Deferred work")
end)

task.delay(0.02, function()
    local child = Instance.new("Folder")
    child.Name = "Ready"
    child.Parent = folder
end)

print("Startup complete")
task.wait(0.03)
print("Timer complete")
