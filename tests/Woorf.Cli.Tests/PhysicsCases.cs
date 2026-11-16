static class PhysicsCases
{
    internal static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases() =>
        GetPropertyCases().Concat(GetMotionCases()).Concat(GetCollisionCases()).Concat(GetLifecycleCases()).Concat(GetMaterialCases());

    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetPropertyCases()
    {
        yield return ("physics exposes validated gravity velocity and collision properties", """
            assert(workspace.Gravity == 196.2)
            local part = Instance.new("Part")
            assert(part.CanCollide and part.AssemblyLinearVelocity == Vector3.zero and part.AssemblyAngularVelocity == Vector3.zero)
            part.AssemblyLinearVelocity = Vector3.new(1, 2, 3)
            part.AssemblyAngularVelocity = Vector3.new(0, 0, 1)
            part.CanCollide = false
            local clone = part:Clone()
            assert(clone.AssemblyLinearVelocity == part.AssemblyLinearVelocity and not clone.CanCollide)
            assert(clone.AssemblyAngularVelocity == part.AssemblyAngularVelocity)
            assert(not pcall(function() workspace.Gravity = math.huge end))
            assert(not pcall(function() part.AssemblyLinearVelocity = Vector3.new(0/0, 0, 0) end))
            assert(not pcall(function() part.AssemblyAngularVelocity = Vector3.new(math.huge, 0, 0) end))
            assert(not pcall(function() part.CanCollide = 1 end))
            print("properties")
            """, 0, "properties\n", "");
        yield return ("physics exposes immutable configurable physical properties", """
            local material = PhysicalProperties.new(2, 0.4, 0.8, 2, 3)
            assert(typeof(material) == "PhysicalProperties" and material.Density == 2)
            assert(math.abs(material.Friction - 0.4) < 0.00001 and material.ElasticityWeight == 3)
            assert(not pcall(function() material.Friction = 1 end))
            assert(not pcall(function() PhysicalProperties.new(0/0, 0, 0) end))
            assert(not pcall(function() PhysicalProperties.new(1, 0, 0, 1) end))
            assert(not pcall(function() PhysicalProperties.new(1, 0, 0, 1, 1, 0.5) end))
            local clamped = PhysicalProperties.new(0, 5, -1, -2, 101)
            assert(clamped.Density > 0 and clamped.Friction == 2 and clamped.Elasticity == 0)
            assert(clamped.FrictionWeight == 0 and clamped.ElasticityWeight == 100)
            local part = Instance.new("Part")
            assert(part.CustomPhysicalProperties == nil)
            part.CustomPhysicalProperties = material
            assert(part:Clone().CustomPhysicalProperties == material)
            part.CustomPhysicalProperties = nil
            assert(part.CustomPhysicalProperties == nil)
            print("materials")
            """, 0, "materials\n", "");
    }

    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetMotionCases()
    {
        yield return ("physics drops a box and settles it on an anchored floor", """
            local floor = Instance.new("Part", workspace)
            floor.Anchored = true; floor.Size = Vector3.new(40, 2, 40); floor.Position = Vector3.new(0, -1, 0)
            local box = Instance.new("Part", workspace)
            box.Size = Vector3.new(2, 2, 2); box.Position = Vector3.new(0, 10, 0)
            task.wait(0.15)
            assert(box.Position.Y < 9.5 and box.Position.Y > 3, "box did not fall")
            task.wait(0.85)
            assert(math.abs(box.Position.Y - 1) < 0.08, "box did not settle on floor")
            assert(math.abs(box.AssemblyLinearVelocity.Y) < 0.2)
            assert(floor.Position == Vector3.new(0, -1, 0))
            print("settled")
            """, 0, "settled\n", "");
        yield return ("physics stops a moving box against an anchored wall", """
            workspace.Gravity = 0
            local wall = Instance.new("Part", workspace)
            wall.CustomPhysicalProperties = PhysicalProperties.new(0.7, 0.3, 0)
            wall.Anchored = true; wall.Size = Vector3.new(1, 20, 20); wall.Position = Vector3.new(5, 0, 0)
            local box = Instance.new("Part", workspace); box.Size = Vector3.new(2, 2, 2)
            box.CustomPhysicalProperties = PhysicalProperties.new(0.7, 0.3, 0)
            box.AssemblyLinearVelocity = Vector3.new(10, 0, 0)
            task.wait(0.8)
            assert(box.Position.X > 3.3 and box.Position.X < 3.6, "box crossed wall")
            assert(math.abs(box.AssemblyLinearVelocity.X) < 0.2)
            print("wall")
            """, 0, "wall\n", "");
        yield return ("physics integrates angular velocity into Part orientation", """
            workspace.Gravity = 0
            local box = Instance.new("Part", workspace)
            box.AssemblyAngularVelocity = Vector3.new(0, 0, math.pi / 2)
            task.wait(1)
            assert(math.abs(box.CFrame.RightVector.X) < 0.08 and box.CFrame.RightVector.Y > 0.99)
            assert(math.abs(box.AssemblyAngularVelocity.Z - math.pi/2) < 0.01)
            print("rotated")
            """, 0, "rotated\n", "");
    }

    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCollisionCases()
    {
        yield return ("physics resolves collisions between two dynamic boxes", """
            workspace.Gravity = 0
            local left = Instance.new("Part", workspace)
            left.CustomPhysicalProperties = PhysicalProperties.new(0.7, 0.3, 0)
            left.Size = Vector3.new(2, 2, 2)
            left.Position = Vector3.new(-3, 0, 0); left.AssemblyLinearVelocity = Vector3.new(5, 0, 0)
            local right = left:Clone(); right.Position = Vector3.new(3, 0, 0)
            right.AssemblyLinearVelocity = Vector3.new(-5, 0, 0); right.Parent = workspace
            task.wait(0.8)
            assert(left.Position.X < right.Position.X and right.Position.X-left.Position.X > 1.9)
            assert(math.abs(left.AssemblyLinearVelocity.X) < 0.2 and math.abs(right.AssemblyLinearVelocity.X) < 0.2)
            print("dynamic")
            """, 0, "dynamic\n", "");
        yield return ("physics slides a box down an oriented ramp", """
            local material = PhysicalProperties.new(1, 0, 0)
            local ramp = Instance.new("Part", workspace); ramp.Anchored = true
            ramp.Size = Vector3.new(30, 1, 15); ramp.CFrame = CFrame.Angles(0, 0, math.rad(20))
            ramp.CustomPhysicalProperties = material
            local box = Instance.new("Part", workspace); box.Size = Vector3.new(2, 2, 2)
            box.CFrame = ramp.CFrame * CFrame.new(0, 1.5, 0); box.CustomPhysicalProperties = material
            task.wait(0.3)
            local localPoint = ramp.CFrame:PointToObjectSpace(box.Position)
            assert(localPoint.X < -1 and localPoint.Y > 1.3, "box did not slide along oriented ramp")
            print("ramp")
            """, 0, "ramp\n", "");
        yield return ("physics applies configurable restitution to a wall impact", """
            workspace.Gravity = 0
            local material = PhysicalProperties.new(1, 0, 1)
            local wall = Instance.new("Part", workspace); wall.Anchored = true
            wall.Size = Vector3.new(1, 20, 20); wall.Position = Vector3.new(5, 0, 0)
            wall.CustomPhysicalProperties = material
            local box = Instance.new("Part", workspace); box.Size = Vector3.new(2, 2, 2)
            box.CustomPhysicalProperties = material; box.AssemblyLinearVelocity = Vector3.new(10, 0, 0)
            task.wait(0.6)
            assert(box.AssemblyLinearVelocity.X < -9.5 and box.Position.X < 2)
            print("bounce")
            """, 0, "bounce\n", "");
    }

    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetLifecycleCases()
    {
        yield return ("physics honors CanCollide and runtime anchoring changes", """
            local floor = Instance.new("Part", workspace)
            floor.Anchored = true; floor.Size = Vector3.new(40, 2, 40); floor.CanCollide = false
            local box = Instance.new("Part", workspace); box.Position = Vector3.new(0, 8, 0)
            box.Anchored = true
            task.wait(0.05); assert(box.Position.Y == 8)
            box.Anchored = false
            task.wait(0.4); assert(box.Position.Y < -2, "non-colliding floor blocked fall")
            box.Anchored = true
            local frozen = box.Position
            task.wait(0.1); assert(box.Position == frozen)
            box:Destroy(); task.wait(0.02)
            print("lifecycle")
            """, 0, "lifecycle\n", "");
        yield return ("physics preserves callback edits to another moving Part", """
            workspace.Gravity = 10
            local a = Instance.new("Part", workspace); a.CanCollide = false
            local b = Instance.new("Part", workspace); b.CanCollide = false; b.Position = Vector3.new(10, 0, 0)
            local changed = false
            a:GetPropertyChangedSignal("CFrame"):Connect(function()
                if not changed then changed = true; b.Position = Vector3.new(100, 50, 0) end
            end)
            task.wait(0.03)
            assert(changed and b.Position.X == 100, "physics overwrote a callback teleport")
            print("callback")
            """, 0, "callback\n", "");
    }

    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetMaterialCases()
    {
        yield return ("physics friction slows sliding on a floor", """
            local floor = Instance.new("Part", workspace); floor.Anchored = true
            floor.Size = Vector3.new(100, 2, 40); floor.Position = Vector3.new(0, -1, 0)
            floor.CustomPhysicalProperties = PhysicalProperties.new(1, 1, 0)
            local box = Instance.new("Part", workspace); box.Size = Vector3.new(2, 2, 2)
            box.Position = Vector3.new(0, 1, 0); box.CustomPhysicalProperties = PhysicalProperties.new(1, 1, 0)
            box.AssemblyLinearVelocity = Vector3.new(5, 0, 0)
            task.wait(0.4)
            assert(math.abs(box.AssemblyLinearVelocity.X) < 0.2 and box.Position.Y > 0.9)
            print("friction")
            """, 0, "friction\n", "");
        yield return ("physics density controls momentum transfer between boxes", """
            workspace.Gravity = 0
            local a = Instance.new("Part", workspace); a.Size = Vector3.new(2, 2, 2)
            a.Position = Vector3.new(-3, 0, 0); a.CustomPhysicalProperties = PhysicalProperties.new(1, 0, 0)
            a.AssemblyLinearVelocity = Vector3.new(6, 0, 0)
            local b = Instance.new("Part", workspace); b.Size = a.Size; b.Position = Vector3.new(3, 0, 0)
            b.CustomPhysicalProperties = PhysicalProperties.new(3, 0, 0)
            task.wait(0.85)
            assert(math.abs(a.AssemblyLinearVelocity.X - 1.5) < 0.03)
            assert(math.abs(b.AssemblyLinearVelocity.X - 1.5) < 0.03)
            print("mass")
            """, 0, "mass\n", "");
        yield return ("physics supports reversing the gravity direction", """
            workspace.Gravity = -10
            local part = Instance.new("Part", workspace)
            task.wait(0.2)
            assert(part.Position.Y > 0.15 and part.AssemblyLinearVelocity.Y > 1.5)
            print("upward")
            """, 0, "upward\n", "");
    }

}
