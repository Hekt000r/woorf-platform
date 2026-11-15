static class Vector3Cases
{
    public static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return ("constructs immutable Vector3 values with single precision components", """
            local v = Vector3.new(1, 2, 3)
            assert(typeof(v) == "Vector3" and type(v) == "userdata")
            assert(v.X == 1 and v.Y == 2 and v.Z == 3)
            assert(Vector3.new() == Vector3.zero)
            assert(Vector3.new(4) == Vector3.new(4, 0, 0))
            assert(Vector3.new(16777217, 0, 0).X == 16777216)
            assert(Vector3.one == Vector3.new(1, 1, 1))
            assert(Vector3.xAxis == Vector3.new(1, 0, 0))
            assert(Vector3.yAxis == Vector3.new(0, 1, 0))
            assert(Vector3.zAxis == Vector3.new(0, 0, 1))
            assert(not pcall(function() v.X = 5 end))
            assert(not pcall(function() Vector3.zero = v end))
            assert(not pcall(function() return v.Missing end))
            assert(not pcall(function() Vector3.new({}) end))
            print(v)
            """, 0, "1, 2, 3\n", "");
        yield return ("calculates componentwise Vector3 arithmetic and scalar scaling", """
            local a, b = Vector3.new(2, -4, 6), Vector3.new(1, 2, 3)
            assert(a + b == Vector3.new(3, -2, 9))
            assert(a - b == Vector3.new(1, -6, 3))
            assert(-a == Vector3.new(-2, 4, -6))
            assert(a * b == Vector3.new(2, -8, 18))
            assert(a / b == Vector3.new(2, -2, 2))
            assert(a * 2 == Vector3.new(4, -8, 12) and 2 * a == a * 2)
            assert(a / 2 == Vector3.new(1, -2, 3))
            assert(Vector3.new(-3, 5, 7) // 2 == Vector3.new(-2, 2, 3))
            assert(Vector3.new(-3, 5, 7) // b == Vector3.new(-3, 2, 2))
            assert(not pcall(function() return a + 1 end))
            assert(not pcall(function() return a * {} end))
            assert(a ~= b and a ~= {} and a == Vector3.new(2, -4, 6))
            """, 0, "", "");
        yield return ("derives magnitude normalization and vector products", """
            local v = Vector3.new(3, 4, 0)
            assert(v.Magnitude == 5)
            assert(math.abs(v.Unit.X - 0.6) < 0.000001 and math.abs(v.Unit.Y - 0.8) < 0.000001)
            assert(Vector3.new(1, 2, 3):Dot(Vector3.new(4, 5, 6)) == 32)
            assert(Vector3.xAxis:Cross(Vector3.yAxis) == Vector3.zAxis)
            assert(Vector3.yAxis:Cross(Vector3.xAxis) == -Vector3.zAxis)
            assert(Vector3.zero:Lerp(Vector3.new(4, 8, 12), 0.25) == Vector3.new(1, 2, 3))
            assert(Vector3.zero:Lerp(Vector3.one, 2) == Vector3.new(2, 2, 2))
            local zeroUnit = Vector3.zero.Unit
            assert(zeroUnit.X ~= zeroUnit.X and zeroUnit.Y ~= zeroUnit.Y and zeroUnit.Z ~= zeroUnit.Z)
            assert(not pcall(function() v:Dot({}) end))
            assert(not pcall(function() v:Lerp(Vector3.one, {}) end))
            """, 0, "", "");
        yield return ("uses Vector3 value equality for table keys and IEEE NaNs", """
            local key = Vector3.new(1, 2, 3)
            local values = {[key] = "found"}
            assert(values[Vector3.new(1, 2, 3)] == "found")
            local nan = Vector3.zero.Unit
            assert(nan ~= nan)
            assert(Vector3.new(1, 2, 3):Cross(Vector3.new(4, 5, 6)) == Vector3.new(-3, 6, -3))
            assert(Vector3.new(2, 4, 6):Lerp(Vector3.new(6, 12, 18), 0.5) == Vector3.new(4, 8, 12))
            assert(vector.dot(key, vector.create(4, 5, 6)) == 32)
            assert(vector.create(1, 2, 3) == key)
            """, 0, "", "");
    }
}
