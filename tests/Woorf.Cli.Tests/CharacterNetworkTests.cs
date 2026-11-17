using System.Diagnostics;
using static NetworkProjectTests;

static class CharacterNetworkTests
{
    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-character-network-" + Guid.NewGuid().ToString("N"));
        var processes = new List<Process>();
        try
        {
            WriteProject(root);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var server = Start(typeof(CharacterNetworkTests).Assembly.Location, "--reference-host", root); processes.Add(server);
            var serverError = server.StandardError.ReadToEndAsync();
            var listening = await server.StandardOutput.ReadLineAsync(timeout.Token);
            if (listening is null || !listening.StartsWith("Listening ")) throw new Exception("Character server did not listen.");
            var clients = new[] { Start(cliPath, "--connect", listening[10..]), Start(cliPath, "--connect", listening[10..]) };
            processes.AddRange(clients);
            foreach (var client in clients)
            {
                var output = client.StandardOutput.ReadToEndAsync(); var error = client.StandardError.ReadToEndAsync();
                await client.WaitForExitAsync(timeout.Token);
                if (client.ExitCode != 0 || (await output).Trim() != "character-client" || await error != "")
                    throw new Exception("Character client failed: " + await output + await error);
            }
            if (await server.StandardOutput.ReadLineAsync(timeout.Token) != "character-server") throw new Exception("Server did not complete both character sessions.");
            await server.StandardInput.WriteLineAsync("stop"); await server.WaitForExitAsync(timeout.Token);
            if (server.ExitCode != 0 || await serverError != "") throw new Exception("Character server failed: " + await serverError);
            Console.WriteLine("PASS two TCP characters walk collide jump die and respawn with authoritative replicated state");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL character TCP: " + error.Message); return 1; }
        finally
        {
            foreach (var process in processes)
            {
                if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); }
                process.Dispose();
            }
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static Process Start(string assembly, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(assembly);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start)!;
    }

    private static void WriteProject(string root)
    {
        Write(root, "woorf.project.json", "{}");
        Write(root, "src/ServerScriptService/Main.server.luau", Server);
        Write(root, "src/ReplicatedStorage/CharacterLocal.client.luau", "script.Parent:SetAttribute('LocalCharacterScript', true)");
        WriteClient(root, Client);
    }

    private const string Server = """
        workspace.Gravity = 50
        local players = game:GetService("Players")
        players.CharacterAutoLoads = true; players.RespawnTime = 0.06
        local floor = Instance.new("Part", workspace); floor.Anchored = true
        floor.Size = Vector3.new(100,2,100); floor.Position = Vector3.new(0,-1,0)
        local wall = Instance.new("Part", workspace); wall.Anchored = true
        wall.Size = Vector3.new(1,20,60); wall.Position = Vector3.new(10,10,0)
        local message = Instance.new("RemoteEvent", game:GetService("ReplicatedStorage")); message.Name = "Control"
        local spawns, ready, done = {}, 0, 0
        players.PlayerAdded:Connect(function(player)
            spawns[player] = 0
            player.CharacterAdded:Connect(function(character)
                spawns[player] += 1; character.Humanoid.WalkSpeed = 8; character.Humanoid.JumpPower = 20
                game:GetService("ReplicatedStorage").CharacterLocal:Clone().Parent = character
            end)
        end)
        message.OnServerEvent:Connect(function(player, kind, direction)
            local character = player.Character
            if not character then return end
            if kind == "ready" then
                ready += 1; if ready == 2 then message:FireAllClients("start") end
            elseif kind == "walk" then
                if typeof(direction) ~= "Vector3" or direction.Magnitude > 1 then return end
                character.Humanoid:Move(direction)
            elseif kind == "jump" then character.Humanoid.Jump = true
            elseif kind == "die" then character.Humanoid.Health = 0
            elseif kind == "done" then
                assert(spawns[player] == 2 and character.Humanoid.Health == 100)
                done += 1; if done == 2 then print("character-server") end
                message:FireClient(player, "done")
            end
        end)
        """;

    private const string Client = """
        local player = game:GetService("Players").LocalPlayer
        local message = game:GetService("ReplicatedStorage"):WaitForChild("Control")
        local started = false
        message.OnClientEvent:Connect(function(kind)
            if kind == "start" then started = true
            elseif kind == "done" then print("character-client"); player:Kick() end
        end)
        repeat task.wait(0.02) until player.Character
        local original = player.Character
        task.wait(0.05)
        assert(original:GetAttribute("LocalCharacterScript") == true, "Owned character LocalScript did not start")
        assert(original.PrimaryPart == original.HumanoidRootPart and original.Humanoid.WalkSpeed == 8)
        message:FireServer("ready")
        repeat task.wait(0.02) until started
        task.wait(0.5)
        local start = original.PrimaryPart.Position
        message:FireServer("walk", Vector3.xAxis)
        task.wait(1.3)
        message:FireServer("walk", Vector3.zero)
        assert(original.PrimaryPart.Position.X > start.X + 1 and original.PrimaryPart.Position.X < 8.6,
            "walk start=" .. tostring(start) .. " end=" .. tostring(original.PrimaryPart.Position))
        assert(original.PrimaryPart.CFrame.UpVector.Y > 0.99)
        local ground = original.PrimaryPart.Position.Y
        message:FireServer("jump")
        task.wait(0.12)
        assert(original.PrimaryPart.Position.Y > ground + 0.5)
        local added, removed = 0, 0
        player.CharacterAdded:Connect(function(character)
            assert(character.PrimaryPart and character.Parent == workspace)
            added += 1
        end)
        player.CharacterRemoving:Connect(function() removed += 1 end)
        message:FireServer("die")
        repeat task.wait(0.02) until player.Character and player.Character ~= original
        assert(added == 1 and removed == 1 and original.Parent == nil)
        message:FireServer("done")
        """;
}
