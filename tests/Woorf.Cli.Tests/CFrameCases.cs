static class CFrameCases
{
    private const string Near = """
        local function near(a, b)
            assert((a - b).Magnitude < 0.00001, tostring(a) .. " differs from " .. tostring(b))
        end
        
        """;

    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return GetConstructionCase();
        yield return GetRotationCase();
        yield return GetLookCase();
        yield return GetArithmeticCase();
        yield return GetTransformCase();
        yield return GetValidationCase();
        yield return GetVerticalLookCase();
        yield return GetOrthonormalizationCase();
    }

    private static (string, string?, int, string, string) GetConstructionCase() =>
        ("constructs immutable CFrames with Roblox matrix columns", Near + """
            assert(typeof(CFrame.new()) == "CFrame" and type(CFrame.new()) == "userdata")
            assert(CFrame.new() == CFrame.identity)
            local f = CFrame.new(10, 20, 30, 0, -1, 0, 1, 0, 0, 0, 0, 1)
            assert(f.Position == Vector3.new(10, 20, 30) and f.X == 10 and f.Y == 20 and f.Z == 30)
            assert(f.RightVector == Vector3.yAxis and f.UpVector == -Vector3.xAxis)
            assert(f.LookVector == -Vector3.zAxis)
            assert(f.XVector == f.RightVector and f.YVector == f.UpVector and f.ZVector == -f.LookVector)
            local numbers = table.pack(f:GetComponents())
            local expected = {10,20,30, 0,-1,0, 1,0,0, 0,0,1}
            assert(numbers.n == 12)
            for i, v in expected do assert(numbers[i] == v) end
            assert(f.Rotation.Position == Vector3.zero and f.Rotation.RightVector == f.RightVector)
            assert(CFrame.new(Vector3.new(10,20,30)) == CFrame.new(10,20,30))
            assert(CFrame.fromMatrix(Vector3.new(10,20,30), Vector3.yAxis, -Vector3.xAxis) == f)
            assert(CFrame.fromMatrix(Vector3.new(10,20,30), Vector3.yAxis, -Vector3.xAxis, Vector3.zAxis) == f)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetRotationCase() =>
        ("constructs CFrame rotations in documented Euler order", Near + """
            local quarter = math.pi / 2
            local xyz = CFrame.Angles(quarter, quarter, 0)
            near(xyz.RightVector, Vector3.yAxis)
            near(xyz.UpVector, Vector3.zAxis)
            near(xyz.ZVector, Vector3.xAxis)
            local alias = CFrame.fromEulerAnglesXYZ(quarter, quarter, 0)
            near(alias.RightVector, xyz.RightVector); near(alias.UpVector, xyz.UpVector)
            local yxz = CFrame.fromEulerAnglesYXZ(quarter, quarter, 0)
            near(yxz.RightVector, -Vector3.zAxis)
            near(yxz.UpVector, Vector3.xAxis)
            near(yxz.ZVector, -Vector3.yAxis)
            near(CFrame.fromOrientation(quarter, quarter, 0).UpVector, yxz.UpVector)
            near(CFrame.fromAxisAngle(Vector3.new(0,2,0), quarter).LookVector, -Vector3.xAxis)
            local quaternion = CFrame.new(1,2,3, 0,2,0,2)
            near(quaternion.LookVector, -Vector3.xAxis)
            assert(quaternion.Position == Vector3.new(1,2,3))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetLookCase() =>
        ("builds look frames using negative Z as forward", Near + """
            local at = Vector3.new(1,2,3)
            local f = CFrame.lookAt(at, Vector3.new(2,2,3))
            assert(f.Position == at)
            near(f.LookVector, Vector3.xAxis)
            near(f.RightVector, Vector3.zAxis)
            near(f.UpVector, Vector3.yAxis)
            near(CFrame.lookAlong(at, Vector3.new(5,0,0)).RightVector, Vector3.zAxis)
            near(CFrame.new(at, Vector3.new(2,2,3)).LookVector, Vector3.xAxis)
            local rolled = CFrame.lookAlong(at, Vector3.new(0,0,-2), Vector3.xAxis)
            near(rolled.UpVector, Vector3.xAxis)
            near(rolled.RightVector, -Vector3.yAxis)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetArithmeticCase() =>
        ("composes CFrames and translates in world space", Near + """
            local f = CFrame.new(10,20,30, 0,-1,0, 1,0,0, 0,0,1)
            local composed = f * CFrame.new(2,3,4)
            assert(composed.Position == Vector3.new(7,22,34))
            assert(composed.RightVector == Vector3.yAxis)
            assert(f * Vector3.new(2,3,4) == Vector3.new(7,22,34))
            local moved = f + Vector3.new(2,3,4)
            assert(moved.Position == Vector3.new(12,23,34) and moved.Rotation == f.Rotation)
            assert((moved - Vector3.new(2,3,4)) == f)
            local rotated = f * CFrame.Angles(math.pi/2,0,0)
            near(rotated.RightVector, Vector3.yAxis)
            near(rotated.UpVector, Vector3.zAxis)
            near(rotated.ZVector, Vector3.xAxis)
            assert(f ~= CFrame.identity)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetTransformCase() =>
        ("inverts CFrames and transforms variadic frames points and directions", Near + """
            local f = CFrame.new(10,20,30, 0,-1,0, 1,0,0, 0,0,1)
            assert(f * f:Inverse() == CFrame.identity)
            local p, q = f:PointToWorldSpace(Vector3.new(2,3,4), Vector3.zero)
            assert(p == Vector3.new(7,22,34) and q == Vector3.new(10,20,30))
            local a, b = f:PointToObjectSpace(p,q)
            assert(a == Vector3.new(2,3,4) and b == Vector3.zero)
            local v, w = f:VectorToWorldSpace(Vector3.new(2,3,4), Vector3.xAxis)
            assert(v == Vector3.new(-3,2,4) and w == Vector3.yAxis)
            local x, y = f:VectorToObjectSpace(v,w)
            assert(x == Vector3.new(2,3,4) and y == Vector3.xAxis)
            local g, h = f:ToWorldSpace(CFrame.new(2,3,4), CFrame.identity)
            assert(g.Position == Vector3.new(7,22,34) and h == f)
            local i, j = f:ToObjectSpace(g,h)
            assert(i == CFrame.new(2,3,4) and j == CFrame.identity)
            assert(f:PointToWorldSpace() == f.Position)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetValidationCase() =>
        ("rejects invalid CFrame members constructors and operands", """
            local f = CFrame.identity
            assert(not pcall(function() f.Position = Vector3.one end))
            assert(not pcall(function() CFrame.identity = CFrame.new(1,2,3) end))
            assert(not pcall(function() return f.Unknown end))
            assert(not pcall(function() CFrame.new({}) end))
            assert(not pcall(function() CFrame.Angles({},0,0) end))
            assert(not pcall(function() CFrame.fromMatrix(Vector3.zero, {}, Vector3.yAxis) end))
            assert(not pcall(function() return f * 2 end))
            assert(not pcall(function() return f + CFrame.identity end))
            assert(not pcall(function() f:PointToWorldSpace({}) end))
            assert(not pcall(function() CFrame.identity.Inverse({}) end))
            assert(not pcall(function() f:ToWorldSpace(CFrame.identity, {}) end))
            """, 0, "", "");

    private static (string, string?, int, string, string) GetVerticalLookCase() =>
        ("uses X as up when the legacy look constructor faces vertically", Near + """
            local f = CFrame.new(Vector3.new(1,2,3), Vector3.new(1,5,3))
            near(f.LookVector, Vector3.yAxis)
            near(f.UpVector, Vector3.xAxis)
            near(f.RightVector, -Vector3.zAxis)
            """, 0, "", "");

    private static (string, string?, int, string, string) GetOrthonormalizationCase() =>
        ("orthonormalizes scaled skewed CFrame bases while retaining position", Near + """
            local f = CFrame.fromMatrix(Vector3.new(1,2,3), Vector3.new(2,0,0), Vector3.new(1,3,0), Vector3.new(0,0,4))
            local corrected = f:Orthonormalize()
            assert(corrected.Position == Vector3.new(1,2,3))
            near(corrected.RightVector, Vector3.xAxis)
            near(corrected.UpVector, Vector3.yAxis)
            near(corrected.ZVector, Vector3.zAxis)
            assert(f.RightVector == Vector3.new(2,0,0))
            """, 0, "", "");
}
