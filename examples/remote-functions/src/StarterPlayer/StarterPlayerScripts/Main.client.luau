local query = game:GetService("ReplicatedStorage"):WaitForChild("Greeting")
local greeting, data = query:InvokeServer("Hello from client")
print(greeting, data.Received)
