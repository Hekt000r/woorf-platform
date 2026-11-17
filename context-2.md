# Woorf continuation context

## Latest milestone: playable graphical character (2026-11-16)

The user explicitly authorized a moving character integrated with graphics and
recommended subagents. Three agents implemented physics control, lifecycle and
graphical input/posing; root integrated scripting, replication and acceptance tests.
The user later said computer use does not work and asked to own hands-on verification.
Do not retry computer-use verification for this task; automated checks are complete.

- Run `dotnet run --project src/Woorf.Cli --no-restore -- examples/character-playground --play`.
  WASD walks camera-relative, Space requests one grounded jump per press, arrows orbit,
  R kills/resets, Escape closes. The follow camera snaps to a respawned root.
- Woorf.Engine/Characters contains CharacterWorld and CharacterController. The character
  is a Model with Humanoid and a 2x4x2 upright HumanoidRootPart; PrimaryPart is the root.
  Visual torso/head/limbs/eyes are centrally generated RenderItems, not extra physics
  bodies or per-instance rendering. Procedural stride follows actual travel.
- Player.Character (read-only in Luau), CharacterAdded/Removing and server-side
  LoadCharacter are bound. Humanoid:Move(direction, false/nil), WalkSpeed16, JumpPower50,
  MoveDirection and Jump are supported. Movement is world-horizontal and capped to one.
- Players.CharacterAutoLoads is deliberately false by default to preserve headless
  workloads; --play enables it. RespawnTime defaults3. CharacterSpawn Workspace
  Vector3 attribute defaults(0,6,0), with four-stud X offsets by UserId modulo4.
  Falling below Y=-100 kills; death/missing components respawn after the configured
  delay. Leave/reload/disposal clean up the model and controller hooks.
- Controller runs on live 120 Hz body hooks, locks root rotation, uses normal.Y>=0.7
  support, clips inward horizontal wall/character movement, preserves airborne Y,
  has full XZ air control and rejects air jump buffering. No step climbing, assemblies,
  moving-platform attachment or full Humanoid state machine. +Y up/downward gravity
  are assumed for the first controller. See docs/characters.md for boundaries.
- TCP protocol is now v7; explicit older versions are rejected. Character references
  and movement/service state replicate locally and over TCP. References publish after
  the replicated model graph is initialized and obey shared visibility. LocalScripts
  in the owning Player.Character now run and stop with character lifecycle. The
  graphical viewer is still one authoritative local player, not a graphical TCP client.
- Full regression: 232 PASS, zero FAIL/compiler warnings. --graphics now has three
  real-GPU aggregate checks, including --play capture with eight visual items and two
  draws. A 1800-frame idle playground soak measured median17.025 ms, p95 17.236 ms,
  simulation p95 0.874 ms and two draw calls on Intel UHD OpenGL3.3. This is a workload
  measurement, not arbitrary-game qualification. User owns hands-on input testing.
- CharacterNetworkTests runs two separate TCP clients that walk/collide/jump/die/respawn
  using validated application remotes. It also checks owning-character LocalScripts.
  Controller regressions reproduced two-player wall penetration (8.602 vs8.5 bound),
  fixed by contact clipping. Lifecycle tests reproduced a synchronous Died callback
  destroying the player before auto-respawn; synchronization now safely rechecks it.
- Targeted test flags: --character-controller, --character-lifecycle, --character-view,
  --character-input, --character-replication, --character-network, --character-graphics.
  .local/character-full-suite.txt and character-performance.csv hold diagnostics only.
- The API inventory is 206 implemented subsets, 496 missing, 169 legacy-unimplemented,
  59 restricted/non-scriptable across the unchanged930-symbol selected denominator.
  This is still not a global compatibility percentage. Eleven selected character symbols
  changed classification; see inventory qualifications and current source hashes.
- The user committed the earlier graphics work as 84dbc21 while this work was underway.
  Character changes remain uncommitted. No commit/reset/cleanup was requested. Preserve
  all current work. See docs/characters.md and examples/character-playground/README.md.

## Latest follow-up: viewer performance fixes (2026-11-16)

The user reported severe lag while moving/orbiting the camera and requested engine
performance fixes with rendering owned centrally by the engine. This is implemented;
read docs/graphics-performance.md for the reproduced cause and measured limits.

- Baseline profiling found simulation, not GPU submission, causing multi-second
  frames. Fast bodies falling far below the scene forced global collision substeps,
  then slow frames caused more fixed-step catch-up. CollisionSubsteps now considers
  swept potential pairs and relative motion; distant unrelated velocity and common
  falling motion no longer impose extra slices. Gravity reversal has a ceiling test.
  The fixed 120 Hz steps, 64-slice ceiling and qualified high-speed tests remain.
