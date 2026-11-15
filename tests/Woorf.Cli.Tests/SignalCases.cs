static class SignalCases
{
    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return GetConnectionCase();
        yield return GetPropertyCase();
        yield return GetOnceCase();
        yield return GetDispatchMutationCase();
        yield return GetValidationCase();
        yield return GetErrorCase();
    }

    private static (string, string?, int, string, string) GetConnectionCase() =>
        ("connects and disconnects typed signal objects", """
            local folder = Instance.new("Folder")
            local signal = folder.Changed
            assert(typeof(signal) == "RBXScriptSignal" and signal == folder.Changed)
            assert(signal ~= folder.ChildAdded and signal ~= Instance.new("Folder").Changed)
            local calls = 0
            local connection = signal:Connect(function(property)
                assert(property == "Name" and folder.Name == "Renamed")
                calls += 1
            end)
            assert(typeof(connection) == "RBXScriptConnection" and connection.Connected)
            folder.Name = "Renamed"
            assert(calls == 1)
            connection:Disconnect(); connection:Disconnect()
            assert(not connection.Connected)
            folder.Name = "Again"
            assert(calls == 1)
            assert(not pcall(function() connection.Connected = true end))
            assert(not pcall(function() signal.Connect = function() end end))
            assert(not pcall(function() setmetatable(signal, {}) end))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetPropertyCase() =>
        ("fires property signals only for changed values", """
            local folder = Instance.new("Folder")
            local changed, names, archivable = {}, 0, 0
            local nameSignal = folder:GetPropertyChangedSignal("Name")
            assert(nameSignal == folder:GetPropertyChangedSignal("Name"))
            assert(nameSignal ~= folder.Changed)
            folder.Changed:Connect(function(property) table.insert(changed, property) end)
            nameSignal:Connect(function(...)
                assert(select("#", ...) == 0 and folder.Name == "Changed")
                names += 1
            end)
            folder:GetPropertyChangedSignal("Archivable"):Connect(function(...) archivable += 1; assert(select("#", ...) == 0) end)
            folder.Name = "Folder"; folder.Archivable = true; folder.Parent = nil
            assert(#changed == 0)
            folder.Name = "Changed"; folder.Name = "Changed"
            folder.Archivable = false; folder.Archivable = false
            assert(names == 1 and archivable == 1)
            assert(#changed == 2 and changed[1] == "Name" and changed[2] == "Archivable")
            """, 0, "", "");

    private static (string, string?, int, string, string) GetOnceCase() =>
        ("disconnects Once before reentrant delivery", """
            local folder = Instance.new("Folder")
            local calls = 0
            local connection
            connection = folder.Changed:Once(function(property)
                assert(property == "Name" and not connection.Connected)
                calls += 1
                folder.Archivable = false
            end)
            folder.Name = "Trigger"
            folder.Name = "Another"
            assert(calls == 1 and not connection.Connected)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetDispatchMutationCase() =>
        ("handles subscription changes during dispatch", """
            local folder = Instance.new("Folder")
            local first, later, added = 0, 0, 0
            local laterConnection
            folder.Changed:Connect(function()
                first += 1
                laterConnection:Disconnect()
                folder.Changed:Connect(function() added += 1 end)
            end)
            laterConnection = folder.Changed:Connect(function() later += 1 end)
            folder.Name = "One"
            assert(first == 1 and later == 0 and added == 0)
            folder.Name = "Two"
            assert(first == 2 and later == 0 and added == 1)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetValidationCase() =>
        ("rejects invalid signal arguments and receivers", """
            local folder = Instance.new("Folder")
            local signal = folder.Changed
            assert(typeof(signal) == "RBXScriptSignal")
            assert(not pcall(function() signal:Connect(nil) end))
            assert(not pcall(function() signal:Once({}) end))
            assert(not pcall(function() signal.Connect({}, function() end) end))
            assert(not pcall(function() signal.Unknown() end))
            local connection = signal:Connect(function() end)
            assert(not pcall(function() connection.Disconnect({}) end))
            assert(not pcall(function() return connection.Unknown end))
            assert(not pcall(function() folder:GetPropertyChangedSignal("Unknown") end))
            assert(not pcall(function() folder:GetPropertyChangedSignal("Destroy") end))
            assert(not pcall(function() folder:GetPropertyChangedSignal({}) end))
            local readOnlySignal = folder:GetPropertyChangedSignal("ClassName")
            assert(typeof(readOnlySignal) == "RBXScriptSignal")
            """, 0, "", "");

    private static (string, string?, int, string, string) GetErrorCase() =>
        ("reports callback errors without aborting other listeners", """
            local folder = Instance.new("Folder")
            folder.Changed:Connect(function() error("intentional signal callback failure") end)
            local observed = false
            folder.Changed:Connect(function() observed = true end)
            local succeeded = pcall(function() folder.Name = "Changed" end)
            assert(succeeded and observed and folder.Name == "Changed")
            """, 0, "", "intentional signal callback failure");
}
