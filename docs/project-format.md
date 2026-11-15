# Filesystem projects

Run a project directory with:

```sh
dotnet run --project src/Woorf.Cli -- examples/basic-project
```

The directory contains `woorf.project.json`:

```json
{
  "name": "Hello Woorf",
  "root": "src"
}
```

`name` defaults to `Game`; `root` defaults to `src`. The source directory must exist and remain inside the project directory. The manifest does not create an additional Instance. It configures the root `DataModel` exposed to scripts as `game`.

```text
woorf.project.json
src/
  Workspace/
    Scene/
      init.meta.json
  ReplicatedStorage/
    Shared.luau
  ServerScriptService/
    Main.server.luau
  ServerStorage/
  StarterGui/
    Menu.client.luau
```

Top-level source directories identify services. The current supported containers are `Workspace`, `ReplicatedStorage`, `ReplicatedFirst`, `ServerStorage`, `ServerScriptService`, `StarterGui`, `StarterPack`, and `StarterPlayer`. Only `Workspace` exists automatically; other supported services are created when loaded or requested through `game:GetService()`.

Nested directories become `Folder` instances named after the directory. An optional `init.meta.json` inside a directory selects its class:

```json
{ "className": "Model" }
```

Directory classes currently supported are `Folder`, `Model`, and `Configuration`. Only the `className` metadata field is consumed in this milestone. Service directories retain their service class.

| File suffix | Instance class | Instance name example |
| --- | --- | --- |
| `.server.luau` | `Script` | `Main.server.luau` → `Main` |
| `.client.luau` | `LocalScript` | `Menu.client.luau` → `Menu` |
| `.luau` | `ModuleScript` | `Shared.luau` → `Shared` |

The loader reads the complete hierarchy and script sources before execution. The headless runner starts server `Script` instances under `Workspace` or `ServerScriptService` in ordinal source-file path order. Yielding scripts allow later scripts to start; deferred work and timers drain after startup. Each script receives its own global environment and `script` reference while sharing the same C# DataModel. Startup script failures return exit code 1. Server scripts in storage containers and `LocalScript` instances remain inactive. ModuleScripts execute on demand through `require(moduleScript)`; see [module behavior](runtime-compatibility.md#modules) and [scheduler behavior](scheduler.md).

Unknown service directories, unsupported directory classes, invalid JSON, duplicate sibling names, and linked source paths produce load errors. Other file types are ignored. Runtime hierarchy edits remain in memory; this milestone does not save them to disk.

The format is specific to woorf. It follows Roblox's [DataModel hierarchy](https://create.roblox.com/docs/reference/engine/classes/DataModel) and [container organization](https://create.roblox.com/docs/projects/data-model), but does not read Roblox place/model files or Rojo project files.

