# Initial hierarchy runtime

The C# graph is authoritative. Native Luau userdata hold object identifiers and call C# for properties and methods. References retain identity across lookups and scripts; `typeof(instance)` returns `Instance`. Every invocation creates a new world; project scripts share one world and VM, with a separate global environment per script.

## Implemented surface

| Area | Supported behavior |
| --- | --- |
| Globals | `game`, `workspace`, `Instance.new(className, parent?)`; project scripts also receive `script` |
| Spatial types | `Vector3` construction, constants, components, arithmetic, magnitude, normalization, `Dot`, `Cross`, `Lerp`; see [spatial types](spatial-types.md) |
| CFrame | Position/rotation constructors, composition, inversion, frame/point/direction transforms; see [CFrame](cframe.md) |
| Model bounds | `GetBoundingBox`, `GetExtentsSize`; pivot-oriented bounds of descendant Parts; see [bounds](bounds.md) |
| Pivots | Part `PivotOffset`, Part/Model `GetPivot` and `PivotTo`, Model `PrimaryPart`/`WorldPivot`, nested movement; see [pivots](pivots.md) |
| Part properties | Synchronized `CFrame`/`Position`, `Size`, `Anchored`, size bounds, change signals, cloning; no physics |
| Instance properties | `Name`, `Parent`, `Archivable`; read-only `ClassName` |
| Classification | `IsA` follows the supported class inheritance chain; `tostring` returns the name |
| Enumeration | `GetChildren`, `GetDescendants` return independent arrays of Instance references |
| Search | `FindFirstChild` with optional recursive search, `FindFirstChildOfClass`, `FindFirstChildWhichIsA`; `FindFirstAncestor`, `FindFirstAncestorOfClass`, `FindFirstAncestorWhichIsA` |
| Relationships | `IsDescendantOf`, `IsAncestorOf`, `GetFullName`; child-name indexing after properties/methods |
| Lifecycle | `Clone` copies supported structural state and archivable descendants; `Destroy` recursively detaches and locks parenting; `ClearAllChildren` destroys direct children and their descendants |
| DataModel | Root with no parent, `Workspace`, `GetService`, `FindService`, `IsLoaded` |
| Signals | `RBXScriptSignal:Connect`, `Once`; `RBXScriptConnection.Connected`, `Disconnect`; typed userdata with stable signal identity |
| Notifications | `Changed`, `GetPropertyChangedSignal`, `ChildAdded`, `ChildRemoved`, `AncestryChanged`, `DescendantAdded`, `DescendantRemoving`, `Destroying` |

Creatable classes: `Folder`, `Model`, `Configuration`, `Script`, `LocalScript`, `ModuleScript`, `Part`. ModuleScripts support on-demand execution; Script/LocalScript class-specific APIs remain partial. Services: `Workspace`, `ReplicatedStorage`, `ReplicatedFirst`, `ServerStorage`, `ServerScriptService`, `StarterGui`, `StarterPack`, `StarterPlayer`. These containers provide hierarchy behavior, without their class-specific engine features. Workspace also reports its Model/PVInstance ancestry. Part reports FormFactorPart/BasePart/PVInstance ancestry; abstract ancestors are not creatable.

`GetService` creates a supported service on demand and finds it by class even after renaming. `FindService` never creates one. Workspace is created with the world. Loaded projects are complete before scripts execute, so `IsLoaded` is true during execution. Parent assignments reject cycles, invalid argument types, destroyed targets, and changes to the DataModel root. Unknown classes, services, properties, and methods raise errors.

## Modules

`require(moduleScript)` compiles and executes the module on demand in an isolated global environment with its own `script` reference. Modules share the same DataModel and VM as their callers. The source is loaded into the C# instance graph; renaming or reparenting a module does not change its source. Clones copy source but have independent module identities and caches. Runtime scripts cannot read or assign `Source` through Luau.

A module must return exactly one value. Explicit `nil` and `false` are valid single values. The result is cached per module instance for the lifetime of the runtime, including table/function identity, and is shared by server scripts and event callbacks. Unrequired modules do not execute. Compilation errors, runtime errors, and invalid return counts are cached failures; later calls raise the same failure without rerunning the module.

Only ModuleScript instances are supported: numeric asset IDs, string paths, and other values raise errors. Circular dependencies fail immediately with a diagnostic rather than hanging; this is a deliberate woorf convention. Modules can yield, and concurrent callers share a single execution and cached result or failure. Error messages include source line information when available.

