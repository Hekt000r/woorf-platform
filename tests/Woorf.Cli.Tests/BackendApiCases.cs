static class BackendApiCases
{
    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        foreach (var test in GetCoreCases()) yield return test;
        foreach (var test in GetValueCases()) yield return test;
        foreach (var test in BackendDatatypeCases.GetCases()) yield return test;
        foreach (var test in BackendServiceCases.GetCases()) yield return test;
    }

    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCoreCases()
    {
        yield return ("restricts RemoteFunction public members to its own API", """
            local remote = Instance.new("RemoteFunction")
            assert(remote:IsA("Instance") and not remote:IsA("RemoteEvent"))
            for _, member in {"FireServer", "FireClient", "FireAllClients", "OnServerEvent", "OnClientEvent"} do
                assert(not pcall(function() return remote[member] end), member)
            end
            print("ok")
            """, 0, "ok\n", "");
        yield return ("warn writes its arguments to stderr", "warn('backend warning', 7)", 0, "", "backend warning\t7");
        yield return ("stores attributes with change notifications independent clone state and validation", """
            local folder = Instance.new("Folder")
            local changed = 0
            folder:GetAttributeChangedSignal("Coins"):Connect(function() changed += 1 end)
            folder:SetAttribute("Coins", 10); folder:SetAttribute("Coins", 10)
            assert(changed == 1 and folder:GetAttribute("Coins") == 10)
            local attributes = folder:GetAttributes(); attributes.Coins = 99
            assert(folder:GetAttribute("Coins") == 10)
            local clone = folder:Clone(); clone:SetAttribute("Coins", 20)
            assert(folder:GetAttribute("Coins") == 10 and clone:GetAttribute("Coins") == 20)
            assert(not pcall(function() folder:SetAttribute("Invalid Name", 1) end))
            assert(not pcall(function() folder:SetAttribute("Table", {}) end))
            assert(not pcall(function() folder:SetAttribute("Callback", function() end) end))
            folder:SetAttribute("Coins", nil)
            assert(changed == 2 and folder:GetAttribute("Coins") == nil)
            print("ok")
            """, 0, "ok\n", "");
        yield return ("tracks collection membership through tags clone and destruction", """
            local tags = game:GetService("CollectionService")
            local added, removed = 0, 0
            tags:GetInstanceAddedSignal("Active"):Connect(function()
                added += 1
                tags:GetInstanceAddedSignal("Nested"):Connect(function() end)
            end)
            tags:GetInstanceRemovedSignal("Active"):Connect(function() removed += 1 end)
            local folder = Instance.new("Folder", workspace)
            tags:AddTag(folder, "Active"); tags:AddTag(folder, "Active")
            assert(added == 1 and folder:HasTag("Active") and tags:HasTag(folder, "Active"))
            assert(#tags:GetTagged("Active") == 1 and #folder:GetTags() == 1 and #tags:GetAllTags() == 1 and tags:GetAllTags()[1] == "Active")
            local clone = folder:Clone(); assert(clone:HasTag("Active"))
            tags:RemoveTag(folder, "Active"); assert(removed == 1)
            tags:AddTag(folder, "Active"); folder:Destroy()
            assert(removed == 2 and #tags:GetTagged("Active") == 0 and #tags:GetAllTags() == 0)
            print("ok")
            """, 0, "ok\n", "");
    }

    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetValueCases()
    {
        yield return ("ValueObjects publish values and preserve independent clone state", """
            local coins = Instance.new("IntValue")
            assert(coins:IsA("ValueBase") and coins.Value == 0)
            local received
            coins.Changed:Connect(function(value) received = value end)
            coins.Value = 42
            assert(received == 42 and coins.Value == 42)
            local clone = coins:Clone(); clone.Value = 5
            assert(coins.Value == 42 and clone.Value == 5)
            assert(not pcall(function() coins.Value = "bad" end))
            local flag = Instance.new("BoolValue"); flag.Value = true; assert(flag.Value)
            local text = Instance.new("StringValue"); text.Value = "text"; assert(text.Value == "text")
            local number = Instance.new("NumberValue"); number.Value = 1.5; assert(number.Value == 1.5)
            local object = Instance.new("ObjectValue"); object.Value = coins; assert(object.Value == coins)
            print("ok")
            """, 0, "ok\n", "");
        yield return ("BindableEvent preserves complete tuples and independent cloned signals", """
            local event = Instance.new("BindableEvent")
            local count = 0
            event.Event:Connect(function(...)
                local args = table.pack(...)
                assert(args.n == 3 and args[1] == "hello" and args[2] == 2 and args[3] == nil)
                count += 1
            end)
            event:Fire("hello", 2, nil)
            event:Clone():Fire("hello", 2, nil)
            assert(count == 1)
            print("ok")
            """, 0, "ok\n", "");
    }
}
