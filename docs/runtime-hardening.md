# Runtime lifecycle, budgets and local identity

The backend finishing milestone adds dynamic script lifecycle, VM execution
budgets, inbound limits, atomic UpdateAsync and reconnect credentials. These are
local runtime contracts, not full Roblox behavioral conformance or production certification.

## Script lifecycle

Script and LocalScript expose Enabled (default true) and its inverse Disabled.
Script.RunContext accepts Enum.RunContext.Legacy, Server and Client. The enum items
are immutable; Plugin is recognized but rejected for execution. Broader Enum APIs
are not implemented. ModuleScripts remain on-demand and have no Enabled flag.

Legacy server Scripts run below Workspace or ServerScriptService. Server-context
Scripts also run below ReplicatedStorage. LocalScripts run below the local Player,
including the copied PlayerScripts and PlayerGui trees. Client-context Scripts
also run in Workspace, ReplicatedStorage and ReplicatedFirst on clients. Template
containers do not independently execute their contents.

The runtime checks hierarchy, Enabled and RunContext changes at scheduler/host
boundaries. Newly eligible scripts start, including clones with loaded source.
Scripts leaving eligible ancestry, being disabled or changing execution context
stop their owned timers, waits, pending calls and registered signal connections.
Re-enabling restarts the script with a fresh environment; completed scripts do not
rerun merely because another tick occurs. Changes coalesce between boundaries.
The currently executing callback is not synchronously unwound by setting Enabled.

Initial server startup preserves project source-path order. Client startup copies
StarterPlayerScripts and StarterGui before starting eligible scripts. Source remains
read-only through Luau; cloning a loaded script copies its source. This does not
add source editing, characters/Backpack startup, Studio plugin execution or exact
Roblox simulation-phase behavior. Script enabled/context state and client-executable
sources replicate over local hosting and TCP. Private server source is excluded.

## Execution budgets

Luau VM interrupts limit each native VM entry to 250 ms of script execution time.
Managed host operations are excluded from that slice; nested callbacks receive
their own slice. An outer entry also has a five-second wall-clock deadline checked
at VM safepoints. Host calls are synchronous: disk I/O, lock waits and native library
operations cannot be interrupted while inside that call. The clock is independent
of the scheduler's virtual test clock.

An exceeded budget poisons that VM and returns failure; the engine abandons its
suspended work rather than resuming inconsistent scheduler state. Catching errors
with pcall cannot bypass the limit. Startup, task, signal and shutdown callbacks
are covered. Shutdown failures propagate to execution status; a poisoned VM skips
further shutdown callbacks. This is fail-closed session termination, not per-script
fault isolation. CPU limits do not provide a memory quota or an OS sandbox.

## Inbound limits

Each TCP server peer has a token bucket allowing 120 frames per second with a
120-frame burst, and 1 MiB per second with a burst of 4 MiB plus the frame header.
Exceeding either disconnects that peer before further dispatch. The receive pump
accepts at most 256 KiB and decodes at most 32 frames per peer per tick. Existing
4 MiB frame, handshake timeout and 32-peer bounds still apply. Bytes are charged
before decoding; packet admission is checked before deserializing each frame.

Local clients also have the 120-request bucket before enqueueing server-bound
messages; exceeding it queues disconnection and raises an error to the sender.
Socket clients do not rate-limit server responses with the server admission policy.
These bounds do not establish acceptable sustained load, application-specific
quotas, bounded long-lived RPC counts or protection from connection churn.

## Atomic local stores

UpdateAsync(key, transform) holds the store's exclusive file lock across reading,
transforming and durable atomic replacement. The transform receives an independent
JSON-compatible value or nil and must return exactly one JSON-compatible value.
Returning nil cancels the update and returns nil. Errors, unsupported values and
yield attempts leave stored data unchanged. Nested DataStore operations inside a
transform are rejected. A subsequent operation can proceed after any such failure.

The local transform runs once under the lock, rather than emulating cloud retry,
version metadata or quota behavior. Separate-process tests update a composite
profile concurrently and verify that all currency and item changes survive.
GetAsync followed by SetAsync remains two operations; callers must use UpdateAsync
for atomic read/modify/write behavior. File locking is local filesystem coordination,
not distributed storage. Operations remain synchronous.

The round game uses atomic rewards and purchases, including checking available
currency inside the purchase transaction. Leave/shutdown hooks update save metadata
without overwriting newer balances. This prevents stale snapshots overwriting
concurrent progress; it does not add distributed game-session ownership.

## Reconnect credentials

TCP protocol v4 issues a cryptographically random bearer credential to a new
connection. The server stores its hash and stable UserId in project-root/.woorf/auth,
using locked atomic writes. Existing valid credentials recover the same UserId
across disconnects and server restarts. Unknown, malformed and duplicate active
credentials on the same server are rejected. Credentials never become Instance
properties or replicated state; welcome packets contain only that peer's credential.

Use one identity file per local player:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- --connect 127.0.0.1:7777 --identity-file .woorf/client-alpha.json
```

Connect(address, cancellationToken, observe, identityFile) exposes the same option
for embedding. The client atomically persists the credential before starting its
scripts. Reuse that file for the same server/project, including when its address
changes. A connection without an identity file is a new guest; retain a file when
progress must belong to a returning player. Concurrent clients need separate files.
Keep server auth state with its data backups; resetting it can reuse numeric IDs.

This authenticates possession of a locally issued secret. It is not Roblox or an
external account login. Registration is open, credentials have no recovery/revocation
flow, and transport has no TLS. Use loopback or an independently secured transport;
anyone obtaining a credential can act as its owner. Credential files contain secrets
and should remain in a private directory outside version control. In-process
LocalNetworkHost identities remain host-assigned session identities.

## Verification

The integration suite covers dynamic server/client startup, disable/re-enable and
stale-work cleanup, context changes, fatal dynamic errors, uncatchable runaway loops,
shutdown failure status, request and byte floods, healthy-peer isolation, atomic
rollback/concurrent updates, forged/duplicate credentials, and reordered reconnects.
The round-game test restarts the server with reversed client connection order and
verifies restored currency, inventory, rounds and leave/shutdown metadata.
