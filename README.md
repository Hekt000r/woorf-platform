# woorf

woorf embeds the official Luau compiler and VM in a C# engine library, invoked by a headless CLI. It can run a standalone script or load a filesystem project into a C# DataModel with Roblox-style hierarchy APIs.

## Build and run

Prerequisites: .NET 9 SDK, CMake 3.20+, and a C++17 compiler. Ninja is needed for the commands below. On Windows, use an MSVC developer shell or put MinGW-w64 on PATH. Build the native bridge before building C# so its library is copied to the output.

```sh
cmake -S native -B build/native -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/native --target woorf_luau
dotnet restore tests/Woorf.Cli.Tests --configfile NuGet.Config
dotnet run --project src/Woorf.Cli --no-restore -- examples/hello.luau
```

Expected output:

```text
hello world
```

To load a complete project and run its server scripts:

```sh
dotnet run --project src/Woorf.Cli --no-restore -- examples/basic-project
```

See [filesystem project format](docs/project-format.md) for directory conventions and [runtime compatibility](docs/runtime-compatibility.md) for the implemented API surface. [examples/signals.luau](examples/signals.luau) demonstrates hierarchy events and property-change listeners.

With MinGW, add `-DCMAKE_C_COMPILER=gcc -DCMAKE_CXX_COMPILER=g++` to the configure command. Native libraries are written to `artifacts/native` and copied beside the managed binaries. Rebuild C# after rebuilding the native bridge. Use a separate build directory for each target architecture or compiler.

In a restricted Windows environment where the user NuGet configuration cannot be read, set `$env:APPDATA = Join-Path $PWD '.local'` in PowerShell before invoking .NET. No NuGet packages are required.

## Tests

Module example: `dotnet run --project src/Woorf.Cli -- examples/modules` loads a shared configuration with `require()`. See [module semantics](docs/runtime-compatibility.md#modules). [examples/scheduler.luau](examples/scheduler.luau) demonstrates tasks and child waits; see [scheduler behavior](docs/scheduler.md).

```sh
dotnet run --project tests/Woorf.Cli.Tests --no-restore
```

This dependency-free integration test runner starts the actual CLI and checks script execution, hierarchy APIs, project loading, isolated script environments, and invalid inputs. A failing test returns a nonzero exit code. This is the project's test command; `dotnet test` does not execute this runner.

CLI exit codes: `0` success, `1` file/compilation/execution/native-loading failure, `2` incorrect arguments. Diagnostics go to stderr; script prints go to stdout.

## Scope

The native bridge uses portable CMake and the managed engine uses C#/.NET. Windows x64 is currently verified; Linux, macOS, mobile, and WASM have not been verified. Browser support will require a separate .NET/WASM and native interoperability build path. The current runtime includes a structural subset of Roblox APIs with signals, property-change events, yielding ModuleScript `require`, and cooperative task scheduling. Local headless client/server execution and RemoteEvents are supported through --clients N; see [client/server runtime](docs/client-server.md). TCP transport supports separate server/client processes; editor, rendering, and physics remain future work. There are no execution time or memory quotas yet.

See [project.md](project.md) for the broader goals and [vendor/LUAU.md](vendor/LUAU.md) for upstream provenance and licenses.


The headless spatial subset adds Vector3, core CFrame transforms, and Part CFrame, Position, Size, and Anchored properties. See [spatial types](docs/spatial-types.md) and run `examples/parts.luau` for a complete example.

See [CFrame and Part transforms](docs/cframe.md) and run `examples/cframe.luau` to rotate and move a Part.

[Part and Model pivots](docs/pivots.md) support moving groups while preserving relative transforms. Run `examples/pivots.luau` for an example.

[Model bounds](docs/bounds.md) expose `GetBoundingBox()` and `GetExtentsSize()`. Try `examples/bounds.luau`.

Run a server and separate clients using [TCP transport](docs/socket-transport.md).

See the [backend API readiness audit](docs/backend-api-readiness.md) for the pinned
reference snapshot, measured subset, confirmed gaps, and prioritized API roadmap.

The [backend API milestone](docs/backend-apis.md) documents the implemented audit
paths, local persistence contract, and remaining compatibility limits.
