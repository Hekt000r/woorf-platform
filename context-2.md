# Woorf continuation context

Updated: 2026-11-16. This file summarizes the completed conversation and supersedes historical next-step recommendations in context.md where they conflict. Read context.md for older architectural history.

## Project and user priorities

Woorf is a platform-agnostic Roblox runtime/Studio alternative written in C#, embedding the official Luau compiler and VM through a native C++ bridge. Development is currently headless, with a CLI and filesystem-based projects. Windows x64 is verified; other platforms, WASM, standalone packaging and an editor remain future work.

The user explicitly wants backend/runtime APIs established before concentrating on rendering or other frontend work. Characters and physics are runtime requirements too, but they are not the immediate priority. The long-term 99% compatibility ambition is not a measured achievement and still needs a defined denominator.

Always use the general-coding-rules and test-driven-development skills for code changes. Check for existing tests, observe relevant failing tests before implementation, then run the complete suite. Follow the writing-good-tests guidance when changing tests. Keep functions at most 70 lines and code files at most 350 lines, follow existing naming and separation of concerns, and avoid speculative abstractions. Do not add a Studio behavior-comparison harness; the user previously rejected that approach.

For implementation tasks, continue until the authorized feature is complete. The user was frustrated when work stopped after merely adding a failing test. Do not stop at a plan or partial implementation. Do not spawn subagents unless explicitly authorized by the user or applicable instructions.

## Implemented foundation

- Official vendored Luau 0.745, native bridge, C# engine and CLI.
- DataModel/Instance hierarchy, lookup, classification, cloning, destruction and property/hierarchy signals.
- Isolated script environments and ModuleScript caches, yielding require, failure caching and cycle diagnostics.
- Cooperative task scheduling, waits, delays, cancellation, yielding callbacks, signal waits and WaitForChild. Script destruction cancels owned work.
- Immutable Vector3, core CFrame, Part transforms/size/anchoring, Part/Model pivots and model bounds.
- Filesystem project loading. General dynamic script activation and Roblox RunContext/Enabled behavior remain incomplete.

## Client/server architecture and transport

Server and clients have separate Luau VMs and C# object graphs. Server state is authoritative; client edits do not write back into it. Remotes copy payloads and translate shared Instance references. Sender identity comes from the endpoint/connection, not a client-supplied player argument.

- RemoteEvents: FireServer, FireClient, FireAllClients, OnServerEvent and OnClientEvent.
- RemoteFunctions: InvokeServer/InvokeClient, invoke callbacks, yielding replies, tuples, errors, concurrent calls, late callback assignment and lifecycle cancellation.
- RemoteFunction public event-only member lookups are now rejected. Internal underscore-prefixed transport hooks are implementation details; C# transport machinery is still shared.
- Players.LocalPlayer, UserId, GetPlayers, PlayerAdded, PlayerRemoving and Kick.
- LocalNetworkHost exposes Start, ConnectClient, DisconnectClient and RunUntilIdle for synchronous embedding. Late joins receive current snapshots; disconnects clean up VMs, replicas, callbacks and pending RPC work.
- TCP server/client processes use protocol v3. Explicit older versions are rejected; use matching builds. Clients receive shared state and client script templates without requiring a local project copy.
- Initial snapshots are followed by additions, property updates and removals, with per-peer baselines and replication ordered before related remote messages.
- Dirty tracking captures initial shared state once, then recaptures affected instances and dependencies. Repeated mutations coalesce; idle ticks avoid full graph scans and captures. Clients apply incremental changes. Automatic pivots and external reference visibility are handled, including ObjectValue/team mappings.
- Nonblocking writes have an 8 MiB outgoing limit per peer and a 256 KiB send budget per peer per tick. Overflow or five seconds without queued-write progress disconnects the affected peer. Partial writes preserve frame offsets; graceful client leave drains outgoing messages.
- SocketTransport.Listen/Connect accept an optional metrics callback for queued/sent/replication bytes, tick duration, peer count, slow disconnects, peak queue size, node captures and full scans.

