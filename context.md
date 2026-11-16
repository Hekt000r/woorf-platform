# woorf project context

## Project

woorf is a platform-agnostic Roblox Studio and Roblox game runtime alternative written in C#. It embeds the official Luau runtime and aims for high compatibility with Roblox runtime APIs while preserving familiar Luau scripting and project conventions. Longer-term goals include a familiar Studio-style editor, WebAssembly builds for browser play, and standalone game builds.

The initial focus is headless runtime APIs and a CLI. Graphics, physics, multiplayer, client execution, and the editor are later work. Windows x64 is currently verified; Linux, macOS, mobile, and WASM targets have not yet been verified.

## Current implementation

- Official Luau 0.745 is vendored in `vendor/luau` and built with CMake. A native C++ bridge connects Luau to the C# engine and CLI.
- The C# DataModel/Instance graph supports a filesystem-based Roblox-style project layout and headless server-script execution.
- Implemented structural APIs include `game`, `workspace`, supported services, `Instance.new`, hierarchy traversal and lookup, parenting, cloning, and destruction.
- Spatial APIs include immutable single-precision Vector3 values (native Luau representation, C# methods) plus core CFrame constructors, composition, inversion, and space transforms; headless Part CFrame/Position/Size/Anchored properties, cloning, and property signals. See `docs/spatial-types.md` for scope.
- Pivot APIs include Part PivotOffset, Part/Model GetPivot and PivotTo, Model PrimaryPart/WorldPivot, cached descendant movement, and clone reference remapping. Workspace inherits Model pivot behavior. See `docs/pivots.md`.
- Signals include `Connect`, `Once`, `Disconnect`, hierarchy notifications, `Destroying`, `Changed`, and property-change signals.
- ModuleScript `require()` supports isolated environments, per-instance result and failure caching, yielding modules, shared concurrent requires, and cycle detection.
- The cooperative scheduler supports `task.spawn`, `task.defer`, `task.delay`, `task.wait`, `task.cancel`, signal `Wait`, yielding callbacks, and `WaitForChild`.
- Scheduler lifecycle behavior now cancels suspended work and yielding callbacks when their Script is destroyed, releases waiters of a canceled ModuleScript load, and caps nested synchronous task resumption at 100 levels.

The runtime is still a structural subset. Roblox API coverage has not been measured against a pinned API snapshot. Core Vector3/CFrame and a headless Part property subset are implemented. Local headless client/server execution, replication, and RemoteEvents are implemented; see docs/client-server.md. TCP transport supports separate processes; see docs/socket-transport.md. There is no physics, rendering, execution quotas, or Roblox-hosted service replacement yet.

## Tests and build

The integration test runner starts the actual CLI. Run the complete suite with:

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore
```

Build the native bridge when its C++ source changes:

```powershell
cmake --build build/native --target woorf_luau
```

The scheduler lifecycle, Vector3/Part, CFrame, pivot, and bounds milestone work were verified with the full integration suite after rebuilding the native bridge. The parts, CFrame, pivot, and bounds examples also ran successfully. Seven Model bounds cases pass alongside the full suite. The pivot milestone adds 15 passing CLI integration cases, including callback edits and recursive movement. The current changes may still be uncommitted; inspect `git status` before making further edits or commits.

## Client/server milestone

Projects support --clients N with isolated server/client VMs and DataModels, queued RemoteEvents, Players.LocalPlayer, and RunService context methods. See docs/client-server.md and examples/client-server. RemoteFunctions now support yielding request/reply calls in both directions, tuple copying, late callback assignment, concurrent requests, errors, and cancellation on callback Script destruction. TCP transport is implemented; character simulation remains future work.

## Previously recommended next milestone

Vector3, core CFrame operations, and synchronized Part CFrame/Position are implemented; see `docs/spatial-types.md`, `docs/cframe.md`, and `examples/cframe.luau`. Part/Model pivot APIs are implemented; see `docs/pivots.md` and `examples/pivots.luau`. Model bounds APIs (GetBoundingBox/GetExtentsSize) are implemented; see `docs/bounds.md` and `examples/bounds.luau`. A recommended next milestone is Color3 and basic Part appearance properties, while staying headless and deferring physics. Compatibility is guided by Roblox Engine API docs and local test-first implementation. The user explicitly requested no Studio behavior-comparison harness.

## Key project references

- [Project goals and open decisions](project.md)
- [Runtime compatibility inventory](docs/runtime-compatibility.md)
- [Headless spatial types](docs/spatial-types.md)
- [CFrame and Part transforms](docs/cframe.md)
- [Headless pivots](docs/pivots.md)
- [Model bounds](docs/bounds.md)
- [Scheduler semantics](docs/scheduler.md)
- [Filesystem project format](docs/project-format.md)
- [Build and test instructions](README.md)

Open project decisions include the precise scope of the 99% compatibility target, import requirements for Roblox place/model assets, client/server and multiplayer plans, standalone target platforms, hosted-service replacements, rendering/physics/editor technology choices, and project licensing.

## Player lifecycle milestone

LocalNetworkHost exposes Start, ConnectClient, DisconnectClient, and RunUntilIdle
for synchronous embedding on the host thread. Clients may join after server work
has run and receive the latest snapshot. PlayerRemoving fires before removal,
Kick queues safe disconnects, client VMs are disposed, pending InvokeClient calls
fail, and suspended server callbacks from departed clients are cancelled.
Remaining clients receive join/removal notifications. See docs/client-server.md.
TCP transport is now implemented; see the socket transport milestone below.

## Socket transport milestone

SocketTransport.Listen and Connect provide separate-process TCP server/client
execution, exposed through --listen ip:port and --connect host:port CLI modes.
A versioned bounded JSON protocol carries snapshots and remote payloads; identities
come from connections. Clients receive script templates from the server. EOF and
explicit leaves trigger player cleanup and RPC cancellation. See
docs/socket-transport.md for commands and current limits. Authentication, TLS,
reconnect, and dirty-property tracking and interest management remain future work.

## Incremental replication milestone

TCP protocol v2 now sends one initial snapshot per connection followed by additions,
changed properties, and removed Instance IDs. Per-peer baselines prevent late joins
from missing prior changes and avoid unchanged replication traffic. Clients merge
updates while retaining local edits for properties unchanged by the server. Dirty-property tracking now avoids per-tick shared graph scans; see the milestone below. Tests cover real packets, additions/removals/reparenting,
fragmented TCP input, no-op traffic, and client RPCs after updates. See
docs/socket-transport.md. Protocol v1 peers are rejected.

## Backpressure milestone

TCP writes now use nonblocking per-peer queues, including deferred remote payloads.
Queues are bounded to 8 MiB and pumps send at most 256 KiB per peer per tick.
Overflow disconnects happen at safe runtime boundaries; five seconds without write
progress also disconnects stalled peers. Partially sent frames retain offsets and
client leave messages drain before closure. The unbounded runtime outgoing queue
was removed. Listen/Connect optionally report SocketTransportMetrics for queued,
sent, and replication bytes, tick duration, peer count, slow disconnects, and peak
per-peer queued bytes. Tests use real non-reading clients plus a healthy client,
stalled sub-limit traffic, partial-header disconnects, and byte-accurate metrics.
See docs/socket-transport.md. Long-running load tests and inbound rate limiting are
still future work.

## Dirty replication tracking milestone

WireGraph subscribes to internal DataModel mutation notifications and caches shared
instance state. Only dirty instances and affected derived model state are recaptured.
Repeated changes coalesce; idle ticks perform no hierarchy scans, captures, or
per-peer full comparisons. Late-join snapshots use the cache. Client deltas apply
only changed/added nodes and explicit removals. Visibility changes process moved
subtrees and invalidate external PrimaryPart references; fixed model pivots are
excluded from unrelated spatial invalidations. Tracker subscriptions are detached
when the socket runtime is disposed. SocketTransportMetrics now includes
ReplicationNodesCaptured and ReplicationFullScans. Tests verify idle capture counts,
coalescing, automatic/fixed pivots, shared/private transitions, destruction, and
external PrimaryPart visibility alongside the complete integration suite.

## Backend API readiness audit (2026-11-16)

The user prioritized backend API readiness before characters/rendering. See
docs/backend-api-readiness.md and docs/api-audit/member-inventory.json.
Official creator-docs is pinned at 5a56d0d361d709aa785585b738354134ae47b4ae:
75 selected types (55 classes, 20 datatypes), 952 entries deduplicated into
930 symbols; 110 implemented subsets, 591 missing, 170 legacy unimplemented,
59 restricted/nonscriptable. This is a scoped documentation audit, not an Engine
API dump, engine-build pin, or global compatibility percentage. The existing suite
passes 136 checks. A synthetic backend workload reports 3 available and 16 blocked
entry checks (15 missing entry paths, 1 confirmed RemoteFunction API mismatch).
No actual representative full game exists in the example set.

Confirmed mismatch: RemoteFunction exposes event-only members through C#
RemoteEvent inheritance and NetworkBindings pattern matching. Fix this first.
Then prioritize attributes and their replication, ValueObjects/bindables/tags,
common datatypes/globals/JSON utilities, clock/script/shutdown lifecycle, and an
explicit persistence/backend identity contract. Characters and physics remain
backend needs for games that use them but are not the immediate API milestone.
No engine behavior was changed by this audit.

## Backend API implementation milestone

All 15 missing synthetic audit paths and the RemoteFunction event-member mismatch
are implemented as scoped APIs. All 19 probes now pass. See docs/backend-apis.md.
Added typed attributes and tag metadata, ValueObjects, BindableEvent, immutable
single-precision Color3, local seeded Random, JSON encode/decode, Debris timers,
Heartbeat, durable local stores, yielding shutdown hooks, Teams assignment, health
Humanoid and oriented-box raycasts. Metadata, typed values, health/team state and
Color3 replicate across local clients and TCP. Protocol is now v3; explicit older
versions are rejected. Data stores are server-only, project-local .woorf/data,
file locked and atomically replaced; concurrency/restart tests pass. This is not
Roblox cloud storage or authenticated account persistence. Health, queries, teams,
scheduler and value types retain documented limits; no full-game coverage claim.
The readiness inventory and source manifest were refreshed after implementation.