Run `dotnet run --project src/Woorf.Cli -- examples/modules` to try [the module example](../examples/modules/woorf.project.json). This follows the [ModuleScript documentation](https://create.roblox.com/docs/reference/engine/classes/ModuleScript) for on-demand execution and a single cached return value; failure caching and cycle diagnostics are documented woorf behavior.

## Signal behavior

Signals begin delivery immediately and callbacks can yield through the cooperative scheduler. `Changed` receives the property name; `GetPropertyChangedSignal` callbacks receive no arguments. Actual changes to `Name`, `Archivable`, and `Parent` publish notifications; assignments of the same value and rejected assignments publish none. Property signals are cached, and valid read-only properties can be observed even though they do not change in this runtime.

`ChildAdded` and `ChildRemoved` observe the updated parent hierarchy. `DescendantRemoving` runs before detachment; its subtree cannot be reparented or destroyed while removal callbacks run. Descendant events cover each member of a moved subtree and only ancestors whose membership changed. Descendant `AncestryChanged` listeners receive the originally moved instance and its new parent, including `nil` when unparented.

`Destroying` runs for the entire subtree before detachment. Destruction disconnects that subtree's event and property listeners. Reentrant destruction is ignored, and callbacks cannot resurrect objects being destroyed. Clones receive independent signals without copied listeners.

Listeners run in registration order for each fire, using a snapshot: newly connected listeners wait for a subsequent fire, and listeners disconnected before their turn are skipped. `Once` disconnects before invoking its callback, including during recursive firing. These are woorf's initial immediate-delivery conventions; exact ordering across different event types is not a compatibility guarantee.

Callback errors are reported to stderr without aborting the property operation or other listeners. Listener closures remain alive across project scripts and retain their original environment. Disconnecting releases their Lua registry references after the current host operation completes; session disposal disconnects the session's listeners before closing its VM. A destroyed signal stays closed; connecting to it returns a disconnected connection.

Try `dotnet run --project src/Woorf.Cli -- examples/signals.luau` for a complete example.

## Limits and deliberate initial conventions

- This is a structural foundation, not a complete Roblox compatibility layer or measured 99% coverage.
- `task.spawn`, `task.defer`, `task.wait`, `task.delay`, `task.cancel`, signal `Wait`, yielding callbacks, and `WaitForChild` are supported; see [scheduler semantics](scheduler.md). Destroying a Script cancels its suspended work. `ConnectParallel`, deferred Instance event delivery, attributes, tags, and deprecated aliases remain unsupported.
- Supported service roots are locked to their DataModel after creation and cannot be destroyed or reparented. This preserves singleton identity; exact class-specific Roblox lifecycle rules remain to be expanded. Workspace starts without Terrain or Camera. Other containers are lazy-created rather than matching Studio's complete default tree.
- Project scripts begin in deterministic source-path order and may yield while later scripts start. Roblox scheduling phases, Heartbeat, and RunContext semantics are not implemented.
- LocalScripts load as objects only. There is no client runtime, source editing, or dynamic script startup. Cloning copies stored script source but does not execute a new script automatically.
- The filesystem loader rejects duplicate sibling names to keep file mapping unambiguous; runtime-created instances may share names.
- Hierarchy changes stay in memory. There is no place-file import, saving, rendering, physics, replication, persistence, or Roblox-hosted service access.
- Errors describe woorf validation failures; exact Roblox error wording and coercion rules are not promised.

## References

Implementation consulted Roblox's official documentation on 2026-11-15:

- [Engine API index](https://create.roblox.com/docs/reference/engine)
- [Instance](https://create.roblox.com/docs/reference/engine/classes/Instance) and [Object](https://create.roblox.com/docs/reference/engine/classes/Object)
- [DataModel](https://create.roblox.com/docs/reference/engine/classes/DataModel) and [ServiceProvider](https://create.roblox.com/docs/reference/engine/classes/ServiceProvider)
- [Data model organization](https://create.roblox.com/docs/projects/data-model)
- [RBXScriptSignal](https://create.roblox.com/docs/reference/engine/datatypes/RBXScriptSignal) and [RBXScriptConnection](https://create.roblox.com/docs/reference/engine/datatypes/RBXScriptConnection)

These are live references, not a pinned API snapshot. Future compatibility work should pin an API dump and extend behavior tests alongside each feature.