- Rendering remains centralized: WorkspaceScene is now a dirty cache, not a full
  per-frame Workspace traversal; unchanged captures allocate nothing. OpenGlRenderer
  batches primitives into at most two real instanced draws using shared meshes,
  cached uniform locations and reusable instance data. Normal transforms are cached
  on the CPU instead of inverted per vertex. Instances own no renderer/GPU resources.
- FramePacer uses a high-resolution Windows waitable timer to avoid coarse sleeps
  stretching otherwise fast frames to 31 ms. It requires Windows 10 1803+.
- --profile report.csv records simulation/collection/render/present/frame timings,
  per-frame managed allocation, Part count and actual draw calls. Use --frames N;
  rows are buffered and saved on normal exit. Benchmark separate output copies to
  avoid locking CLI DLLs while rebuilding. Hidden-window benchmarks need GUI access.
- The initial 763-frame stalled run took ~473 seconds, with frame p95 ~2054 ms.
  The optimized 3600-frame soak took ~one minute: median frame 17.023 ms, p95
  17.217 ms, maximum 48.474 ms. Simulation p95 was 7.221 ms and draw count was 2.
  These are same-machine workload measurements, not a universal FPS guarantee.
- Default regression count is now 210; optional --graphics still reports two
  aggregate real-GPU checks, expanded for batching, moved-camera depth and pacing.
  --graphics-scene and --physics-isolation are targeted CPU regressions.
- TDD observed failures for profiling, uncached scene collection, missing batching,
  distant-body integration coupling, gravity reversal and missing precise pacing
  before their corresponding fixes. All earlier uncommitted graphics work is preserved.

## Latest milestone: first OpenGL graphical output (2026-11-16)

The user explicitly authorized a custom renderer using OpenGL with no external
packages, superseding the earlier statement that rendering had not started.
The first implementation is now present in Woorf.Graphics and CLI --render mode.
Read docs/graphics.md for architecture, commands, verification and boundaries.

- Woorf.Graphics is independent of Woorf.Engine and uses System.Numerics plus
  direct OpenGL/OS interop. The first window/context host is Win32/WGL and requires
  an OpenGL 3.3 core driver; other graphics hosts are not implemented.
- Indexed box/sphere meshes, GLSL directional/ambient lighting, depth/cull state,
  inverse-transpose normals, perspective camera, resized viewport, frame capture,
  creating-thread access checks and GPU resource disposal are implemented.
- CLI RenderCli owns the window/renderer and existing LuauSession on one thread.
  Physics and scripts advance before WorkspaceScene captures geometry. Existing
  headless commands and networking retain their behavior; rendering is currently
  standalone/project server visualization, not a graphical TCP client.
- The viewer uses an explicit diagnostic palette, not Part.Color. No Roblox
  Camera/input/Lighting APIs, shadows, textures, GUI, asset meshes or animation
  were added. Character lifecycle and movement remain unimplemented.
- Run: dotnet run --project src/Woorf.Cli --no-restore -- examples/graphics-showcase --render
  WASD/QE moves, arrows orbit, Shift speeds up, Escape closes.
- TDD observed missing --render/mesh failures and a singular-transform failure
  before implementing their behavior. The complete default suite passed 208 checks;
  --graphics passes two additional actual-GPU checks for depth order, box/sphere
  silhouettes/sizing, deletion and padded framebuffer capture. Visual inspection
  of .local/graphics-preview.bmp confirmed the showcase on Intel UHD OpenGL 3.3.
- --graphics-math is a targeted CPU check. --graphics opens hidden windows and
  requires an interactive Windows desktop/driver. It is intentionally separate
  from the headless default suite. No external graphics packages were added.
- This session began with a clean working tree at 7b162d5; the earlier physics
  work had been committed by the user. Graphics work is currently uncommitted.
  Preserve the user's work; no commit/reset/cleanup has been requested.

The earlier sections below retain physics/backend history. Statements there that
rendering has not started and the 207-check baseline predate this graphics work.

Updated: 2026-11-16, after completing physics foundations for characters and discussing game readiness. This is the primary continuation file; context.md is only an index.

## Resume here

- The user asked whether physics was ready for characters, then authorized making the physics engine ready for that phase, explicitly including spheres. That implementation is complete. The latest full suite passed 207 checks with no failures/compiler warnings, probes returned SUMMARY 19 0, and local/TCP two-client physics scenes passed.
- The user then asked whether physics was ready to support games. The answer was: ready for simple games using boxes/spheres and for building a basic single-collider character controller, not a general-purpose engine for arbitrary Roblox games. Validate each game's concrete needs; passing tests does not qualify complex stacks, heavy sustained scenes, every shape or Roblox-identical behavior.
- The next recommendation remains character spawning/lifecycle and movement: Player.Character, spawn/death/respawn, a collider, walking/jumping/grounding/wall collisions, and server-authoritative replicated movement. A proposed acceptance scene uses two headless players that walk, collide, jump, die and respawn.
- Character lifecycle/movement has not been implemented or separately authorized after the physics work. The latest request is this context save. Follow the user's next instruction; do not treat saving context as permission to start characters or invent another broad hardening milestone.
- No renderer, camera, input, animation or character implementation has started. Boxes and spheres work; capsules, welded rigs, joints and Humanoid movement do not exist.
- The working tree currently contains modified and untracked physics implementation, tests, docs and example files. No commit/reset/cleanup was requested. Preserve them and inspect git status before further work. context-2.md already had changes when this session began; these have been retained and consolidated.

