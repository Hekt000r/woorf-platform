# Woorf

## Vision and requirements

Woorf is a Roblox Studio and game runtime alternative. Creating a game should feel
familiar to Roblox developers while giving creators control over where games run.

- Implement the engine in C# and embed the official Luau compiler and VM.
- Preserve familiar Roblox scripting and observable API behavior.
- Design for multiple platforms, with browser/WASM and standalone delivery as goals.
- Eventually provide a familiar Studio-style editor.
- Establish backend/runtime APIs before concentrating on rendering or editor work.
- Aim for 99% compatibility, with the scope and denominator still to be defined.

## Current implementation

The headless CLI and engine embed vendored Luau 0.745 through a native C++ bridge.
The initial execution, hierarchy, signals, modules and cooperative scheduler
milestones are implemented. Filesystem projects load a C# DataModel and isolated
script environments. Vector3, core CFrame, Part transforms, model pivots and bounds
provide a headless spatial subset.

Server and clients have separate VMs and object graphs. Local hosting and separate
TCP processes support RemoteEvents, yielding RemoteFunctions, player join/removal,
Kick and authoritative incremental replication. TCP protocol v7 includes dirty
capture, bounded nonblocking outgoing queues and transport metrics.

Backend APIs include attributes, tags, ValueObjects, BindableEvent, Color3, Random,
JSON, Debris, headless Heartbeat, BindToClose, local durable DataStores, basic teams,
Humanoid health and basic movement, and box/sphere queries. These are documented subsets.
Opt-in character spawning, walking, grounded jumping, death/respawn and an animated
graphical figure are implemented around one upright collider; see [characters](docs/characters.md).
A custom OpenGL 3.3 renderer now provides
the first graphical output through a Windows viewer, with boxes, spheres, lighting,
camera controls and live Workspace updates; see [graphics](docs/graphics.md).
Box and sphere physics with character-controller foundations is implemented in a custom dependency-free C# solver; see [physics](docs/physics.md). Dynamic
script lifecycle, VM budgets, inbound limits and atomic UpdateAsync are implemented.
DataStores use Woorf's local backend. Saved TCP credentials preserve server-issued identities across reconnects and restarts;
in-process UserIds remain session identities. External account login is not implemented.

Windows x64 is verified. Linux, macOS, mobile, WASM, standalone packaging and an
editor remain future work. See [runtime compatibility](docs/runtime-compatibility.md),
[backend API boundaries](docs/backend-apis.md), [client/server behavior](docs/client-server.md)
and [TCP transport](docs/socket-transport.md).

## Compatibility approach

Compatibility means observable behavior: types, defaults, results, errors, events
and scheduling where applicable. API names alone do not establish compatibility.
The [readiness audit](docs/backend-api-readiness.md) pins an official creator-docs
revision and inventories 930 selected symbols. This is a documentation subset,
not a complete Engine API dump, engine-build pin or global compatibility percentage.

Maintain explicit implementation limits and extend local behavior tests with each
feature. Do not add a Studio behavior-comparison harness. Keep engine state and
behavior in C# with native and Luau glue for VM integration and scheduling.

The complete-suite baseline after the character milestone is 232 passing checks, plus
three optional real-OpenGL acceptance checks. The synthetic
readiness workload reports 19 available and 0 blocked checks. Neither establishes
arbitrary full-game support. See [build and verification commands](README.md).

## Representative backend workload

The [headless round game](examples/round-game/README.md) combines persistent test
profiles and currency, tagged inventory, teams, a Heartbeat-driven round, validated
remote requests, joins/leaves and shutdown saves. Its acceptance test runs separate
server/client processes, completes a round, restarts the server and verifies saved
currency, completed rounds and inventory. It uses two saved local credentials and
reverses connection order after restart to prove profile ownership. There is one round per server session. Broader game
support, external account identity and distributed session ownership still need work.

Characters and physics are runtime requirements for games that use them, even
while backend APIs remain the immediate priority. VM budgets, inbound limits, atomic
local updates and credential reconnect are now implemented; see [runtime hardening](docs/runtime-hardening.md). TLS, external
accounts, memory quotas and sustained-load qualification remain production gates.

## Open decisions

- Define the 99% scope: runtime, Studio/plugin APIs, deprecated behavior, hosted
  services, overloads and behavioral cases.
- Choose place/model and asset import formats and representative game dependencies.
- Select standalone targets and packaging strategy, plus the browser/WASM build path.
- Define production hosting, stable authenticated identity and service adapters for
  assets, persistence, purchases, moderation and matchmaking.
- Extend the custom OpenGL renderer and choose editor technology when that milestone begins. The first Win32/WGL window host is Windows-only; other graphics hosts remain future work. Physics is custom managed C# by explicit user preference; broader simulation features remain future work.
- Decide licensing and dependency constraints.

See [context-2.md](context-2.md) for continuation guidance and working-tree precautions.
