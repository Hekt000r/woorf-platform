static class PhysicsShapeCases
{
    internal static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases() => GetQueryCases().Concat(GetSphereCases()).Concat(GetSphereContactCases());
    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetQueryCases()
    {
        yield return ("physics spherecast catches thin walls faces edges and corners", """
            local wall = Instance.new("Part", workspace); wall.Anchored = true
            wall.Size = Vector3.new(0.05,2,2)
            local hit = workspace:Spherecast(Vector3.new(-4,0,0), 1, Vector3.new(10,0,0))
            assert(hit.Instance == wall and math.abs(hit.Distance-2.975) < 0.001 and hit.Normal.X < -0.99)
            assert(math.abs(hit.Position.X+0.025) < 0.001)
            local edge = workspace:Spherecast(Vector3.new(-4,1.6,0), 1, Vector3.new(10,0,0))
            assert(edge and math.abs(edge.Distance-3.175) < 0.001 and edge.Normal.Y > 0.59)
            assert(workspace:Spherecast(Vector3.new(-4,1.8,1.8), 1, Vector3.new(10,0,0)) == nil, "expanded box false corner hit")
            assert(workspace:Spherecast(Vector3.zero, 1, Vector3.new(10,0,0)) == nil, "initial overlap should be ignored")
            wall:Destroy()
            local ball = Instance.new("Part", workspace); ball.Anchored = true
            ball.Shape = Enum.PartType.Ball; ball.Size = Vector3.new(2,2,2)
            hit = workspace:Spherecast(Vector3.new(-4,0,0), 1, Vector3.new(10,0,0))
            assert(math.abs(hit.Distance-2) < 0.001 and math.abs(hit.Position.X+1) < 0.001)
            local params = RaycastParams.new(); params.FilterDescendantsInstances = {ball}
            assert(workspace:Spherecast(Vector3.new(-4,0,0),1,Vector3.new(10,0,0),params) == nil)
            assert(not pcall(function() workspace:Spherecast(Vector3.zero, -1, Vector3.one) end))
            print("sweep")
            """, 0, "sweep\n", "");
        foreach (var test in GetFilterCases()) yield return test;
        yield return ("physics queries skip invalid legacy geometry", """
            local invalid = Instance.new("Part", workspace); invalid.Anchored = true
            invalid.Position = Vector3.new(math.huge,0,0)
            local invalidSize = Instance.new("Part", workspace); invalidSize.Anchored = true
            invalidSize.Size = Vector3.new(0/0,2,2)
            local floor = Instance.new("Part", workspace); floor.Anchored = true
            floor.Size = Vector3.new(20,2,20); floor.Position = Vector3.new(0,-1,0)
            local ray = workspace:Raycast(Vector3.new(0,5,0), Vector3.new(0,-10,0))
            assert(ray.Instance == floor and math.abs(ray.Distance-5) < 0.001, "invalid part poisoned ray")
            local sweep = workspace:Spherecast(Vector3.new(0,5,0),1,Vector3.new(0,-10,0))
            assert(sweep.Instance == floor and math.abs(sweep.Distance-4) < 0.001, "invalid part poisoned sweep")
            print("finite")
            """, 0, "finite\n", "");
    }
    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetSphereCases()
    {
        yield return ("physics sphere settles on a box floor", """
            workspace.Gravity = 20
            local floor = Instance.new("Part", workspace)
            floor.Anchored = true; floor.Size = Vector3.new(40, 2, 40); floor.Position = Vector3.new(0, -1, 0)
            floor.CustomPhysicalProperties = PhysicalProperties.new(1, 0, 0)
            local ball = Instance.new("Part", workspace)
            ball.Shape = Enum.PartType.Ball; ball.Size = Vector3.new(2, 2, 2); ball.Position = Vector3.new(0, 5, 0)
            ball.CustomPhysicalProperties = floor.CustomPhysicalProperties
            task.wait(1)
            assert(math.abs(ball.Position.Y - 1) < 0.03, "sphere did not settle")
            assert(math.abs(ball.AssemblyLinearVelocity.Y) < 0.05)
            assert(ball:Clone().Shape == Enum.PartType.Ball)
            assert(not pcall(function() ball.Shape = Enum.PartType.Cylinder end))
            assert(not pcall(function() ball.Shape = Enum.RunContext.Legacy end))
            print("sphere")
            """, 0, "sphere\n", "");
        yield return ("physics sphere narrowphase rejects box corner false positives", """
            workspace.Gravity = 0
            local box = Instance.new("Part", workspace); box.Anchored = true; box.Size = Vector3.new(2,2,2)
            local ball = Instance.new("Part", workspace); ball.Shape = Enum.PartType.Ball
            ball.Size = Vector3.new(2,2,2); ball.Position = Vector3.new(1.8,1.8,0)
            task.wait(0.1)
            assert((ball.Position-Vector3.new(1.8,1.8,0)).Magnitude < 0.001, "sphere used box geometry")
            print("corner")
            """, 0, "corner\n", "");
        yield return ("physics spheres exchange momentum", """
            workspace.Gravity = 0
            local a = Instance.new("Part", workspace); a.Shape = Enum.PartType.Ball
            a.Size = Vector3.new(2,2,2); a.Position = Vector3.new(-3,0,0)
            a.CustomPhysicalProperties = PhysicalProperties.new(1,0,1)
            a.AssemblyLinearVelocity = Vector3.new(8,0,0)
            local b = a:Clone(); b.Position = Vector3.new(1,0,0); b.AssemblyLinearVelocity = Vector3.zero; b.Parent = workspace
            task.wait(0.5)
            assert(math.abs(a.AssemblyLinearVelocity.X) < 0.05 and math.abs(b.AssemblyLinearVelocity.X-8) < 0.05)
            print("momentum")
            """, 0, "momentum\n", "");
    }
    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetFilterCases()
    {
        yield return ("physics raycast filters self and intersects sphere surfaces", """
            local self = Instance.new("Model", workspace)
            local body = Instance.new("Part", self); body.Anchored = true; body.Size = Vector3.new(2,2,2)
            local ball = Instance.new("Part", workspace); ball.Anchored = true
            ball.Shape = Enum.PartType.Ball; ball.Size = Vector3.new(2,2,2); ball.Position = Vector3.new(0,-5,0)
            local params = RaycastParams.new(); params.FilterDescendantsInstances = {self}
            local hit = workspace:Raycast(Vector3.zero, Vector3.new(0,-10,0), params)
            assert(hit.Instance == ball and math.abs(hit.Distance-4) < 0.001 and hit.Normal.Y > 0.99)
            assert(workspace:Raycast(Vector3.new(0.9,-3,0.9), Vector3.new(0,-4,0), params) == nil)
            params.FilterType = Enum.RaycastFilterType.Include; params.FilterDescendantsInstances = {self}
            assert(workspace:Raycast(Vector3.new(0,4,0), Vector3.new(0,-10,0), params).Instance == body)
            body.CanCollide = false; params.RespectCanCollide = true
            assert(workspace:Raycast(Vector3.new(0,4,0), Vector3.new(0,-10,0), params) == nil)
            print("query")
            """, 0, "query\n", "");
    }
    private static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetSphereContactCases()
    {
        yield return ("physics box rests on an anchored sphere", """
            workspace.Gravity = 20
            local sphere = Instance.new("Part", workspace); sphere.Shape = Enum.PartType.Ball
            sphere.Anchored = true; sphere.Size = Vector3.new(6,6,6); sphere.Position = Vector3.new(0,-3,0)
            sphere.CustomPhysicalProperties = PhysicalProperties.new(1,0,0)
            local box = Instance.new("Part", workspace); box.Size = Vector3.new(2,2,2)
            box.Position = Vector3.new(0,2,0); box.CustomPhysicalProperties = sphere.CustomPhysicalProperties
            task.wait(0.8)
            assert(math.abs(box.Position.Y-1) < 0.03 and math.abs(box.AssemblyLinearVelocity.Y) < 0.05)
            print("support")
            """, 0, "support\n", "");
        yield return ("physics sphere escapes an initial box overlap", """
            workspace.Gravity = 0
            local box = Instance.new("Part", workspace); box.Anchored = true; box.Size = Vector3.new(10,10,10)
            local sphere = Instance.new("Part", workspace); sphere.Shape = Enum.PartType.Ball
            sphere.Size = Vector3.new(2,2,2)
            task.wait(0.1)
            assert(sphere.Position.X > 5.99 and math.abs(sphere.Position.Y) < 0.001)
            assert(sphere.AssemblyLinearVelocity.Magnitude < 0.001)
            print("overlap")
            """, 0, "overlap\n", "");
        yield return ("physics sphere rolls down a rotated box ramp", """
            workspace.Gravity = 50
            local material = PhysicalProperties.new(1,1,0)
            local ramp = Instance.new("Part", workspace); ramp.Anchored = true
            ramp.Size = Vector3.new(40,1,20); ramp.CFrame = CFrame.Angles(0,0,math.rad(20)); ramp.CustomPhysicalProperties = material
            local sphere = Instance.new("Part", workspace); sphere.Shape = Enum.PartType.Ball
            sphere.Size = Vector3.new(2,2,2); sphere.CFrame = ramp.CFrame * CFrame.new(0,1.5,0)
            sphere.CustomPhysicalProperties = material
            task.wait(0.3)
            local point = ramp.CFrame:PointToObjectSpace(sphere.Position)
            assert(point.X < -0.3 and point.Y > 1.45, "sphere did not roll on ramp")
            assert(sphere.AssemblyAngularVelocity.Magnitude > 0.5, "contact did not generate sphere spin")
            print("rolling")
            """, 0, "rolling\n", "");
    }
}
