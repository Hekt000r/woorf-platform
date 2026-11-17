using TouchCase = (string Name, string? Source, int Exit, string Output, string Error);

static class PhysicsTouchCases
{
    internal static IEnumerable<TouchCase> GetCases()
    {
        yield return CreateVelocityCase();
        yield return CreateSphereCase();
        yield return CreateSpinnerCase();
        yield return CreateTransitionCase();
        yield return CreateCanTouchCase();
        yield return CreateFilterCase();
        yield return CreateTeleportCase();
        yield return CreateCallbackCase();
        yield return CreatePlayerLookupCase();
        yield return CreateRemovalCase();
        yield return CreatePublicationTeleportCase();
    }

    private static TouchCase CreateVelocityCase() => ("touch changing velocity inside a sensor preserves the eventual exit event", """
            workspace.Gravity = 0
            local sensor = Instance.new("Part", workspace)
            sensor.Anchored = true; sensor.CanCollide = false; sensor.Size = Vector3.new(4, 4, 4)
            local mover = Instance.new("Part", workspace)
            mover.Size = Vector3.one; mover.Position = Vector3.new(-4, 0, 0)
            local begins, ends = 0, 0
            sensor.Touched:Connect(function() begins += 1 end)
            sensor.TouchEnded:Connect(function() ends += 1 end)
            mover.AssemblyLinearVelocity = Vector3.new(8, 0, 0)
            task.wait(0.3)
            mover.AssemblyLinearVelocity = Vector3.zero
            task.wait(0.05)
            mover.AssemblyLinearVelocity = Vector3.new(-8, 0, 0)
            task.wait(0.3)
            assert(begins == 1 and ends == 1, "Velocity edits must not clear touch history")
            """, 0, "", "");

    private static TouchCase CreatePublicationTeleportCase() => ("touch publication skips contacts invalidated by transform callbacks", """
            workspace.Gravity = 0
            local sensor = Instance.new("Part", workspace)
            sensor.Anchored = true; sensor.CanCollide = false; sensor.Size = Vector3.new(2, 4, 4)
            local mover = Instance.new("Part", workspace)
            mover.Size = Vector3.one; mover.Position = Vector3.new(-4, 0, 0)
            local begins = 0
            sensor.Touched:Connect(function() begins += 1 end)
            mover:GetPropertyChangedSignal("Position"):Connect(function()
                if mover.Position.X > -1.5 and mover.Position.X < 10 then mover.Position = Vector3.new(20, 0, 0) end
            end)
            mover.AssemblyLinearVelocity = Vector3.new(8, 0, 0)
            task.wait(0.5)
            assert(begins == 0, "A transform callback teleport left a stale touch queued")
            """, 0, "", "");

    private static TouchCase CreateRemovalCase() => ("touch removed subtrees release cached touching parts without synthetic exit", """
            workspace.Gravity = 0
            local holder = Instance.new("Folder", workspace)
            local sensor = Instance.new("Part", holder)
            sensor.Anchored = true; sensor.CanCollide = false; sensor.Size = Vector3.new(4, 4, 4)
            local mover = Instance.new("Part", workspace)
            mover.Size = Vector3.one; mover.Position = Vector3.new(-4, 0, 0)
            local ends = 0
            sensor.Touched:Connect(function() end)
            sensor.TouchEnded:Connect(function() ends += 1 end)
            mover.AssemblyLinearVelocity = Vector3.new(8, 0, 0)
            task.wait(0.3)
            assert(#sensor:GetTouchingParts() == 1)
            holder.Parent = game:GetService("ServerStorage")
            task.wait(0.02)
            assert(#sensor:GetTouchingParts() == 0, "Removed physics geometry retained cached contacts")
            assert(ends == 0, "Hierarchy edits must not synthesize physical exits")
            """, 0, "", "");

    private static TouchCase CreateSphereCase() => ("touch spherical sensors reject bounding box corner false positives", """
            workspace.Gravity = 0
            local sensor = Instance.new("Part", workspace)
            sensor.Shape = Enum.PartType.Ball; sensor.Size = Vector3.new(2, 2, 2)
            sensor.Anchored = true; sensor.CanCollide = false
            local mover = Instance.new("Part", workspace)
            mover.Shape = Enum.PartType.Ball; mover.Size = Vector3.new(2, 2, 2)
            mover.Position = Vector3.new(-3, 1.8, 1.8)
            local begins = 0
            sensor.Touched:Connect(function() begins += 1 end)
            mover.AssemblyLinearVelocity = Vector3.new(2, 0, 0)
            task.wait(0.8)
            assert(begins == 0, "Overlapping sphere bounds must not count as physical contact")
            mover.Position = Vector3.new(-3, 0, 0); mover.AssemblyLinearVelocity = Vector3.new(6, 0, 0)
            task.wait(0.3)
            assert(begins == 1, "A direct spherical entry must touch")
            """, 0, "", "");

    private static TouchCase CreateSpinnerCase() => ("touch rotating anchored spinner detects a physically moving character collider", """
            workspace.Gravity = 0
            local spinner = Instance.new("Part", workspace)
            spinner.Anchored = true; spinner.CanCollide = false; spinner.Size = Vector3.new(13, 0.8, 1)
            local root = Instance.new("Part", workspace)
            root.Size = Vector3.new(2, 4, 2); root.Position = Vector3.new(0, 5, 0)
            root.AssemblyLinearVelocity = Vector3.new(0, -6, 0)
            local begins = 0
            spinner.Touched:Connect(function(other) assert(other == root); begins += 1 end)
            for index = 1, 30 do
                spinner.CFrame = CFrame.Angles(0, index * 0.04, 0)
                task.wait(1 / 60)
            end
            assert(begins == 1, "Animated spinner must detect the dynamic character exactly once")
            """, 0, "", "");

