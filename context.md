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

The runtime is still a structural subset. Roblox API coverage has not been measured against a pinned API snapshot. Core Vector3/CFrame and a headless Part property subset are implemented. There is no physics, rendering, client/server replication, execution quotas, or Roblox-hosted service replacement yet.

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

## Recommended next milestone

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
