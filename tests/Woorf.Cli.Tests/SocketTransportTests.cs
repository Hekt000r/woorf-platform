using System.Diagnostics;

static class SocketTransportTests
{
    internal static async Task<int> RunAsync(string cliPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "woorf-sockets-" + Guid.NewGuid().ToString("N"));
        var processes = new List<Process>();
        try
        {
            Arrange(root);
            var server = Start(cliPath, root, "--listen", "127.0.0.1:0");
            processes.Add(server);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var line = await server.StandardOutput.ReadLineAsync(timeout.Token);
            if (line is null || !line.StartsWith("Listening "))
                throw new Exception("Server did not start a socket listener: " + line + await server.StandardError.ReadToEndAsync(timeout.Token));
            var address = line["Listening ".Length..];
            await SocketProtocolTests.CheckAsync(address, timeout.Token);
            if (await server.StandardOutput.ReadLineAsync(timeout.Token) != "abrupt-disconnect")
                throw new Exception("Pending InvokeClient did not fail after abrupt disconnect.");
            var completedLine = server.StandardOutput.ReadLineAsync(timeout.Token).AsTask();
            var serverError = server.StandardError.ReadToEndAsync();
            var clients = new[] { Start(cliPath, "--connect", address), Start(cliPath, "--connect", address) };
            processes.AddRange(clients);
            foreach (var client in clients)
            {
                var output = client.StandardOutput.ReadToEndAsync();
                var error = client.StandardError.ReadToEndAsync();
                await client.WaitForExitAsync(timeout.Token);
                if (client.ExitCode != 0 || (await output).Trim() != "socket-client" || await error != "")
                    throw new Exception("Socket client failed: " + await output + await error);
            }
            var text = await completedLine;
            if (text != "socket-server") throw new Exception("Server routing failed: " + text);
            server.Kill(true);
            await server.WaitForExitAsync(timeout.Token);
            if (await serverError != "") throw new Exception("Server error: " + await serverError);
            Console.WriteLine("PASS routes replication and bidirectional remotes across separate TCP processes");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL routes replication and bidirectional remotes across separate TCP processes: " + error.Message);
            return 1;
        }
        finally
        {
            foreach (var process in processes)
            {
                if (!process.HasExited) process.Kill(true);
                process.Dispose();
            }
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static Process Start(string cliPath, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(cliPath);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start)!;
    }

    private static void Arrange(string root)
    {
        NetworkProjectTests.Write(root, "woorf.project.json", "{\"name\":\"Sockets\",\"root\":\"src\"}");
        ArrangeServer(root);
        ArrangeClient(root);
    }

    private static void ArrangeServer(string root)
    {
        NetworkProjectTests.Write(root, "src/ServerScriptService/Main.server.luau", """
            local storage = game:GetService("ReplicatedStorage")
            local event = Instance.new("RemoteEvent", storage); event.Name = "Message"
            local query = Instance.new("RemoteFunction", storage); query.Name = "Query"
            local marker = Instance.new("Part", workspace); marker.Name = "Marker"; marker.Anchored = true
            marker:SetAttribute("Label", "start"); marker:AddTag("Active")
            local coins = Instance.new("IntValue", marker); coins.Name = "Coins"; coins.Value = 10
            local target = Instance.new("Folder", game:GetService("ServerStorage")); target.Name = "BackendTarget"
            local pointer = Instance.new("ObjectValue", marker); pointer.Name = "Pointer"; pointer.Value = target
            local nonFinite = Instance.new("Part", workspace); nonFinite.Name = "NonFinite"; nonFinite.Anchored = true
            nonFinite.Position = Vector3.new(math.huge, 0, 0)
            local deltaPart = Instance.new("Part", workspace); deltaPart.Name = "DeltaPart"; deltaPart.Anchored = true
            local removed = Instance.new("Folder", workspace); removed.Name = "DeltaRemoved"
            local ready, done = 0, 0
            query.OnServerInvoke = function(player, data)
                assert(data.id == player.UserId and data.ref == marker)
                task.wait(0.01)
                return player, data, nil
            end
            event.OnServerEvent:Connect(function(player, kind)
                if kind == "noop" then
                    event:FireClient(player, "noop")
                elseif kind == "delta" then
                    local added = Instance.new("Folder", workspace); added.Name = "DeltaAdded"
                    deltaPart.Name = "DeltaRenamed"; deltaPart.Parent = added
                    removed:Destroy()
                    event:FireClient(player, "delta done")
                elseif kind == "abrupt" then
                    local ok, message = pcall(function() query:InvokeClient(player) end)
                    assert(not ok and string.find(message, "disconnected", 1, true))
                    print("abrupt-disconnect")
                elseif kind == "ready" then
                    ready += 1
                    local count = ready
                    assert(query:InvokeClient(player) == player.UserId)
                    if count == 2 then
                        marker.Position = Vector3.new(7,8,9)
                        target.Parent = workspace
                        marker:SetAttribute("Label", "updated"); marker:RemoveTag("Active"); coins.Value = 20
                        local added = Instance.new("Folder", workspace); added.Name = "RuntimeAdded"
                        marker.Parent = added
                        workspace.DeltaAdded:Destroy()
                        event:FireAllClients("go", marker)
                    end
                else
                    assert(kind == "done")
                    done += 1
                    if done == 2 then print("socket-server") end
                end
            end)
            """);
    }

    private static void ArrangeClient(string root)
    {
        NetworkProjectTests.WriteClient(root, """
            local storage = game:GetService("ReplicatedStorage")
            local player = game:GetService("Players").LocalPlayer
            assert(game:GetService("ServerScriptService"):GetChildren()[1] == nil)
            assert(workspace.NonFinite.Position.X == math.huge)
            local markerCopy = workspace.Marker
            assert(markerCopy:GetAttribute("Label") == "start" and markerCopy:HasTag("Active"))
            assert(markerCopy.Coins.Value == 10 and markerCopy.Pointer.Value == nil)
            markerCopy.Name = "LocalMarker"
            storage.Query.OnClientInvoke = function() task.wait(0.01); return player.UserId end
            storage.Message.OnClientEvent:Connect(function(kind, marker)
                assert(kind == "go" and marker == markerCopy)
                assert(marker.Parent == workspace.RuntimeAdded and marker.Name == "LocalMarker")
                assert(workspace:FindFirstChild("DeltaAdded") == nil)
                assert(marker.Position == Vector3.new(7,8,9))
                assert(marker:GetAttribute("Label") == "updated" and not marker:HasTag("Active"))
                assert(marker.Coins.Value == 20 and marker.Pointer.Value == workspace.BackendTarget)
                local result = table.pack(storage.Query:InvokeServer({id = player.UserId, ref = marker}))
                assert(result.n == 3 and result[1] == player and result[2].ref == marker and result[3] == nil)
                print("socket-client")
                storage.Message:FireServer("done")
                player:Kick()
            end)
            storage.Message:FireServer("ready")
            """);
    }
}