## Completed physics character foundations

Physics now supports Block/Ball Part.Shape, sphere/box and sphere/sphere contacts,
sphere mass/inertia, shape-aware Raycast and Spherecast, and RaycastParams with
FilterDescendantsInstances, FilterType and RespectCanCollide. Spheres use half the
smallest Size dimension as radius. Other shapes and parameter fields are rejected.
Adaptive collision substeps (up to 64 per 120 Hz step) reduce tunnelling; this is
not general CCD. Qualified fixtures include 2-stud bodies at 1200 studs/second
against a 0.05-stud wall. Extreme motion/scales remain unqualified.

Managed PhysicsWorld integration now provides BeforeStep, GetBody,
SetRotationLocked and GetContacts for a future single-collider controller.
Contacts include other Part, point, outward normal and signed separation, with
cleanup after separation, removal and callback teleports. Hooks operate on live
solver bodies per fixed step; script queries see published Part state. See
docs/physics.md for the contract and remaining controller responsibilities.

TCP protocol is now v6; explicit older versions are rejected. The physics-boxes
example drops both a box and sphere, tests live shape changes, and passes with
two local clients and two separate TCP clients. PhysicsFoundationTests adds
controlled-clock upright/contact, wall/corner, jump/ceiling/landing, slope,
high-speed, callback/removal and late-join checks. Run just those with
`dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --physics-foundation`.

The full suite baseline is 207 PASS checks; backend probes remain SUMMARY 19 0.
The selected API inventory is now 195 implemented subsets, 506 missing, 170
legacy-unimplemented and 59 restricted/nonscriptable; the denominator is unchanged.
Source hashes and docs have been refreshed. The final source-manifest verification
reported zero hash mismatches across 161 files. git diff --check passed; Git may
emit LF-to-CRLF normalization notices, which are not compiler warnings.

### Exact contracts and boundaries

- Part.Shape defaults to Enum.PartType.Block and accepts Ball. Cylinder/Wedge/CornerWedge enum items are recognized but assignment is rejected; arbitrary numbers and enum items from other namespaces are rejected. Sphere radius uses half the minimum Size dimension, and spheres remain round under rotation. This sizing rule is documented as Woorf's subset, not ellipsoid or complete Roblox resizing compatibility.
- Sphere mass is density times sphere volume; inertia is 2/5 mass times radius squared. Sphere/sphere collision uses center distance; sphere/box uses the closest point on the oriented box and a nearest-face escape for an embedded center. Box SAT/manifolds remain in use for box/box.
- Collision subdivision considers linear travel, angular surface travel and minimum dynamic-body dimension. It targets at most a quarter dimension of travel per slice, with a maximum of 64 slices per 120 Hz step. It is adaptive discrete collision detection, not general time-of-impact CCD; extreme speeds/scales can still tunnel. Controller hooks run once per fixed step, not once per collision slice.
- RaycastParams.new supports FilterDescendantsInstances, FilterType (Exclude by default or Include), and RespectCanCollide (false by default). Filters include the Instance and its descendants. Arrays are copied on assignment/read; unsupported parameter fields are rejected. With RespectCanCollide=false, noncollidable Parts remain queryable. CanQuery, water, collision groups and Material results are absent.
- Spherecast uses rounded box faces/edges/corners and spheres, returns immutable RaycastResult (Instance, surface Position, outward Normal, travel Distance), and ignores initial overlaps. Radius must be positive and at most 256; travel is limited to 1024 studs. Queries ignore invalid legacy geometry, including NaN sizes. Initial overlap handling is a separate controller responsibility.
- PhysicsWorld.BeforeStep, GetBody, SetRotationLocked and GetContacts are internal managed engine integration APIs, not new Roblox scripting members. GetBody exposes live solver state. Rotation locking disables angular integration/inverse inertia and preserves the current orientation; it does not automatically turn a tilted body upright. Unlocking restores rotation.
- GetContacts returns the previous completed step's contacts during a hook. Input changes invalidate stale support; callback teleports are preserved and invalidate contact data immediately. Destroying a collider from a hook excludes it from that step. Part publication still occurs once per host advance. Script queries use published Part state, so substep controller logic must use live bodies/contact data where appropriate.
- Step climbing, slope limits, air control, moving-platform attachment, character state and multiplayer character lifecycle remain controller/character work. The tests exercise a collider through these basic physical situations; they do not implement a Player character or Humanoid controller.

### Verification history and continuation aids

