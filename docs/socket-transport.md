# TCP transport

Run an authoritative server in one terminal:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/remote-functions --listen 127.0.0.1:7777
```

Connect from two other terminals, running this in each:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- --connect 127.0.0.1:7777
```

Clients receive shared state and client script templates from the server; they do
not need a local project directory. The server prints its listening address.
Port 0 requests an available server port. Pass --identity-file <path> when connecting to retain a local player identity; concurrent players need separate files. Stop the server or a client with Ctrl+C.
The server stays alive while waiting for connections; clients stay connected
after runnable script work drains. Player:Kick disconnects a socket client.

The existing single-script, server-only, and --clients N modes still work.
Embedders can call SocketTransport.Listen(project, address, cancellationToken)
or SocketTransport.Connect(address, cancellationToken).

## Runtime behavior

Each process owns its own Luau VM and DataModel. New guests receive monotonically
allocated local Player identities assigned by the server. A saved server-issued credential restores the same identity on reconnect; see [credentials](runtime-hardening.md#reconnect-credentials). A versioned handshake
precedes the initial shared-state snapshot. The snapshot includes client script
templates; private server-executed source and private storage are excluded. Script
source is sent when its RunContext explicitly makes it client-executable. Client scripts
start after their LocalPlayer and the initial snapshot are available.

The server sends incremental replication updates before related remote messages.
Each connection starts with a full snapshot, then receives additions, changed
properties, and removed Instance IDs. Unchanged objects and properties are omitted.
Each peer has its own baseline, including clients that join after earlier changes.
Clients preserve local edits until the corresponding server property changes.
Updates are applied before related remotes so new Instance references resolve.

The server captures the shared graph once, then tracks property and hierarchy
mutations. Repeated mutations coalesce into one final capture per affected instance.
Idle ticks do not traverse or recapture the shared graph, and unchanged peers skip
delta comparisons. Late joins receive a full snapshot assembled from the cached
state. Clients apply only added or changed nodes plus explicit removals.

Moving a subtree into or out of shared ancestry updates its descendants. Dependent
automatic model pivots and external PrimaryPart visibility references are tracked;
fixed model pivots are not recaptured for unrelated geometry edits. Automatic pivot
calculations can still inspect descendants of an affected model. Snapshot chunking
and interest management remain future optimizations.

RemoteEvents and RemoteFunctions use the existing value-copying semantics:
nil-preserving tuples, tables, booleans, numbers, strings, Vector3, CFrame, Color3, and
replicated Instance references. Local or private Instance references become nil.
The sending Player is derived from the socket connection. Clients cannot supply
a sender identity or write replication state to the server.

The server detects explicit leave messages and socket EOF/reset, fires
PlayerRemoving before removing the Player, and cancels pending client RPC work.
Other clients continue running and receive the updated player list. The departing
client's process stops its VM. Loss of the server connection stops the client.

## Protocol and limits

Protocol v4 uses UTF-8 JSON frames with a four-byte big-endian byte-length prefix.
Frames are limited to 4 MiB. Invalid versions, lengths, remote targets, and values
disconnect the offending peer. Handshakes time out after five seconds and the
server accepts up to 32 simultaneous peers. Socket reads, nonblocking writes, and VM execution occur on the runtime thread.
Each peer has an 8 MiB outgoing byte budget, including deferred remote frames and
four-byte headers. Writes send at most 256 KiB per peer per tick, retain partial-frame
offsets, and yield immediately when the socket would block. The OS send buffer is
set to 64 KiB separately from the application queue.

Queue overflow disconnects the affected peer at a safe runtime boundary. Queued
writes that make no progress for five seconds also disconnect that peer. Idle
connections without queued data are not treated as stalled. Replication frames
remain ordered before related remote messages. Client leave requests drain their
queued messages before closing.

This milestone provides ordered TCP delivery and local identity, with server-issued reconnect credentials and inbound admission limits. External account
authentication, TLS, automatic reconnect/state resume, compression, interest management
and Roblox network-rate fidelity remain unsupported. Use loopback addresses for local testing. Binding another interface
permits connections reachable on that interface; there is no TLS or external account authentication. Reconnect credentials are bearer secrets.

## Incremental replication verification

Integration tests inspect real TCP packets to verify property-only patches,
additions, removals, replication-before-remote ordering, and absence of replication
for no-op requests. Separate client processes verify reparenting and deletion,
preserved local edits, and RPC references after updates. The transport tests also
cover fragmented headers/payloads, unsupported protocol versions, oversized frames,
invalid remote data, and abrupt disconnects. Real slow-reader tests cover queue overflow, stalled writes below the byte limit,
and continued healthy-client replies. A partial-header disconnect is also exercised.
These are bounded functional tests; prolonged load testing remains further work.

## Transport measurements

Listen and Connect accept an optional observe callback. It runs on the runtime
thread after network work, once per tick:

```csharp
SocketTransport.Listen(project, "127.0.0.1:7777", cancellationToken, metrics =>
{
    // Record the sample without blocking the runtime thread.
    Console.WriteLine(metrics);
});
```

SocketTransportMetrics reports current aggregate queued bytes, cumulative sent bytes,
cumulative replication bytes, the latest tick duration in milliseconds, connected
peer count, slow-peer disconnect count, and peak bytes queued by one peer.
ReplicationNodesCaptured counts instance state captures; ReplicationFullScans counts
initial shared-hierarchy scans. These counters allow checking that idle ticks perform
no new captures or scans. Byte
counters include frame headers; replication counts welcome and delta frames. Sent
counters measure bytes accepted by the socket, not application-level acknowledgments.
Cumulative sent counters include peers that have disconnected. Tick duration excludes
the observer callback and the loop's deliberate sleep.

This bounds application-level outgoing traffic and avoids blocking writes; script
VM budgets and inbound limits are now implemented; see [runtime hardening](runtime-hardening.md). Memory quotas and sustained-load qualification remain separate work.

Backend attributes, tags, typed values, health, team references and Color3 are
carried by protocol v4. See [backend APIs](backend-apis.md) for scope and limits.
