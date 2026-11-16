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

UnreliableRemoteEvent, real authentication, characters, physics ownership, streaming, and Roblox scheduling/network-rate fidelity remain future work. Indefinite waits alone do not keep the headless CLI alive; the host exits after queued work and timers drain.

## RemoteFunctions

RemoteFunction supports InvokeServer/OnServerInvoke and InvokeClient/OnClientInvoke.
Assign a function to the callback on the receiving side. Invocation suspends the
calling coroutine until that callback returns; callbacks may yield. Requests made
before callback assignment wait for an assignment. Concurrent requests correlate
their own replies, and server callbacks receive the endpoint-associated Player.

Arguments and return tuples use the same copying and Instance-reference mapping
as RemoteEvents, including nil values. Callback errors propagate to the caller
and can be caught with pcall. Callback members are write-only; assigning nil clears
a callback. Calls from the wrong side and invalid target Players raise errors.
Destroying the remote fails pending local invocations. Destroying the callback's
Script cancels its suspended work and replies with an error.

There is no automatic invocation timeout. As with other unsignalled waits, pending
calls alone do not keep the headless host alive after all runnable work and timers
drain. Disconnecting the target fails pending InvokeClient calls. In-flight server callbacks from a disconnected client are cancelled.

The API follows the [Roblox RemoteFunction reference](https://create.roblox.com/docs/reference/engine/classes/RemoteFunction).

Run the request/reply example with:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/remote-functions --clients 2
```

## Player connection lifecycle

The embedding API supports joining and leaving after startup. Calls are synchronous
and must be made on the host thread between execution calls:

```csharp
using var host = new LocalNetworkHost(project, new MonotonicClock());
host.Start();                       // Returns the server startup status.
var first = host.ConnectClient();    // Returns the server-side Player.
host.RunUntilIdle();                 // Returns execution status; keeps the host reusable.
var second = host.ConnectClient();
host.DisconnectClient(first);        // Repeated disconnects are harmless.
host.RunUntilIdle();
```

Each new client receives the current server snapshot before its LocalScripts run.
Existing clients observe PlayerAdded with the joining player's identity initialized.
Local player identities increase monotonically and are not reused after disconnect.

PlayerRemoving fires before the Player leaves Players, on the server and remaining
clients. The departing client's VM is disposed, cancelling its timers and callbacks.
Its replicas disappear from remaining clients and GetPlayers stops returning it.
Queued messages associated with departed endpoints are discarded during host drainage.
Pending InvokeClient calls fail with a catchable disconnection error; suspended
InvokeServer callbacks originating from that player are cancelled.

Player:Kick(message) queues a disconnect for the next safe host boundary. A server
can kick a connected player; a client can kick only its own LocalPlayer. The message
is currently accepted without a user-facing disconnect screen. The CLI --clients
option continues to start an initial batch of clients; dynamic joins use the
embedding API. This host API provides an in-process, headless lifecycle. TCP transport is available
separately; authentication, reconnect, and character lifecycle remain future work.

Separate processes can use the [TCP transport](socket-transport.md). The local host
API and --clients mode remain available for in-process tests.
