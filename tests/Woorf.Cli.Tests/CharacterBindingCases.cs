static class CharacterBindingCases
{
    internal static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return ("character bindings validate movement properties and direction", """
            local h = Instance.new("Humanoid")
            assert(h.WalkSpeed == 16 and h.JumpPower == 30 and h.MoveDirection == Vector3.zero)
            h.WalkSpeed = 12; h.JumpPower = 35; h.Jump = true
            h:Move(Vector3.new(3,5,4))
            assert(math.abs(h.MoveDirection.Magnitude - 1) < 1e-5 and h.MoveDirection.Y == 0)
            assert(h.Jump and h.WalkSpeed == 12 and h.JumpPower == 35)
            for _, key in {"WalkSpeed", "JumpPower"} do
                assert(not pcall(function() h[key] = -1 end))
                assert(not pcall(function() h[key] = 0/0 end))
            end
            assert(not pcall(function() h.Jump = 1 end))
            assert(not pcall(function() h:Move(Vector3.one, true) end))
            assert(not pcall(function() h.MoveDirection = Vector3.one end))
            """, 0, "", "");
        yield return ("character bindings spawn walk die respawn and clean up on player removal", """
            local floor = Instance.new("Part", workspace)
            floor.Anchored = true; floor.Size = Vector3.new(100,2,100); floor.Position = Vector3.new(0,-1,0)
            local players = game:GetService("Players")
            assert(not players.CharacterAutoLoads)
            players.RespawnTime = 0.05
            local p = Instance.new("Player", players)
            assert(p.Character == nil)
            local adds, removes = 0, 0
            p.CharacterAdded:Connect(function(character) assert(p.Character == character) adds += 1 end)
            p.CharacterRemoving:Connect(function(character) assert(p.Character == character) removes += 1 end)
            p:LoadCharacter()
            local first = p.Character
            assert(first.Parent == workspace and first.HumanoidRootPart == first.PrimaryPart and adds == 1)
            local start = first.PrimaryPart.Position
            first.Humanoid:Move(Vector3.xAxis)
            task.wait(0.12)
            assert(first.PrimaryPart.Position.X > start.X + 0.5)
            players.CharacterAutoLoads = true
            first.Humanoid.Health = 0
            task.wait(0.15)
            assert(p.Character ~= first and adds == 2 and removes == 1 and first.Parent == nil)
            local second = p.Character
            p:Destroy()
            task.wait(0.02)
            assert(second.Parent == nil)
            """, 0, "", "");
    }
}