    private static TouchCase CreateTransitionCase() => ("touch signals fire once on physical entry and exit for noncolliding parts", """
            workspace.Gravity = 0
            local sensor = Instance.new("Part", workspace)
            sensor.Anchored = true; sensor.CanCollide = false; sensor.Size = Vector3.new(2, 4, 4)
            local mover = Instance.new("Part", workspace)
            mover.Size = Vector3.one; mover.Position = Vector3.new(-4, 0, 0)
            local begins, ends, reverse = 0, 0, 0
            sensor.Touched:Connect(function(other) assert(other == mover); begins += 1 end)
            sensor.TouchEnded:Connect(function(other) assert(other == mover); ends += 1 end)
            mover.Touched:Connect(function(other) assert(other == sensor); reverse += 1 end)
            mover.AssemblyLinearVelocity = Vector3.new(8, 0, 0)
            task.wait(0.5)
            assert(begins == 1 and reverse == 1 and ends == 0, "Entry must fire once on both parts")
            assert(sensor:GetTouchingParts()[1] == mover, "Touch interest must expose noncolliding contacts")
            task.wait(0.5)
            assert(begins == 1 and ends == 1 and #sensor:GetTouchingParts() == 0, "Exit must fire exactly once")
            """, 0, "", "");

    private static TouchCase CreateCanTouchCase() => ("touch CanTouch disconnects handlers rejects connection and clones independently", """
            local part = Instance.new("Part")
            assert(part.CanTouch)
            local connection = part.Touched:Connect(function() error("disabled connection ran") end)
            local ended = part.TouchEnded:Connect(function() end)
            part.CanTouch = false
            assert(not connection.Connected and not ended.Connected, "Disabling touch must disconnect existing subscriptions")
            assert(not pcall(function() part.Touched:Connect(function() end) end), "Disabled part must reject touch subscriptions")
            assert(not pcall(function() part.CanTouch = 1 end))
            assert(not part:Clone().CanTouch)
            part.CanTouch = true
            local fresh = part.Touched:Connect(function() end)
            assert(fresh.Connected and not connection.Connected)
            part:Destroy()
            assert(not fresh.Connected, "Destroying a part must release touch subscriptions")
            """, 0, "", "");

    private static TouchCase CreateFilterCase() => ("touch filters both CanTouch and anchored pairs without affecting collisions", """
            workspace.Gravity = 0
            local sensor = Instance.new("Part", workspace)
            sensor.Anchored = true; sensor.Size = Vector3.new(2, 8, 8)
            local mover = Instance.new("Part", workspace)
            mover.Size = Vector3.one; mover.Position = Vector3.new(-4, 0, 0); mover.CanTouch = false
            local begins = 0
            sensor.Touched:Connect(function() begins += 1 end)
            mover.AssemblyLinearVelocity = Vector3.new(8, 0, 0)
            task.wait(1)
            assert(begins == 0 and mover.Position.X < -1, "CanTouch must suppress touch but preserve solid collision")
            mover.Anchored = true; mover.CanTouch = true; mover.Position = Vector3.zero
            task.wait(0.03)
            assert(begins == 0, "Two anchored parts must not touch")
            """, 0, "", "");

    private static TouchCase CreateTeleportCase() => ("touch CFrame edits alone do not synthesize touch transitions", """
            workspace.Gravity = 0
            local sensor = Instance.new("Part", workspace)
            sensor.Anchored = true; sensor.CanCollide = false
            local mover = Instance.new("Part", workspace)
            mover.CanCollide = false; mover.Size = Vector3.one; mover.Position = Vector3.new(20, 0, 0)
            local begins, ends = 0, 0
            sensor.Touched:Connect(function() begins += 1 end)
            sensor.TouchEnded:Connect(function() ends += 1 end)
            task.wait(0.02)
            mover.CFrame = CFrame.new(0, 0, 0)
            task.wait(0.02)
            mover.CFrame = CFrame.new(20, 0, 0)
            task.wait(0.02)
            assert(begins == 0 and ends == 0, "Teleports must not cause touch events")
            """, 0, "", "");

    private static TouchCase CreateCallbackCase() => ("touch destroying another part inside callback cancels stale reverse delivery", """
            workspace.Gravity = 0
            local sensor = Instance.new("Part", workspace)
            sensor.Anchored = true; sensor.CanCollide = false; sensor.Size = Vector3.new(2, 4, 4)
            local mover = Instance.new("Part", workspace)
            mover.CanCollide = false; mover.Size = Vector3.one; mover.Position = Vector3.new(-4, 0, 0)
            local begins, reverse = 0, 0
            sensor.Touched:Connect(function(other) begins += 1; other:Destroy() end)
            mover.Touched:Connect(function() reverse += 1 end)
            mover.AssemblyLinearVelocity = Vector3.new(8, 0, 0)
            task.wait(0.5)
            assert(begins == 1 and reverse == 0 and #sensor:GetTouchingParts() == 0)
            """, 0, "", "");

    private static TouchCase CreatePlayerLookupCase() => ("touch Players GetPlayerFromCharacter resolves current characters only", """
            local players = game:GetService("Players")
            local player = Instance.new("Player", players)
            player:LoadCharacter()
            local old = player.Character
            assert(players:GetPlayerFromCharacter(old) == player)
            player:LoadCharacter()
            assert(players:GetPlayerFromCharacter(old) == nil and players:GetPlayerFromCharacter(player.Character) == player)
            assert(players:GetPlayerFromCharacter(Instance.new("Model")) == nil)
            """, 0, "", "");
}