Typical commands:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/client-server --clients 2
dotnet run --project src/Woorf.Cli --no-restore -- examples/remote-functions --listen 127.0.0.1:7777
dotnet run --project src/Woorf.Cli --no-restore -- --connect 127.0.0.1:7777
```

## Backend API audit and completed follow-up

The user requested an API readiness audit, then explicitly authorized implementing its 15 missing paths and the RemoteFunction mismatch. That implementation is complete for the documented subsets.

The official creator-docs reference is pinned at commit 5a56d0d361d709aa785585b738354134ae47b4ae. The snapshot selects 55 classes and 20 datatypes: 952 entries deduplicated into 930 symbols. The updated inventory classifies 175 implemented subsets, 526 missing, 170 legacy unimplemented and 59 restricted/nonscriptable symbols. These are selected documentation symbols, not a complete Engine API dump, engine-build pin, behavioral conformance measurement or global compatibility percentage.

Added and tested:

1. warn, writing formatted messages to stderr.
2. Attributes: typed storage, get/set/removal, attribute signals, cloning, validation and replication.
3. Instance tags and CollectionService membership queries/signals, including reentrant callbacks, clone and destruction behavior.
4. IntValue, NumberValue, BoolValue, StringValue, ObjectValue, Vector3Value, CFrameValue and Color3Value; typed Value, value-bearing Changed, cloning and replication.
5. BindableEvent with tuple-preserving Fire, subscriptions and yielding listeners.
6. Seeded local Random with cloning, number/integer generation and unit vectors.
7. HttpService JSON encode/decode and validation.
8. Debris cleanup independent of the scheduling Script's destruction.
9. Headless Heartbeat ticks and waits.
10. Server-only local DataStoreService handles with GetAsync, SetAsync, RemoveAsync and IncrementAsync.
11. BindToClose with concurrent/yielding callbacks and bounded shutdown. Unrelated Heartbeat subscriptions do not delay completed callbacks.
12. Teams enumeration, Team:GetPlayers, Player.Team and derived Neutral state.
13. Health-only Humanoid with Health/MaxHealth, TakeDamage, HealthChanged and Died.
14. Oriented-box Workspace raycasts and immutable hit results.
15. Immutable native single-precision Color3, RGB/HSV construction, interpolation and value transport.

Attributes, tags, typed values, health/team state, reference visibility and Color3 work through local and TCP replication.

## Persistence contract and important limits

Projects store data in project-root/.woorf/data; standalone scripts use .woorf/data under the working directory. This directory is ignored by Git. Store name/scope pairs are encoded unambiguously and hashed into filenames. Operations use an exclusive file lock, durable flush and atomic replacement. Tests verify process restart persistence, scope isolation and concurrent increments from separate processes.

This is a synchronous local backend, not Roblox cloud access. UpdateAsync, key/version metadata, quotas, distributed storage and authenticated stable account identity are not implemented. Connection-assigned UserIds are local session identities and cannot be treated as production account identity.

Other material limits:

- No rendering, physics, character rigs, Humanoid movement/state machine, terrain or asset/place import.
- Raycasts cover box Parts, without RaycastParams/filtering, meshes, collision groups or Material results. Unsupported params are rejected.
- Teams lacks automatic balancing, AutoAssignable and TeamColor; Neutral is derived read-only state.
- Random does not promise Roblox-identical seeded sequences.
- JSON accepts supported JSON-compatible values, not arbitrary engine objects or sparse/mixed tables.
- BindableEvent is limited by the supported value codec, rather than arbitrary Lua function/coroutine/custom-userdata transfer.
- Heartbeat is a headless clock, not Roblox simulation-phase fidelity. Active subscriptions keep finite local scheduler runs alive until disconnected.
- Source editing, general dynamic script startup, Enum coverage and many datatype/class members remain incomplete.
- No TLS/account authentication, reconnect protocol, inbound rate limiting, script execution budgets or prolonged load qualification.
- Passing the probes does not establish arbitrary full-game compatibility.

## Verification and repository state

Latest complete suite: 160 PASS checks, with no reported failures or compiler warnings. The readiness workload reports SUMMARY 19 0. git diff --check passes; Git may print a pre-existing LF-to-CRLF normalization notice for native/ValueBridge.cpp.

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore
dotnet run --project src/Woorf.Cli --no-restore -- docs/api-audit/probes/backend-readiness.luau
```

Rebuild native code when changing the bridge, then rebuild C# so the DLL is copied:

```powershell
cmake --build build/native --target woorf_luau
```

There is substantial uncommitted work across these conversation milestones. Inspect git status before editing. Preserve the existing changes; no commit, reset or cleanup of user work was requested. The source manifest records the audited working-tree files, not an engine release. Refresh it and the inventory when their recorded state changes.

## Planned next milestone

The latest recommendation, not yet implemented, is a representative headless game backend. The user has asked for this context summary; they have not yet explicitly instructed implementation of the reference game.

Instead of adding more isolated API names, build a small reference workload exercising:

- Player profiles and persistent currency.
- Inventory backed by attributes, tags and ValueObjects.
- Teams and a Heartbeat-driven round/session loop.
- Client requests through remotes with server-side validation.
- Joining, leaving, restarting and saving during shutdown.

The proposed acceptance test runs separate server/client processes, completes a round, restarts the server and verifies saved progress. Use test identities explicitly; do not imply authenticated accounts exist. Existing examples are feature demos, not a complete representative game.

Let failures in that workload determine the next API priorities. Likely candidates are atomic UpdateAsync, stable player identity, script lifecycle and inbound rate limits. Those are expectations, not an already-authorized commitment or implemented feature list. Avoid silently expanding into rendering or characters while the user is prioritizing backend readiness.

## Key references

- docs/backend-apis.md: current added APIs and precise boundaries.
- docs/backend-api-readiness.md: audit, current inventory counts, remaining readiness gates and roadmap.
- docs/api-audit/roblox-api-snapshot.json: pinned official reference metadata.
- docs/api-audit/member-inventory.json: per-symbol implementation/evidence classification.
- docs/api-audit/woorf-source-manifest.json: working-tree source/test hashes.
- docs/api-audit/probes/backend-readiness.luau and backend-readiness-results.txt: executable probes and recorded output.
- docs/client-server.md and docs/socket-transport.md: runtime/transport semantics and limits.
- docs/runtime-compatibility.md, docs/scheduler.md, docs/project-format.md: broader behavior.
- docs/spatial-types.md, docs/cframe.md, docs/pivots.md, docs/bounds.md: headless spatial scope.
- README.md: build/run commands. project.md: vision and unresolved platform/service-policy decisions.
