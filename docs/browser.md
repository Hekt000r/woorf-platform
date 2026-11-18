# Browser / WebAssembly player

The project requires one cross-platform engine with only tiny platform integration
shims. Animation state/timing, simulation, character control, camera behavior and
rendering policy must remain shared C#. A discrepancy between WASM and native must
be fixed in the shared behavior or adapter contract, not by creating a second
implementation for one host. See [the architecture requirement](../project.md#cross-platform-architecture-requirement).

The browser target runs the same managed C# DataModel, scheduler, custom physics,
character controller and official Luau compiler/VM used by the desktop runtime.
Luau is compiled into a static WebAssembly archive and linked into .NET's browser
runtime. JavaScript supplies the browser event loop, WebGL 2 drawing and Canvas 2D
GUI painting. Gameplay is not reimplemented in JavaScript.

Both hosts reference `Woorf.Presentation`, which owns world collection, character
facing/posing, camera controls and sky selection. Neither host compiles linked
copies of CLI rendering source. `Woorf.Graphics` owns shared meshes, instance
packing/normal transforms, camera projection, image decoding and scene/sky shaders.
OpenGL and WebGL adapters upload resources, bind those shared values and issue draw
calls. WebGL changes the GLSL version/precision declarations, not lighting equations.
The browser uses the same six decoded bundled sky textures and sky orientation as
native. Unchanged texture bytes are sent once, rather than in every frame payload.

Character facing follows the replicated movement direction, including frames that
span large movement distances or occur between physics publications. Gait advances
from published movement velocity and elapsed time, rather than requiring a new
position on every render frame. This avoids dropping to idle between physics
updates at high display refresh rates, including after landing. Manually moved
characters retain a position-travel fallback. GUI layout is shared C#; system-font/Canvas glyph
rasterization remains host-specific and is not expected to be pixel-identical.

## Build on Windows

The [Last Platform Standing arena](../examples/last-platform/README.md) is another
playable browser target, with three bots for solo play:

```powershell
./scripts/build-browser.ps1 -Project examples/last-platform -Output .local/browser-arena
python -m http.server 8081 --directory .local/browser-arena/wwwroot
```

Verify it with `node scripts/test-arena-runtime.mjs` and
`node scripts/test-browser-graphics.mjs --arena`. Its NPC physics/posing, keyboard
service and game rules run through the same managed engine as desktop.

Install the .NET 9 SDK, CMake, Ninja and the .NET WebAssembly tools:

```powershell
dotnet workload install wasm-tools --skip-manifest-update
./scripts/build-browser.ps1 -Project examples/obby -Output artifacts/browser-obby
python -m http.server 8080 --directory artifacts/browser-obby/wwwroot
```

Open `http://localhost:8080`. WASD walks, Space jumps, arrows orbit, and R resets.
The script discovers Emscripten 3.1.56 from the installed .NET 9 workload, builds
the native bridge and Luau with matching WebAssembly exception handling, publishes
the browser application and exports the game. It does not install tools silently.
The first publish links the runtime and can take several minutes. Later builds
reuse native object files. This build script currently supports Windows build hosts.

Deploy the contents of the generated `wwwroot` directory to a static HTTP server.
Serve `.wasm` as `application/wasm`. Opening `index.html` through `file://` is not
supported. A normal HTTP server is enough; the player does not require a desktop
Woorf server or a separate JavaScript build system.

`scripts/export-browser-game.ps1 -Project <directory> -Output <game.json>` exports
the manifest, Luau scripts and folder metadata using the project's configured root.
Saved `.woorf` credentials/data and unrelated project files are excluded. The
package contains server scripts because this player executes a local game in the
browser. Only distribute code and assets intended to be public.

## Runtime contract

The player creates an authoritative local server VM and a separate local client
VM through `LocalPlayHost`. Normal StarterGui and StarterPlayerScripts startup,
replication, remotes, local GUI state and character lifecycle remain active. The
JavaScript animation loop advances the existing scheduler and 120 Hz physics.
The same scene collector, character posing, camera and movement controls are shared
with desktop. GUI uses the shared `GuiScene` layout and interaction implementation.

Native calls use static managed callbacks with an explicit per-VM context.
This avoids unsupported marshalled delegate callbacks and preserves distinct host
bindings and clocks when server/client VMs call the native bridge.

The first browser target is local single-player play. Browsers cannot open the
existing raw TCP transport. Online play needs a WebSocket transport/server adapter;
this build does not silently connect to, or emulate, the desktop TCP protocol.
Local file DataStores are not durable browser storage. File locking, disk paths,
external accounts and other operating-system services are not browser equivalents.
Custom image assets must be hosted separately at their referenced URLs; the initial
exporter packages script sources and metadata, not arbitrary asset directories.
The browser graphics backend currently draws opaque boxes and spheres and the
supported 2D GUI. Desktop-specific OpenGL/WGL code is not called by this backend.

## Runtime acceptance checks

After publishing the package, run `node scripts/test-browser-runtime.mjs` (or pass
another `wwwroot` directory as its argument). This executes the actual WASM runtime
and packaged Luau, checking HUD startup, GUI-to-server remotes, text editing and
caret output, character movement, grounded jumping and continued walking after
landing at 144, 240, 30 and 5 Hz. It does not validate browser
WebGL/Canvas pixels; native overlay pixels are covered by `--gui-graphics`.

After building the native test runner and browser package, run:

```powershell
node scripts/test-browser-graphics.mjs
```

This captures 13 native reference frames and compares actual WebGL pixels in
headless Chrome: all six sky faces, translated/oriented skies, lighting, no-sky
background and three character headings. It requires at least 99% of pixels to
match within 3/255 per color channel. It also loads the complete WASM browser
player and verifies keyboard movement/turning, jump/landing/continued walking
animation, camera orbit, sky and HUD. Set
`CHROME_PATH` if Chrome is installed elsewhere. Artifacts and screenshots are in
`.local/renderer-parity`. This is a bounded rendering regression, not a claim of
99% API compatibility or an exact source-code portability percentage.

## References

The implementation follows Microsoft's [browser .NET JavaScript interop guide](https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop/wasm-browser-app?view=aspnetcore-9.0),
[native WebAssembly dependencies and callback requirements](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-native-dependencies?view=aspnetcore-9.0),
and [WebAssembly build tools](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot?view=aspnetcore-9.0).
Emscripten's [exception support](https://emscripten.org/docs/porting/exceptions.html)
describes the native WebAssembly exception mode used by the Luau archive.
