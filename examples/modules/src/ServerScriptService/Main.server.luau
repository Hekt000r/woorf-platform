local configModule = game:GetService("ReplicatedStorage").Config
local config = require(configModule)
assert(config == require(configModule))
print(config.GameName)
