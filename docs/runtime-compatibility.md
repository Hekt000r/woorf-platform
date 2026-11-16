# Runtime compatibility inventory

For the pinned API audit and backend roadmap, see [backend API readiness](backend-api-readiness.md).

The C# graph is authoritative. Native Luau userdata hold object identifiers and call C# for properties and methods. References retain identity across lookups and scripts; `typeof(instance)` returns `Instance`. Every invocation creates a new world; project scripts share one world and VM, with a separate global environment per script.

## Implemented surface

| Area | Supported behavior |
| --- | --- |
| Globals | `game`, `workspace`, `Instance.new(className, parent?)`; project scripts also receive `script` |
| Spatial types | `Vector3` construction, constants, components, arithmetic, magnitude, normalization, `Dot`, `Cross`, `Lerp`; see [spatial types](spatial-types.md) |
| CFrame | Position/rotation constructors, composition, inversion, frame/point/direction transforms; see [CFrame](cframe.md) |
| Model bounds | `GetBoundingBox`, `GetExtentsSize`; pivot-oriented bounds of descendant Parts; see [bounds](bounds.md) |
| Pivots | Part `PivotOffset`, Part/Model `GetPivot` and `PivotTo`, Model `PrimaryPart`/`WorldPivot`, nested movement; see [pivots](pivots.md) |
| Part properties | Synchronized `CFrame`/`Position`, `Size`, `Anchored`, size bounds, change signals, cloning; server-side box and sphere physics, velocities, collision/material properties |
| Instance properties | `Name`, `Parent`, `Archivable`; read-only `ClassName`; Script/LocalScript Enabled, Disabled and RunContext subset |
| Classification | `IsA` follows the supported class inheritance chain; `tostring` returns the name |
| Enumeration | `GetChildren`, `GetDescendants` return independent arrays of Instance references |
| Search | `FindFirstChild` with optional recursive search, `FindFirstChildOfClass`, `FindFirstChildWhichIsA`; `FindFirstAncestor`, `FindFirstAncestorOfClass`, `FindFirstAncestorWhichIsA` |
| Relationships | `IsDescendantOf`, `IsAncestorOf`, `GetFullName`; child-name indexing after properties/methods |
| Lifecycle | `Clone` copies supported structural state and archivable descendants; `Destroy` recursively detaches and locks parenting; `ClearAllChildren` destroys direct children and their descendants |
| DataModel | Root with no parent, `Workspace`, `GetService`, `FindService`, `IsLoaded` |
| Signals | `RBXScriptSignal:Connect`, `Once`; `RBXScriptConnection.Connected`, `Disconnect`; typed userdata with stable signal identity |
| Notifications | `Changed`, `GetPropertyChangedSignal`, `ChildAdded`, `ChildRemoved`, `AncestryChanged`, `DescendantAdded`, `DescendantRemoving`, `Destroying` |

Creatable classes: `Folder`, `Model`, `Configuration`, `Script`, `LocalScript`, `ModuleScript`, `Part`, `RemoteEvent`, `RemoteFunction`, `Player`, `BindableEvent`, `Humanoid`, `Team`, `IntValue`, `NumberValue`, `BoolValue`, `StringValue`, `ObjectValue`, `Vector3Value`, `CFrameValue`, `Color3Value`. Services: `Workspace`, `ReplicatedStorage`, `ReplicatedFirst`, `ServerStorage`, `ServerScriptService`, `StarterGui`, `StarterPack`, `StarterPlayer`, `Players`, `RunService`, `CollectionService`, `HttpService`, `Debris`, `DataStoreService`, `Teams`. These expose only their documented subsets; see [backend APIs](backend-apis.md).

`GetService` creates a supported service on demand and finds it by class even after renaming. `FindService` never creates one. Workspace is created with the world. Loaded projects are complete before scripts execute, so `IsLoaded` is true during execution. Parent assignments reject cycles, invalid argument types, destroyed targets, and changes to the DataModel root. Unknown classes, services, properties, and methods raise errors.

## Modules

`require(moduleScript)` compiles and executes the module on demand in an isolated global environment with its own `script` reference. Modules share the same DataModel and VM as their callers. The source is loaded into the C# instance graph; renaming or reparenting a module does not change its source. Clones copy source but have independent module identities and caches. Runtime scripts cannot read or assign `Source` through Luau.

A module must return exactly one value. Explicit `nil` and `false` are valid single values. The result is cached per module instance for the lifetime of the runtime, including table/function identity, and is shared by server scripts and event callbacks. Unrequired modules do not execute. Compilation errors, runtime errors, and invalid return counts are cached failures; later calls raise the same failure without rerunning the module.

Only ModuleScript instances are supported: numeric asset IDs, string paths, and other values raise errors. Circular dependencies fail immediately with a diagnostic rather than hanging; this is a deliberate woorf convention. Modules can yield, and concurrent callers share a single execution and cached result or failure. Error messages include source line information when available.

