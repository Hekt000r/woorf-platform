# Initial client/server runtime

Run a project with an authoritative server and two local headless clients:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/client-server --clients 2
```

The original one-argument invocation still runs only server scripts. Each endpoint has a separate C# DataModel and Luau VM, including independent globals, module caches, and script environments. This milestone uses queued in-process transport; it does not yet connect separate machines or processes.

The design follows Roblox's [client/server runtime](https://create.roblox.com/docs/projects/client-server), [RemoteEvent API](https://create.roblox.com/docs/reference/engine/classes/RemoteEvent), [remote arguments](https://create.roblox.com/docs/scripting/events/remote), [Players](https://create.roblox.com/docs/reference/engine/classes/Players), and [RunService](https://create.roblox.com/docs/reference/engine/classes/RunService) documentation. Compatibility is checked with local integration tests, without a Studio comparison harness.

RemoteEvents support FireServer, FireClient, FireAllClients, OnServerEvent, and OnClientEvent. Sending queues a one-way message without waiting for listeners. OnServerEvent receives the host-associated sending Player before payload arguments; payloads cannot choose that identity. FireClient targets a connected Player. Calls from the wrong execution side raise errors.

Players exposes LocalPlayer on clients, GetPlayers, and PlayerAdded. Local test players have deterministic names and UserIds; these are local identities, not Roblox accounts. RunService exposes IsServer and IsClient. StarterPlayer/StarterPlayerScripts scripts are copied into each local player's PlayerScripts; applicable client scripts execute in that client's VM.

Server-visible shared state replicates to client copies; client mutations do not write into the server graph. Shared Instance references in remote messages resolve to the receiving endpoint's corresponding object. References unavailable to the receiver become nil. Argument tables are copied, rather than sharing memory across endpoints. Supported values and replication limits are described below as the implemented subset is verified.

RemoteFunction, UnreliableRemoteEvent, socket transport, real authentication, characters, physics ownership, streaming, and Roblox scheduling/network-rate fidelity remain future work. Indefinite waits alone do not keep the headless CLI alive; the host exits after queued work and timers drain.