The first expanded full-suite run failed only the newly added protocol-v5 rejection
test while the implementation was still v5. Transport was then moved to v6. A
callback-teleport test reproduced stale support contacts and was fixed. Final
review reproduced a NaN-size Part masking the floor in a raycast; the query filter
was fixed and the complete suite rerun successfully. No unresolved test failure
remains. A turn was externally interrupted during implementation; the user asked
why work stopped, and work resumed to completion. Keep providing progress updates
and do not stop at tests or a partial implementation.

Latest full-suite output, if retained: .local/physics-foundation-final.txt (207 PASS,
zero FAIL/compiler warnings, process exit 0). Earlier output files may show the
expected red tests and are not the final baseline. .local/refresh-physics-audit.py
was a temporary ignored metadata-refresh helper, not a required build dependency.
No new package, platform scaffold or Studio comparison harness was introduced.


## Project and user priorities

Woorf is a platform-agnostic Roblox runtime/Studio alternative written in C#, embedding the official Luau compiler and VM through a native C++ bridge. Development is currently headless, with a CLI and filesystem-based projects. Windows x64 is verified; other platforms, WASM, standalone packaging and an editor remain future work.

The user explicitly wants backend/runtime APIs established before concentrating on rendering or other frontend work. They authorized basic physics and the completed sphere/character-foundation extension, explicitly preferring a custom solver over a third-party dependency. Keep it platform-neutral C# and target Roblox behavior closely; do not perform per-platform restore/build checks. The long-term 99% compatibility ambition is not a measured achievement and still needs a defined denominator.

Always use the general-coding-rules and test-driven-development skills for code changes. Check for existing tests, observe relevant failing tests before implementation, then run the complete suite. Follow the writing-good-tests guidance when changing tests. Keep functions at most 70 lines and code files at most 350 lines, follow existing naming and separation of concerns, and avoid speculative abstractions. Do not add a Studio behavior-comparison harness; the user previously rejected that approach.

For implementation tasks, continue until the authorized feature is complete. The user was frustrated when work stopped after merely adding a failing test. Do not stop at a plan or partial implementation. Do not spawn subagents unless explicitly authorized by the user or applicable instructions.

## Implemented foundation

- Official vendored Luau 0.745, native bridge, C# engine and CLI.
- DataModel/Instance hierarchy, lookup, classification, cloning, destruction and property/hierarchy signals.
- Isolated script environments and ModuleScript caches, yielding require, failure caching and cycle diagnostics.
- Cooperative task scheduling, waits, delays, cancellation, yielding callbacks, signal waits and WaitForChild. Script destruction cancels owned work.
- Immutable Vector3, core CFrame, Part transforms/size/anchoring, Part/Model pivots and model bounds.
- Filesystem projects and dynamic loaded-script/clone activation, Enabled/Disabled and Legacy/Server/Client RunContext subsets. See docs/runtime-hardening.md for locations, cancellation and restart behavior.

## Client/server architecture and transport

Server and clients have separate Luau VMs and C# object graphs. Server state is authoritative; client edits do not write back into it. Remotes copy payloads and translate shared Instance references. Sender identity comes from the endpoint/connection, not a client-supplied player argument.

- RemoteEvents: FireServer, FireClient, FireAllClients, OnServerEvent and OnClientEvent.
- RemoteFunctions: InvokeServer/InvokeClient, invoke callbacks, yielding replies, tuples, errors, concurrent calls, late callback assignment and lifecycle cancellation.
- RemoteFunction public event-only member lookups are now rejected. Internal underscore-prefixed transport hooks are implementation details; C# transport machinery is still shared.
- Players.LocalPlayer, UserId, GetPlayers, PlayerAdded, PlayerRemoving and Kick.
- LocalNetworkHost exposes Start, ConnectClient, DisconnectClient and RunUntilIdle for synchronous embedding. Late joins receive current snapshots; disconnects clean up VMs, replicas, callbacks and pending RPC work.
- TCP server/client processes use protocol v6. Explicit older versions are rejected; use matching builds. Clients receive shared state and client script templates without requiring a local project copy.
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

The official creator-docs reference is pinned at commit 5a56d0d361d709aa785585b738354134ae47b4ae. The snapshot selects 55 classes and 20 datatypes: 952 entries deduplicated into 930 symbols. The updated inventory classifies 195 implemented subsets, 506 missing, 170 legacy unimplemented and 59 restricted/nonscriptable symbols. These are selected documentation symbols, not a complete Engine API dump, engine-build pin, behavioral conformance measurement or global compatibility percentage. The latest five classifications cover Part.Shape and the four supported RaycastParams constructor/properties; Spherecast is outside this selected symbol set.

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
10. Server-only local DataStoreService handles with GetAsync, SetAsync, RemoveAsync, IncrementAsync and atomic UpdateAsync.
11. BindToClose with concurrent/yielding callbacks and bounded shutdown. Unrelated Heartbeat subscriptions do not delay completed callbacks.
12. Teams enumeration, Team:GetPlayers, Player.Team and derived Neutral state.
13. Health-only Humanoid with Health/MaxHealth, TakeDamage, HealthChanged and Died.
14. Workspace raycasts and immutable hit results, now extended with sphere geometry, filtered queries and Spherecast.
15. Immutable native single-precision Color3, RGB/HSV construction, interpolation and value transport.