Run `dotnet run --project src/Woorf.Cli -- examples/modules` to try [the module example](../examples/modules/woorf.project.json). This follows the [ModuleScript documentation](https://create.roblox.com/docs/reference/engine/classes/ModuleScript) for on-demand execution and a single cached return value; failure caching and cycle diagnostics are documented woorf behavior.

## Signal behavior

Signals begin delivery immediately and callbacks can yield through the cooperative scheduler. `Changed` receives the property name, except ValueObjects whose Changed signal receives the new Value; `GetPropertyChangedSignal` callbacks receive no arguments. Actual changes to `Name`, `Archivable`, and `Parent` publish notifications; assignments of the same value and rejected assignments publish none. Property signals are cached, and valid read-only properties can be observed even though they do not change in this runtime.

`ChildAdded` and `ChildRemoved` observe the updated parent hierarchy. `DescendantRemoving` runs before detachment; its subtree cannot be reparented or destroyed while removal callbacks run. Descendant events cover each member of a moved subtree and only ancestors whose membership changed. Descendant `AncestryChanged` listeners receive the originally moved instance and its new parent, including `nil` when unparented.

`Destroying` runs for the entire subtree before detachment. Destruction disconnects that subtree's event and property listeners. Reentrant destruction is ignored, and callbacks cannot resurrect objects being destroyed. Clones receive independent signals without copied listeners.

Listeners run in registration order for each fire, using a snapshot: newly connected listeners wait for a subsequent fire, and listeners disconnected before their turn are skipped. `Once` disconnects before invoking its callback, including during recursive firing. These are woorf's initial immediate-delivery conventions; exact ordering across different event types is not a compatibility guarantee.

Callback errors are reported to stderr without aborting the property operation or other listeners. Listener closures remain alive across project scripts and retain their original environment. Disconnecting releases their Lua registry references after the current host operation completes; session disposal disconnects the session's listeners before closing its VM. A destroyed signal stays closed; connecting to it returns a disconnected connection.

Try `dotnet run --project src/Woorf.Cli -- examples/signals.luau` for a complete example.

## Limits and deliberate initial conventions

- This is a structural foundation, not a complete Roblox compatibility layer or measured 99% coverage.
- `task.spawn`, `task.defer`, `task.wait`, `task.delay`, `task.cancel`, signal `Wait`, yielding callbacks, and `WaitForChild` are supported; see [scheduler semantics](scheduler.md). Destroying a Script cancels its suspended work. `ConnectParallel`, deferred Instance event delivery, deprecated aliases remain unsupported. Attributes and tags are implemented; see [backend APIs](backend-apis.md).
- Supported service roots are locked to their DataModel after creation and cannot be destroyed or reparented. This preserves singleton identity; exact class-specific Roblox lifecycle rules remain to be expanded. Workspace starts without Terrain or Camera. Other containers are lazy-created rather than matching Studio's complete default tree.
- Project scripts begin in deterministic source-path order and may yield while later scripts start. Headless Heartbeat and dynamic Enabled/Disabled/RunContext lifecycle are supported as documented subsets; Roblox simulation phases remain incomplete.
- LocalScripts execute in isolated client runtimes from supported StarterPlayerScripts/StarterGui templates. Source editing remains unsupported. Clones with loaded source start when enabled in a supported execution location; see [dynamic script lifecycle](runtime-hardening.md#script-lifecycle).
- The filesystem loader rejects duplicate sibling names to keep file mapping unambiguous; runtime-created instances may share names.
- Hierarchy changes stay in memory. There is no place-file import, hierarchy saving, rendering, or Roblox-hosted service access. Physics supports documented oriented-box rigid bodies; see [physics](physics.md). Server-only local DataStores persist JSON-compatible data independently of the hierarchy; see [persistence contract](backend-apis.md#local-persistence-contract). Shared state replicates over the local host and TCP transport.
- Errors describe woorf validation failures; exact Roblox error wording and coercion rules are not promised.

## References

Implementation consulted Roblox's official documentation on 2026-11-15:

- [Engine API index](https://create.roblox.com/docs/reference/engine)
- [Instance](https://create.roblox.com/docs/reference/engine/classes/Instance) and [Object](https://create.roblox.com/docs/reference/engine/classes/Object)
- [DataModel](https://create.roblox.com/docs/reference/engine/classes/DataModel) and [ServiceProvider](https://create.roblox.com/docs/reference/engine/classes/ServiceProvider)
- [Data model organization](https://create.roblox.com/docs/projects/data-model)
- [RBXScriptSignal](https://create.roblox.com/docs/reference/engine/datatypes/RBXScriptSignal) and [RBXScriptConnection](https://create.roblox.com/docs/reference/engine/datatypes/RBXScriptConnection)

These links are live references. The [backend audit](backend-api-readiness.md) pins a selected creator-docs snapshot and records per-symbol implementation evidence. It is not a complete Engine API dump or a behavioral conformance measurement; extend behavior tests alongside each feature.

## Networking and lifecycle

RemoteEvents, RemoteFunctions, local player identities, join/removal, Kick, and
isolated server/client VMs are implemented as a subset. TCP protocol v6 supports
separate processes, incremental replication, dirty capture, bounded outgoing
queues, and transport measurements. Read [client/server semantics](client-server.md)
and [TCP transport](socket-transport.md) for behavior and limits.

The [backend readiness audit](backend-api-readiness.md) records known mismatches,
including the now-fixed RemoteFunction event-member leakage, and identifies remaining backend APIs.
No complete-engine or full-game compatibility percentage is currently established.

The [backend API milestone](backend-apis.md) adds attributes, tags, ValueObjects,
BindableEvent, Color3/Random, JSON, Debris, Heartbeat, local stores, shutdown hooks,
Teams, health-only Humanoid and box raycasts. These are scoped subsets.

See [runtime hardening](runtime-hardening.md) for dynamic activation, execution budgets,
inbound limits, atomic updates and server-issued reconnect credentials.
