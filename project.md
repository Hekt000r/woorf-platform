# woorf

## Vision

woorf is a Roblox Studio, Roblox game runtime, and engine alternative. Creating a game should feel familiar to Roblox developers while giving creators control over where and how their games run.

## Confirmed requirements

- Write the engine in C#.
- Embed the actual official Luau runtime as the scripting language and runtime. Integration between Luau and C# is allowed. Preserve the familiar Roblox scripting experience.
- Design the engine to be platform-agnostic and capable of running everywhere, rather than targeting a single operating system.
- Aim for 99% compatibility with existing Roblox Studio APIs.
- Support compiling the engine to WebAssembly (WASM) so games can run online in a browser.
- Support standalone game compilation so creators can distribute games independently.
- Eventually provide an editor with a familiar Roblox Studio interface.
- Begin headless with API implementation and embedded Luau, before adding graphics or building the full editor.

## Initial implementation scope

Build a headless C# runtime that embeds Luau and exposes an initial set of Roblox-compatible APIs. Use executable Luau examples to verify that scripts behave as expected.

Proposed first milestone, pending answers below:

1. Establish the C# solution and an embedded Luau runtime that can execute a script and report errors.
2. Implement the object model: `Instance`, class identity, names, parenting, hierarchy traversal, creation, and destruction.
3. Expose initial globals and services such as `game`, `workspace`, and `game:GetService(...)`.
4. Add the first required value types, property access, methods, and events.
5. Add scheduling and script execution semantics as required by the first compatibility examples.
6. Run a small suite of representative Luau scripts against the implemented surface.

This milestone order is a proposal, not a finalized architecture or API list. Rendering, physics, multiplayer, the editor, and packaging remain later work unless explicitly prioritized.

## Compatibility approach

- Treat compatibility as matching observable behavior, including types, defaults, return values, errors, events, and scheduling where applicable—not just matching API names.
- Define the reference Roblox API version or snapshot before measuring coverage.
- Maintain a compatibility inventory with implemented, partial, unsupported, and intentionally different entries.
- Define what counts toward the 99% target: runtime APIs, editor APIs, plugins, deprecated APIs, and hosted Roblox services need explicit scope decisions.
- Track unsupported features explicitly; avoid silently presenting placeholders as working APIs.
- Keep engine behavior in C# and distinguish it from the Luau embedding layer and eventual editor.
- Evaluate Luau integration options against both native and browser targets before committing to a binding. The binding, .NET version, graphics stack, physics stack, and editor toolkit are undecided.

## Questions to resolve

### Before initial implementation

1. Which APIs or existing example scripts should define the first working milestone? The proposed default is `Instance`, hierarchy operations, basic services, value types, and events. This API selection has not yet been approved.

### Before expanding the engine

5. What does the 99% compatibility target include: game runtime APIs only, or Studio/editor/plugin APIs too? Should deprecated behavior be included?
6. Is importing existing Roblox place/model files and assets required? If so, which formats and what example projects should we support first?
7. Should games use Roblox-style client/server execution and replication from the outset, or begin with local single-player execution?
8. Which standalone targets are required: Windows, Linux, macOS, mobile, or others? Does standalone compilation mean a packaged runtime plus game assets, or native/AOT executables?
9. Should browser games support multiplayer immediately? What hosting and backend functionality should woorf provide?
10. How should Roblox-hosted features such as asset delivery, identity, persistence, purchases, and matchmaking map to woorf: replacements, adapters, or explicitly unsupported APIs?
11. Are there preferred rendering, physics, editor UI, or .NET technologies?
12. Will woorf be open source, and what license and dependency constraints should guide library choices?

## Current decisions and status

- Project name: **woorf**.
- Engine language: **C#**.
- Game scripting language: **Luau**.
- Runtime: **the actual official Luau runtime**, with C# integration allowed.
- Platform strategy: **platform-agnostic; capable of running everywhere**.
- First development focus: **headless execution, embedded Luau, and Roblox-compatible APIs**.
- Long-term delivery: **browser/WASM and standalone games**, plus a familiar editor.
- Implementation is now authorized for the first milestone: embed the official Luau runtime and provide a headless C# CLI that compiles and runs a `.luau` file, starting with `print("hello world")`.
- The initial integration uses vendored Luau 0.745, a small native compiler/VM bridge, and a C# engine library plus CLI.
- The hierarchy milestone adds a C# DataModel/Instance graph, Luau-facing structural APIs, filesystem project loading, and synchronous server-script execution. See [project format](docs/project-format.md) and [runtime compatibility](docs/runtime-compatibility.md) for the supported surface and remaining work.
- The signals milestone adds immediate `Connect`/`Once`/`Disconnect`, connection state, hierarchy notifications, property-change signals, and destruction cleanup.
- The modules milestone adds ModuleScript `require()` with isolated environments, shared per-instance cached results, source cloning, cached failures, and fail-fast cycle diagnostics. Asset-ID/path requires remain unsupported.
- The scheduler milestone adds cooperative task execution, waits and delays, signal waits and yielding callbacks, `WaitForChild`, and concurrent yielding module execution, driven by a C# monotonic-clock headless loop. See [scheduler behavior](docs/scheduler.md) for timing and shutdown conventions.
- Unanswered proposals above remain open decisions.