Attributes, tags, typed values, health/team state, reference visibility and Color3 work through local and TCP replication.

## Persistence contract and important limits

Projects store data in project-root/.woorf/data; standalone scripts use .woorf/data under the working directory. This directory is ignored by Git. Store name/scope pairs are encoded unambiguously and hashed into filenames. Operations use an exclusive file lock, durable flush and atomic replacement. Tests verify process restart persistence, scope isolation and concurrent increments from separate processes.

This is a synchronous local backend, not Roblox cloud access. UpdateAsync holds the file lock across a non-yielding transform and durable commit; nil cancels and failures roll back. Key/version metadata, cloud quotas and distributed storage remain absent. Saved server-issued TCP credentials recover stable UserIds; external account login and TLS are not implemented. In-process identities remain session-based.

Other material limits:

- No rendering, character rigs, Humanoid movement/state machine, terrain or asset/place import. Physics covers independent boxes and spheres; joints/assemblies and other shapes remain absent.
- Raycast/Spherecast cover boxes and spheres with the documented RaycastParams subset. Meshes, collision groups, CanQuery, water and Material results remain absent. Unsupported parameter fields are rejected.
- Teams lacks automatic balancing, AutoAssignable and TeamColor; Neutral is derived read-only state.
- Random does not promise Roblox-identical seeded sequences.
- JSON accepts supported JSON-compatible values, not arbitrary engine objects or sparse/mixed tables.
- BindableEvent is limited by the supported value codec, rather than arbitrary Lua function/coroutine/custom-userdata transfer.
- Heartbeat is a headless clock, not Roblox simulation-phase fidelity. Active subscriptions keep finite local scheduler runs alive until disconnected.
- Source editing, broader Enum coverage beyond RunContext/PartType/RaycastFilterType subsets and many datatype/class members remain incomplete. Dynamic script lifecycle works in documented execution locations.
- No TLS/external account authentication, automatic network state resume, memory quotas or prolonged load qualification. Protocol v6 supports credential reconnect, inbound frame/byte limits and VM execution budgets.
- Passing the probes does not establish arbitrary full-game compatibility.

## Verification and repository state

