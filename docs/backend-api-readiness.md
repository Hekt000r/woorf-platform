# Backend API readiness audit

Audit date: 2026-11-16. Verdict: **ready for constrained headless prototypes;
not ready to run an arbitrary full Roblox game or an untrusted production game.**

The networking foundation is substantially ahead of the gameplay API surface.
Rendering is not the immediate blocker: common server-side APIs are still missing.
The original 15 missing probe paths and RemoteFunction discrepancy are now resolved
by the [backend API milestone](backend-apis.md). Remaining class coverage and
production gates still prevent arbitrary full-game compatibility.

## Reference and counting rules

The reference is the official [Roblox creator-docs repository at
5a56d0d361d709aa785585b738354134ae47b4ae](https://github.com/Roblox/creator-docs/tree/5a56d0d361d709aa785585b738354134ae47b4ae/content/en-us/reference/engine).
This is a pinned **documentation revision**, not a pinned Roblox engine binary or
a complete Engine API dump.

The selected snapshot covers 55 classes and 20 datatypes: 952 declared entries,
deduplicated into 930 member symbols. Inherited members are counted once at their
declaring owner; constructor overloads and repeated property/constant entries
share a symbol. Enum values, datatype operators, globals, standard libraries,
Studio/plugin workflows, and the remainder of the Engine API are outside this
denominator. Selection favors existing runtime types and common backend gaps.

| Classification | Selected unique symbols | Meaning |
| --- | ---: | --- |
| Implemented subset | 190 | A corresponding binding exists; full behavioral/overload compatibility is not established |
| Missing | 511 | No reviewed implementation in this runtime |
| Legacy unimplemented | 170 | Deprecated reference members without reviewed implementation |
| Restricted or nonscriptable | 59 | Reference security/tags place the member outside ordinary script access |

**These numbers are not a Roblox compatibility percentage.** Neither all engine
APIs nor all behavioral cases were tested. Some implemented members also have
deprecated tags; implementation takes precedence in this classification.

Artifacts:

- [Pinned reference metadata](api-audit/roblox-api-snapshot.json): source URLs,
  revision, SHA-256 of each original YAML file, member names, tags, and security.
  Prose descriptions were not copied.
- [Audited woorf source manifest](api-audit/woorf-source-manifest.json): hashes of
  the working-tree engine, CLI, native bridge, and tests. The engine working tree
  contains uncommitted milestone work; this audit does not identify a release.
- [Member inventory](api-audit/member-inventory.json): per-symbol classifications,
  binding paths, and test-file evidence. Evidence is at feature/file level,
  not proof of conformance for every member or overload.
- [Synthetic backend probes](api-audit/probes/backend-readiness.luau).
- [Recorded probe output](api-audit/backend-readiness-results.txt).

The snapshot was extracted from the pinned YAML properties, methods, events,
callbacks, constructors, constants, functions, and operators lists where present.
Existing C#/native/Luau bindings were reviewed manually against the resulting names.
No Studio comparison harness was used.

## What currently works

| Area | Available subset | Important limit |
| --- | --- | --- |
| Luau execution | Official embedded Luau; isolated scripts and ModuleScript caches | Numeric/path requires and broader Roblox environment globals remain incomplete; supported dynamic lifecycle now works |
| Instance graph | Identity, names, parenting, traversal, lookup, clone/destroy, signals | Attributes, tags and typed ValueObjects now work; most subclasses remain absent |
| Scheduler | task spawn/defer/delay/wait/cancel, yielding listeners, signal waits, WaitForChild | Heartbeat works; simulation phases and parallel APIs remain absent; VM execution budgets now apply |
| Spatial math | Vector3 core, CFrame core, Part transforms, model pivots/bounds | Color3, raycasts and custom box physics work; broader Enum, shapes, joints and Part properties remain incomplete |
| Player lifecycle | LocalPlayer, UserId, GetPlayers, join/removal notifications, Kick | TCP credentials preserve local identities across restart; in-process identities remain session-based; external accounts, characters and full profile APIs remain absent |
| Remotes | RemoteEvents and yielding RemoteFunctions, tuple/table/value/reference copying | Documented value restrictions and headless shutdown conventions remain; see networking docs |
| Replication | Initial snapshots, property deltas, dirty capture, visibility transitions | Implemented properties only; no streaming, ownership, asset pipeline, or general-purpose place import |
| Transport | Separate processes over TCP, bounded nonblocking outgoing queues, metrics | VM budgets, inbound limits and credential reconnect work; no TLS/external accounts, automatic state resume, memory quotas or prolonged load qualification |
| Service containers | Workspace and nine other supported service roots | Container availability does not implement the service's full API |

At the original audit baseline, ten creatable classes were admitted by ClassCatalog: Folder, Model,
Configuration, Script, LocalScript, ModuleScript, Part, RemoteEvent, RemoteFunction,
and Player. The original ten recognized service roots were: Workspace, ReplicatedStorage,
ReplicatedFirst, ServerStorage, ServerScriptService, StarterGui, StarterPack,
StarterPlayer, Players, and RunService.

The [headless round game](../examples/round-game/README.md) exercises persistent
test profiles, currency, inventory, teams, a Heartbeat round and validated remotes
together. Its acceptance test runs separate TCP server/client processes, records
leave/shutdown saves, then restarts with reversed connection order and verifies saved totals. It uses fixed test
identities and one round per server session; it is not a port of an existing game.

## Executable readiness results

The complete integration command now passes with **186 PASS checks**:

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore
```

The synthetic backend workload now reports **19 available, 0 blocked** checks:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- docs/api-audit/probes/backend-readiness.luau
```

The original baseline had 3 available and 16 blocked entry checks. All requested
missing entry paths now work as documented subsets: warn, attributes, tags,
ValueObjects, BindableEvent, Random, JSON, Debris, Heartbeat, local persistence,
shutdown registration, teams, health-only Humanoid, box raycasts and Color3.
The RemoteFunction event-member discrepancy is also fixed.

Probes deliberately continue after each failure and exit successfully after
printing a summary; BLOCKED lines are findings, not a green readiness result.
An AVAILABLE result is limited to its assertions. For example, obtaining a
data-store handle alone would not prove durability. The workload is synthetic and is
not a port of an actual game or a coverage denominator.

## Confirmed discrepancies and compatibility risks

1. **Resolved: RemoteFunction event-member leakage.** RemoteFunction derives from
   RemoteEvent in C#, and NetworkBindings applies its RemoteEvent cases to it.
   Luau bindings now reject looking up event-only members on RemoteFunction. The pinned reference
   declares InvokeServer/InvokeClient and invoke callbacks on RemoteFunction.
   The executable probe and dedicated tests verify rejection of event-only public
   members; internal transport hooks are reserved implementation details.
2. **Dynamic script lifecycle is a defined subset.** ClientStartup copies
   StarterPlayerScripts and StarterGui templates. Loaded scripts and clones now
   start/stop/restart at host boundaries with Enabled/Disabled and Legacy/Server/Client
   RunContext behavior. Character/Backpack, plugin execution and full Roblox startup
   fidelity remain unsupported; see [lifecycle contract](runtime-hardening.md#script-lifecycle).
3. **Names are not behavioral compatibility.** RunService context methods
   and Heartbeat work; full simulation phases remain absent. StarterPack is a container, not an
   inventory implementation. PlayerScripts/PlayerGui roots do not imply GUI,
   Backpack, Tool, character, or input APIs.
4. **Deliberate scheduling differences remain.** Module cycles fail fast, ordering
   follows the documented local scheduler, and idle waits alone do not keep the
   in-process CLI alive. Socket clients/listeners have different lifetime rules.
5. **Production limits remain explicit.** VM interrupts stop runaway Luau code;
   inbound frame/byte limits isolate flooding peers. UpdateAsync provides atomic
   local profile transactions, and saved TCP credentials preserve identity across
   reconnects. Synchronous host I/O, memory quotas, distributed session ownership,
   external accounts and TLS remain limits; see [runtime hardening](runtime-hardening.md).
6. **Delivery is not yet qualified.** Windows x64 is the verified path; WASM,
   Linux/macOS/mobile, packaging, assets, and .rbxl/.rbxm ingestion are not proven
   full-game delivery paths.

Evidence: [bindings](../src/Woorf.Engine/Scripting/NetworkBindings.cs),
[RemoteFunction](../src/Woorf.Engine/DataModel/RemoteFunction.cs),
[client startup](../src/Woorf.Engine/Networking/ClientStartup.cs),
[class catalog](../src/Woorf.Engine/DataModel/ClassCatalog.cs),
[scheduler conventions](scheduler.md), and [socket limits](socket-transport.md).

The originally listed ten creatable classes and ten service roots were the audit
baseline; the current class catalog also includes the backend milestone classes
and services. See [current backend APIs](backend-apis.md).

## Prioritized backend roadmap

| Order | Milestone | Acceptance gate |
| --- | --- | --- |
| 0 | Completed: reviewed RemoteFunction leakage fix | Event-only member rejection and remote/lifecycle tests pass |
| 1 | Completed subset: attributes and replication | Set/Get/GetAttributes, removal, type validation, change events, clone, late joins, and incremental replication |
| 2 | Values, tags and BindableEvent added; BindableFunction remains | Correct Value/Changed payloads, reference cloning, yielding calls, tag membership and lifecycle events |
| 3 | Remaining utilities, Enum and datatype coverage | warn, Color3, Random, missing Vector3/CFrame helpers, prioritized Enum values, JSON utilities, Debris; representative server modules execute without placeholder services |
| 4 | Heartbeat/shutdown and dynamic lifecycle subsets complete | Enabled/Disabled/context startup, cancellation and restart pass; simulation fidelity remains separate |
| 5 | Atomic local stores and credential identity complete as local subsets | Composite UpdateAsync concurrency and rollback, saved credentials, reordered restart, forged and duplicate rejection pass; external accounts/distributed storage remain separate |
| 6 | Completed bounded workload: headless round game | Separate-process round, request validation, leave/shutdown saves and restart persistence pass with explicit test identities; in-session reconnect remains outside the demo |

Execution budgets and inbound traffic limits are implemented and tested as bounded
contracts. External account authentication/TLS, memory quotas and sustained load
qualification remain production-readiness gates.
Implement persistence against a deliberate woorf backend; do not imply direct
access to Roblox-hosted account, economy, moderation, or data services.

Character lifecycle, Humanoid, physics, raycasts/overlap, teams/tools and asset
ingestion are also runtime/backend requirements for games that use them. Their
order should follow the chosen reference game's server dependencies, independently
of the editor or rendering schedule.

## What would justify saying a game is supported?

Choose an actual game's scripts, modules, assets, and server/service dependencies.
For that named workload, require:

- No unreviewed missing API or placeholder service along exercised code paths.
- Expected observable results across unit, integration and complete session flows.
- Durable player data and correct identity across restart/reconnect where required.
- Script and network resource limits appropriate for the trust model.
- Multiplayer join, leave, shutdown, persistence failure and sustained traffic tests.
- A verified import/build/run path for the intended platform.

The 99% goal still needs a defined scope for runtime versus Studio/plugin APIs,
deprecated behavior, hosted services, and overload/semantic coverage. This audit
provides a reproducible baseline and implementation order, not certification.