Latest complete suite: 207 PASS checks, with no reported failures or compiler warnings. The readiness workload reports SUMMARY 19 0. git diff --check passes; Git may print LF-to-CRLF normalization notices for working-tree files.

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore
dotnet run --project src/Woorf.Cli --no-restore -- docs/api-audit/probes/backend-readiness.luau
```

Rebuild native code when changing the bridge, then rebuild C# so the DLL is copied:

```powershell
cmake --build build/native --target woorf_luau
```

The current physics extension is present as modified and untracked working-tree files; no commit was made in this session. Always inspect git status before editing and preserve existing changes. No commit, reset or cleanup was requested. The source manifest records the audited working-tree files, not an engine release. Refresh it and the inventory when their recorded state changes.

## Representative headless game milestone

The user authorized implementation after the documentation refresh. The workload
is now implemented in examples/round-game; read its README for manual commands
and boundaries. Profiles, currency, tagged inventory/ValueObjects, teams, a
Heartbeat round, validated requests, player leave and yielding shutdown hooks run
together. The finishing milestone then added dynamic lifecycle, VM budgets, inbound
limits, atomic UpdateAsync and persistent credential identity.

ReferenceGameTests copies the real project into a temporary directory and runs a
separate server process through SocketTransport.Listen plus two CLI clients. It
checks invalid/duplicate requests, replicated state, a leave save, a shutdown save
while another client remains connected, and totals after server restart: 7 coins,
1 round, 1 potion, then 14 coins, 2 rounds, 2 potions after reversed reconnect order. Test-only host cancellation
uses redirected stdin; production CLI cancellation behavior is unchanged; --identity-file is now available for saved credentials.

The game now keys profiles by stable server-issued UserId, uses atomic reward and
purchase transactions, and records leave/shutdown metadata without stale balance
writes. Its test reuses separate credential files and reverses client connection
order after restart. There is one round per server session; the demo does not
implement automatic reconnect/state resume. Reward/purchase writes are durable
immediately, rather than depending on a final whole-profile snapshot.

## Runtime finishing milestone

All five identified gaps are addressed as documented local subsets. Read
docs/runtime-hardening.md before changing these contracts. Tests include infinite
startup/task/signal/protected/shutdown loops; dynamic clone activation and cleanup,
RunContext and fatal dynamic errors; request/byte floods and healthy peers;
UpdateAsync rollback, no-yield cleanup and separate-process composite updates;
credential restart/reconnect, reversed join order and forged/duplicate rejection.
A budget breach terminates the VM; it is not per-script isolation. Host I/O remains
synchronous. Credentials are bearer secrets without TLS or external account login.
Basic physics and its character-foundation extension were subsequently authorized and completed; the next proposed character milestone is described in Resume here. Inspect git status rather than assuming whether milestone changes are committed.

## Basic physics milestone

The user requested a basic physics engine, then stopped the initial BEPU library
choice because platform suitability and Roblox behavior had not been established.
They strongly prefer a custom solver unless an external alternative is highly
customizable and proven suitable. BEPU references, package lock and NuGet source
changes were removed. No physics package is referenced. Downloaded build caches
under ignored .local may remain; they are not project dependencies.

The solver is scalar managed C#, with no external packages, unsafe code or
architecture-specific intrinsics. A temporary standalone portability project was
used during development and then removed at the user's explicit request. Do not
recreate it or individually restore/build for x86/x64/ARM/WASM. The user explicitly
said normal dependency-free C# does not need that testing effort. Focus on actual
physics behavior, regression tests and integration. The existing native Luau bridge
still has its separate platform/build concerns; no complete WASM host was built.

### Implemented behavior

- Independent oriented-box and spherical Parts, anchored/static bodies and dynamic/dynamic contact. See the latest foundation section for sphere/query/controller details.
- Fixed 120 Hz semi-implicit gravity and linear integration; world-space angular
  velocity rotates CFrame using axis-angle math and orthonormalization.
- Sweep along world X for broadphase, 15-axis SAT for box overlap, clipped face
  manifolds and closest-edge contacts.
- 24 sequential impulse iterations per step, normal impulses, Coulomb friction,
  weighted restitution and density-derived mass/box inertia. Penetration correction
  uses 0.002-stud slop and 0.8 correction factor; contact skin is 0.004 studs.
- Low-speed restitution is suppressed relative to the gravity step to avoid resting
  bounce. Zero total material weights use an arithmetic mean. These are explicit
  Woorf solver choices, not proof of Roblox solver parity.
- Workspace.Gravity defaults to 196.2 and accepts finite signed values. Parts expose
  AssemblyLinearVelocity, AssemblyAngularVelocity, CanCollide (default true) and
  nullable CustomPhysicalProperties.
- Immutable native PhysicalProperties supports numeric three/five-argument
  constructors, five fields, equality and documented clamps. Default coefficients
  are Plastic-like: density 0.7, friction 0.3, elasticity 0.5, weights 1. Material-enum
  and six-argument/acoustic constructors are explicitly unsupported, not ignored.
- Cloning and local/TCP replication carry gravity, velocities, collision/material
  state. PhysicalProperties also passes through supported remote values. It is not
  an attribute/JSON type. The native userdata tag is 6 and the value-codec kind is 13.
- Server-only simulation. Clients receive authoritative state and do not simulate
  their replicas or write changes back to the server.

### Runtime integration and fixes

PhysicsWorld is owned by server/standalone LuauSession and advances before Heartbeat
and scheduler continuations. It registers Workspace Parts, refreshes input state,
accumulates fixed steps and publishes effective state once per host step. Cached
membership is refreshed on hierarchy mutations; stable IDs follow descendant order.
A maximum of 0.25 seconds is admitted from one elapsed interval; longer stalls drop
excess time. Invalid legacy spatial transforms are excluded from simulation.

Physics alone does not keep a finite CLI script alive. Dynamic bodies request
fixed-step wakeups while other timers are pending; active Heartbeat subscriptions
and TCP hosts keep their established lifetimes. Simulation follows the injected
scheduler clock. Minimum sleeps are clamped to one TimeSpan tick to prevent virtual
clocks hanging when a floating deadline rounds to a zero-duration sleep.

Readback stores original Part state and skips overwriting a Part changed by another
Part's synchronous callback during publishing. A failing test reproduced and fixed
callback teleports being overwritten. ApplyPhysicsState updates state before signals.
Existing networking fixtures that expected static geometry were explicitly anchored;
do not remove those anchors or disable physics to satisfy their replication assertions.

Workspace is now a concrete Model-derived class with Gravity. Physics state uses
ReplicatedPhysicsState/WirePhysicsState appended to ReplicaState/WireNode. TCP is
protocol v6; explicitly older versions are rejected. Client and server builds must match.

### Physics source map

- src/Woorf.Engine/Physics/RigidBody.cs: state, inertia, velocity and orientation integration.
- src/Woorf.Engine/Physics/BoxCollision.cs: SAT, face clipping and edge contacts.
- src/Woorf.Engine/Physics/ShapeCollision.cs: box/sphere and sphere/sphere collision dispatch.
- src/Woorf.Engine/Physics/ShapeQueries.cs: ray/sphere and swept-sphere/rounded-box geometry.
- src/Woorf.Engine/Physics/PhysicsContact.cs: managed contact result.
- src/Woorf.Engine/Physics/ContactConstraint.cs: normal/friction impulses and material mixing.
- src/Woorf.Engine/Physics/PhysicsSolver.cs: broadphase, contact iterations and correction.
- src/Woorf.Engine/Physics/PhysicsWorld.cs: DataModel/time adapter and guarded readback.
- src/Woorf.Engine/DataModel/BasePart.Physics.cs and Workspace.cs: state/property APIs.
- src/Woorf.Engine/DataModel/Part.cs: PartType and validated Shape.
- src/Woorf.Engine/Scripting/HeadlessRaycast.cs, HeadlessSpherecast.cs and QueryFilter.cs: filtered queries.
- src/Woorf.Engine/Scripting/BackendTypes.luau: RaycastParams and immutable query-result wrappers.
- src/Woorf.Engine/Scripting/ScriptLifecycle.luau and native/InstanceBridge.cpp: PartType/RaycastFilterType enums and Shape routing (existing RunContext handling retained).
- src/Woorf.Engine/Datatypes/PhysicalProperties.cs: managed immutable material value.
- src/Woorf.Engine/Scripting/PhysicsBindings.cs and LuaValueCodec.cs: scripting access.
- native/PhysicalPropertiesBridge.cpp: immutable Luau datatype and constructors.
- src/Woorf.Engine/Networking/ReplicatedPhysicsState.cs: physics snapshot/wire conversion.
- tests/Woorf.Cli.Tests/PhysicsCases.cs: 13 Luau physics cases, split into short case groups.
- tests/Woorf.Cli.Tests/PhysicsClockTests.cs: analytical free fall and timer-partition independence.
- tests/Woorf.Cli.Tests/PhysicsNetworkTests.cs: actual example across TCP processes.
- tests/Woorf.Cli.Tests/PhysicsShapeCases.cs: 9 Luau sphere/query cases.
- tests/Woorf.Cli.Tests/PhysicsFoundationTests.cs: 12 direct managed foundation checks; Woorf.Engine.csproj grants test assembly access to internals.

### Physics workload and limits

examples/physics-boxes waits for two clients, drops a 2-stud box and sphere from Y=10 onto a
floor whose top is Y=0, uses gravity 50 and zero elasticity, and verifies final center
Y approximately 1. Both clients see falling/settled state, velocity and material;
the sphere starts as a Block and changes to Ball when the bodies are released.
Completion uses a server acknowledgement before clients Kick themselves: leaving
immediately after FireServer can discard the last queued message in local hosting.
Tests copy this actual project to a fresh temp directory and use two CLI clients.

Verified behaviors include floors, walls, rotated ramps, angular motion, dynamic
collisions, friction, elastic bounce, density-based momentum transfer, signed
gravity, anchoring/filter changes, material validation and callback edits. The
complete suite passes 207 checks, readiness remains 19/0 and git diff --check passes.

Current limits: no cylinders/capsules/mesh/terrain collision; no sleeping,
islands, inter-frame warm starting, general CCD, qualified tall stacks, joints/constraints,
welded assemblies, Touched events, collision groups, automatic fallen-part removal,
fluids, aerodynamic forces or client network ownership. Fast/thin objects can still tunnel beyond the bounded adaptive-substep envelope.
Extreme scales/velocities and sustained heavy scenes are not qualified. We have
working 3D box/sphere rigid-body physics and basic character foundations, not Roblox-identical trajectories. Do not
claim tested robust stacking; only simple situations were discussed as possible.

## Runtime finishing contracts worth remembering

Read docs/runtime-hardening.md for full details before changing these systems:

- Dynamic lifecycle checks occur at host/scheduler boundaries. Legacy server Scripts
  run in Workspace/ServerScriptService; Server-context Scripts additionally run in
  ReplicatedStorage. LocalScripts run below LocalPlayer; Client-context Scripts
  additionally run in Workspace/ReplicatedStorage/ReplicatedFirst. Templates do not
  execute independently. Disabled or ineligible scripts lose owned timers/waits/
  connections, and re-enabling starts a fresh script environment. Modules retain
  their own per-instance cache. Source remains read-only through Luau.
- VM interrupt budgets: 250 ms elapsed native slice, managed host operations excluded,
  nested callbacks get their own slice, five-second outer deadline checked at VM
  safepoints. A breach poisons/terminates the VM and skips further shutdown callbacks;
  this is fail-closed session termination, not isolation/recovery of one script.
  Managed synchronous I/O cannot be interrupted mid-call. Startup/task/signal/pcall/
  shutdown loop tests pass, and shutdown failure propagates to execution status.
- Each server TCP peer has 120 frames/second with a 120-frame burst and 1 MiB/second
  with a 4 MiB-plus-header burst. Receive work is capped at 256 KiB/32 frames per peer
  per tick. Excess peers are disconnected before further dispatch; healthy peers
  continue. Local clients have the request-count bucket before enqueueing too.
- UpdateAsync holds an exclusive local store lock across a single non-yielding
  transform and durable commit. Exactly one JSON-compatible result is required;
  nil cancels. Errors/unsupported values/yielding roll back; nested DataStore calls
  are rejected. Yieldability checks prevent failed callbacks leaving stale timers.
  Composite currency/item updates from separate processes are tested. GetAsync then
  SetAsync is still not an atomic transaction. There is no distributed session lease.
- TCP credentials are random bearer secrets; the server stores hashes and stable IDs
  in project-root/.woorf/auth. Clients must reuse --identity-file <path> to reconnect
  as the same identity, including across server restarts or changed addresses.
  Separate players need separate files; without a file each connection is a new guest.
  Unknown/malformed credentials and duplicate active identities on one server are
  rejected. No external account login, TLS, recovery/revocation or automatic state
  resume exists. Keep auth state and data together in backups. In-process identities
  remain session-based.

## Next proposed milestone: characters and movement

The latest recommendation was:

1. Player.Character, character spawning, death and respawning.
2. A character collider integrated with the custom physics system.
3. Basic walking, jumping, grounded detection and wall collision.
4. Server-authoritative movement replicated to other clients.
5. A two-headless-player acceptance scene covering walk, wall, jump, death and respawn.

After completing the physics extension and discussing game readiness, the user requested this context save. No character code has been written, no
rig/collider shape has been selected, and implementation has not yet been separately
requested after that recommendation. Rendering/camera/input would follow this
headless gameplay milestone, not be silently bundled into it.

Useful starting facts if characters are authorized:

- Player.cs currently has UserId, Team and derived Neutral only; Character and related
  events/methods do not exist. Humanoid.cs has health/damage/death only, without movement.
- ClassCatalog and Instance.CreateRuntime define admitted classes. New scripting
  members are exposed through InstanceBindings, BackendBindings and NetworkBindings.
- ClientStartup creates PlayerScripts/PlayerGui on client graphs. Do not assume the
  server graph already has equivalent player containers or a default character tree.
- ScriptLifecycle.CanRun currently recognizes the locations above; LocalScripts in
  a future character model under Workspace will need explicit eligibility handling.
- Replication must map new Player/character references, lifecycle removals and late
  joins. Existing ObjectValue/Team/PrimaryPart reference handling provides patterns.
- Each Part is currently independent; multi-part rigs cannot behave as welded bodies
  without implementing assemblies/joints or choosing an explicit first-milestone
  representation. Do not label a box controller a complete Roblox character system.
- Workspace Raycast/Spherecast now support self/descendant filtering. Use the existing RaycastParams subset rather than recreating it. Initial overlaps are ignored by Spherecast; use contact data or an explicit overlap policy where required. Managed fixed-step logic sees live body/contact data, while script queries see published Part state.

## Handy targeted commands

```powershell
# Complete required regression suite (dotnet test does not run this custom runner)
dotnet run --project tests/Woorf.Cli.Tests --no-restore
# Individual Luau cases filtered by name
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --case physics
# Controlled-clock physics
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --physics-clock
# Managed controller foundation checks
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --physics-foundation
# Two local clients, finite example
dotnet run --project src/Woorf.Cli --no-restore -- examples/physics-boxes --clients 2
# Separate TCP server/client example
dotnet run --project src/Woorf.Cli --no-restore -- examples/physics-boxes --listen 127.0.0.1:7777
dotnet run --project src/Woorf.Cli --no-restore -- --connect 127.0.0.1:7777
# Persist a client identity when required
dotnet run --project src/Woorf.Cli --no-restore -- --connect 127.0.0.1:7777 --identity-file .woorf/client-alpha.json
```

The test runner also supports --physics-network <absolute-cli-dll-path>,
--reference-game <absolute-cli-dll-path>, --lifecycle <absolute-cli-dll-path>,
--inbound <absolute-cli-dll-path>, --identities <absolute-cli-dll-path> and
--persistence <absolute-cli-dll-path>. The default CLI DLL is
src/Woorf.Cli/bin/Debug/net9.0/Woorf.Cli.dll after building. --reference-host <project>
is a test-only child-process mode that cancels SocketTransport on stdin input,
allowing graceful shutdown without changing the production CLI.

.local/physics-foundation-final.txt contains the latest full-suite output if still present;
.local is ignored and should never be required as source. For restricted NuGet
config access, README documents an APPDATA-to-.local workaround; ordinary builds
and tests use --no-restore. No new external package is needed for physics.

## Key references

- docs/physics.md: custom solver, supported APIs, example and boundaries.
- docs/runtime-hardening.md: lifecycle, VM budgets, inbound limits, atomic updates and credential identity.
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
