# Woorf continuation context

## Resume here — saved 2026-11-18

This is the current handoff. It supersedes older working-tree claims below.
HEAD is `0617959`; git status was clean before this documentation edit, so the
implementation has been committed by the user. Inspect status on resume. Never
reset, clean, discard or commit work without authorization.

Completed: GUI/obby HUD, shared C# native/WASM presentation and sky/lighting parity,
animation timing fixes after landing, and Last Platform Standing with solo bots,
combat, collapsing tiles, scoring and automatic rounds. Latest native regression:
**279 PASS, zero FAIL**, plus seven OpenGL checks. Actual WASM arena tests, Chrome
game/input checks, 13 native/WebGL pixel comparisons and obby animation regressions
passed. Detailed logs and architecture are recorded in the sections below.

Latest user clarification: the arena game itself is **entirely Luau**. Geometry,
bots, combat, collapse, rounds, scoring and HUD live in examples/last-platform/src.
Reusable C# additions are UserInputService keyboard InputBegan/InputEnded/IsKeyDown,
A-Z/Space KeyCodes, existing Humanoid movement/jumping extended to NPC Models and
shared NPC posing. ViewDistance/ViewPitch are Woorf viewer attributes, not Roblox
Camera APIs. No ApplyImpulse API exists; the shove uses scripted displacement.

Mandatory: keep the entire engine as platform-neutral reusable C# as possible.
Only tiny native/browser integration shims are allowed. No separate platform
gameplay, animation, physics or rendering-policy implementations. Apply TDD and
the coding/testing skills; consult official Roblox documentation for new APIs.
See project.md and docs/input.md. Do not claim full Roblox compatibility or
production readiness.

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/last-platform --play
# Add --clients 2 for desktop multiplayer.
python -m http.server 8081 --directory .local/browser-arena/wwwroot
# Open http://localhost:8081 for the already-built browser solo game.
./scripts/build-browser.ps1 -Project examples/last-platform -Output .local/browser-arena
```

Controls: WASD, Space, arrows; E shove, Q charged jump, B host-only bot toggle for
the next round. Solo defaults to three bots. Browser multiplayer is absent. Read
examples/last-platform/README.md, docs/browser.md, docs/gui.md and docs/input.md.
.local contains ignored generated builds, logs and captures, not source.

No implementation task is pending. This request was only to save continuation
context. Follow the next user instruction, and handle simple requests promptly.

## Last Platform Standing — 2026-11-18

User authorized a fun knockout arena with bots, TDD for engine/API work and official
Roblox references for new APIs. The new game is `examples/last-platform`. It is
implemented and uncommitted alongside the earlier GUI/parity changes; preserve all
files. Run `dotnet run --project src/Woorf.Cli --no-restore -- examples/last-platform --play`;
add `--clients 2` for a shared-server desktop session. Browser package is generated
at `.local/browser-arena/wwwroot`; serve it on HTTP (example README uses port 8081).

Gameplay: six-second opening countdown, four contestants by default, 65-second
maximum rounds, frontal E shove with .3s visible windup/1.6s cooldown, Q higher jump
with 6s cooldown, flashing outer-ring tiles with 1.6s warning before falling,
elimination/viewing deck, automatic results/restart and first-to-three champion.
Bots fill empty slots (three in solo, two with two humans). B/HUD bot toggle is
host-only and applies next round. Bots-off solo is a valid practice mode. AI uses
server Luau pursuit, safe-tile steering, shoves and escape jumps. Shove displacement
is scripted with a small vertical launch, not an invented impulse API.

Added shared engine support:
- NpcWorld attaches the existing character controller to non-player Workspace
  Models with Humanoid/Part PrimaryPart, excludes player-owned characters and
  removes stale controllers. NpcFrameComposer shares CharacterView posing.
- Client-only, non-creatable UserInputService supports keyboard InputBegan,
  InputEnded and IsKeyDown for A-Z/Space, immutable typed InputObjects, real KeyCode
  numeric values, held/repeat state and GUI-processed flags. Native/browser input
  adapters forward to the same C# service. No server replication or AI in adapters.
- Optional player `ViewDistance` and initial `ViewPitch` attributes configure the
  shared viewer. These are documented Woorf conventions, not Roblox Camera APIs.

Official API sources and precise boundaries are in docs/input.md. Relevant red/green
tests are --arena-engine and --arena; the latter drives actual Luau combat, collapse,
rounds and HUD/server state. Full suite caught a hidden legacy character-root
regression from broader NPC rendering; restored that behavior and reran green.

Current verification: complete native suite **279 PASS, zero FAIL**, all seven native
OpenGL checks pass, plus a captured two-human/two-bot TCP arena session. Actual WASM
arena runner passes four acceptance checks, including E/remote cooldown, bot toggle
and subsequent practice rounds. Chrome passes all 13 native/WebGL pixel fixtures
and two complete arena checks (E/Q combat and host bot toggle). Images were inspected.
Logs: `.local/arena-full-tests-final.log`, `.local/arena-native-graphics.log`,
`.local/arena-multiplayer.log`, `.local/arena-wasm-final.log`,
`.local/arena-browser-graphics-final.log`. Captures: `.local/arena-preview.bmp`,
`.local/arena-multiplayer.player1.bmp`, `.local/arena-parity/player.png`.
Both arena and obby browser packages were rebuilt from current shared source.
Commands: `node scripts/test-arena-runtime.mjs`,
`node scripts/test-browser-graphics.mjs --arena`; normal browser tests still target
the obby package. See examples/last-platform/README.md for launch and rules.

## Mandatory architecture constraint and active animation report

User requirement reiterated 2026-11-18: the **entire engine** must be as close to
cross-platform/non-platform-bound reusable C# as possible. Only tiny native/browser
integration shims are allowed. Simulation, animation state/timing, character and
camera behavior, scene composition and GUI layout/rendering policy belong to shared
code. Never add a platform-specific animation/gameplay patch or copy shared engine
logic into a host. See project.md, "Cross-platform architecture requirement".

The user reported WASM animations struggling/stopping after jumping and landing.
Found a shared animation bug: CharacterView treated no new position on a render
frame as idle, resetting limbs between physics publications. Actual WASM at 144 Hz
reproduced pose dropouts (116/145 moving samples), while the native capped 60 Hz
loop hid the cadence assumption. Shared CharacterView now advances gait from
published horizontal velocity and elapsed time; it uses position travel as a
fallback for manually moved characters, ignores large teleports and idles when
actual motion stops. No platform-specific animation branch was added.

Verification: **275 PASS, zero FAIL** in .local/landing-full-tests.log. Shared
--character-animation regression passed red/green. Actual WASM passed **13 checks**,
including jump/landing/walking and 144/240/30/5 Hz cadence tests; log
.local/landing-wasm-green.log. Native/WebGL comparisons all passed and the complete
Chrome player passed keyboard jump, landing and 45 subsequent walking frames;
log .local/landing-browser-graphics.log. Its floor uses the obby's zero-elasticity
material so a physical bounce is not misclassified as a failed landing animation.
Browser package rebuilt at .local/browser-obby/wwwroot. The old parity checks had
not exercised continued walking after landing; keep these regressions.

## Native/browser rendering parity — 2026-11-18

This section supersedes rendering details in the earlier GUI continuation below.
The user requires platform-neutral reusable C# presentation, with thin platform
display/input adapters. The browser previously had separate lighting shaders,
different clear color and no skybox. Character facing also depended on rendering
position deltas below four studs, so slow frames could prevent rotation entirely.

Shared `src/Woorf.Presentation` now owns WorkspaceScene, WorkspaceSky, CharacterView,
CharacterFrameComposer, CharacterControls, CharacterViewer, WorldView and packet
encoding. Both CLI and Browser reference that assembly; linked CLI source files
were removed from the browser project. CharacterViewer contains no window API;
native polling lives in CharacterWindowInput. Facing uses replicated MoveDirection
while limb animation still uses actual travel. Browser textbox focus now suppresses
camera orbit consistently with desktop too.

Graphics scene shaders, sky shaders, InstanceBatch and SkyImage now live in the
platform-neutral Graphics namespace. SceneDrawState packs the same model/normal/
color data for both GPU adapters. Skybox.LoadImages and camera sky projection are
shared. Browser WebGL only compiles supplied shader sources, uploads packed data
and decoded sky pixels, binds uniforms and draws. No browser-specific lighting
math, camera calculations, sky substitute or character posing remains there.
System fonts versus Canvas glyph rasterization remains a documented GUI boundary.

Verification: full native runner **274 PASS, zero FAIL**, plus **7 PASS** OpenGL
acceptance checks. Actual WASM runner has **8 PASS**, including the six shared
sky textures, turning and long-frame facing. `node scripts/test-browser-graphics.mjs`
passed 13 actual native/WebGL pixel comparisons with zero pixels exceeding 3/255
channel error, plus a complete Chrome WASM player check for keyboard movement,
turning, camera orbit, sky and HUD. Screenshot `.local/renderer-parity/player.png`
was inspected. Final full-suite rerun also passed 274/0, exit code 0. Logs:
`.local/parity-full-tests-final.log`, `.local/parity-native-graphics.log`,
`.local/parity-wasm-final.log`, `.local/parity-browser-graphics-final.log`.

Native/backend and browser builds passed without compiler warnings/errors. Browser
package was rebuilt at `.local/browser-obby/wwwroot`. Run the same HTTP server as
before and refresh the page after rebuilding. The graphics comparison requires
Chrome (`CHROME_PATH` overrides its path) and writes disposable .local fixtures.
The full Chrome test uses a local debugging endpoint owned by its headless child
process and closes that process afterward. Do not mistake Node-only WASM testing
for proof of WebGL appearance. No changes have been committed.

## GUI continuation — 2026-11-18

This section supersedes the historical handoff below. The user requested completion
of another agent's GUI implementation. All implementation remains uncommitted;
preserve the working tree, including untracked files.

The GUI milestone includes Vector2/UDim/UDim2 and GUI enums, screen containers,
text/buttons/images, scrolling, layouts and size modifiers, shared layout/hit
testing, typed input payloads, text editing, local/TCP replication and StarterGui
respawn handling. Desktop overlays render centrally in Woorf.Graphics. Local play
uses isolated server/client VMs through LocalPlayHost, enabling LocalScripts and
gameplay remotes. The obby now has a scripted stage/time/deaths HUD, finish status
and authoritative restart button. Read docs/gui.md for supported behavior/limits.

The previous agent also added a browser/WASM host, static per-VM native callbacks
and build/export scripts. It uses the same managed engine and official Luau. It
supports local single-player only; browser TCP multiplayer is absent. Read
docs/browser.md. Generated package: .local/browser-obby/wwwroot. Custom assets are
not automatically exported.

Continuation fixes: missing caret/selection drawing, emoji backspace, selection
collapse on arrow navigation, browser GUI JSON serialization of derived Vector2
properties, stale game files/input state after browser reload, and browser keyboard
navigation routing. The browser input fixture explicitly persists its screen across
the initial character spawn. A full rebuild resolved stale incremental references.

Useful verification commands:

```powershell
cmake --build build/native --target woorf_luau
dotnet build tests/Woorf.Cli.Tests --no-restore -t:Rebuild
dotnet run --project tests/Woorf.Cli.Tests --no-build --no-restore
dotnet run --project tests/Woorf.Cli.Tests --no-build --no-restore -- --graphics
dotnet run --project tests/Woorf.Cli.Tests --no-build --no-restore -- --gui-editing
./scripts/build-browser.ps1 -Project examples/obby -Output .local/browser-obby
node scripts/test-browser-runtime.mjs
```

Final complete custom runner: **273 PASS, zero FAIL**, exit code 0. Log:
.local/gui-finish-tests-final.log. All seven real OpenGL checks passed, including GUI pixels, preserved 3D depth,
multiplayer captures and visible windows. Actual WASM passed five acceptance checks:
game/HUD startup, remote activation, text navigation/caret, walking and jumping.
Desktop capture .local/gui-finished-obby.bmp was visually inspected. Native/managed
builds succeeded with zero compiler warnings/errors. An isolated desktop build is
.local/gui-finish-build; pass the same absolute ArtifactsPath for restore/build/run.
Never rebuild a loaded output directory.

One run overlapping WASM optimization failed the existing test "cancels server
callbacks and client timers when their player disconnects", reporting "disconnected
client timer resumed". Preserve this observation when investigating scheduler and
disconnect ordering; do not weaken the fixture to hide it. It passed in the final
complete-suite rerun without WASM optimization load.

## Historical handoff (superseded by the GUI continuation above)

Updated 2026-11-17. This is the current handoff and supersedes the older milestone
notes that previously occupied this file. Read this before starting new work.

## Resume here

Woorf is now an early playable multiplayer engine: official Luau, a C# DataModel,
custom physics, playable characters, graphical TCP clients, engine touch events,
and six-face skyboxes. The Spinner Obby is the current playable acceptance game.
It is not an arbitrary-Roblox-game-compatible or production-ready platform.

The user's latest request is to save the current context and the proposed next
step for a new session. This request is documentation work only. The next GUI/input
milestone described below was recommended, not yet implemented or separately
requested. Follow the user's next instruction; do not silently start unrelated work.

At this save, HEAD is `01d497b feat: graphical multiplayer support`, following
`fb9cd90 feat: player character` and `84dbc21 feat: graphics engine (win opengl)`.
The working tree was clean before updating this file: the user has committed the
implementation, including the latest visibility fix. Inspect `git status` again
on resuming. Never reset, clean, discard or commit work without authorization.

## User preferences and engineering requirements

- Vision: a Roblox Studio/runtime alternative, implemented in C# with the official
  Luau compiler/VM, familiar scripting and observable Roblox behavior. Standalone,
  browser/WASM, broader platforms and an editor are longer-term goals.
- The user prioritizes working runtime/game APIs and concrete playable examples.
  Do not claim compatibility merely because API names exist. The 99% ambition has
  no agreed complete denominator and is not a measured result.
- Apply `general-coding-rules`, `test-driven-development` and `writing-good-tests`
  skills to code/test changes. Read their SKILL.md files when required in the new
  session. Functions <=70 lines; code files <=350 lines; preserve existing naming,
  separation of concerns and reuse. Avoid speculative abstractions/dependencies.
- Write a meaningful behavioral regression, observe the intended failure, implement
  the fix, and run the relevant checks plus the complete custom test runner.
  A passing test added after the fix is not evidence of a red/green cycle.
- Finish authorized implementation; the user dislikes stopping at plans, merely
  adding failing tests, or unnecessary confirmation requests.
- The user explicitly requested delegation for touch events, graphical multiplayer
  and skyboxes. Those three agent tasks are complete and root reviewed/integrated
  them. Do not assume this authorizes delegation for every future unrelated task;
  follow current session instructions. No agents have unfinished implementation.
- Physics must remain a custom, platform-neutral managed C# solver. The user rejected
  replacing it with BEPU/another physics package and rejected per-platform build
  scaffolds/checks. Do not recreate those or a Studio behavior-comparison harness.
- Rendering belongs centrally in the engine; Parts do not own rendering methods,
  GPU resources, or separate per-part drawing loops. No external graphics packages.
- Earlier hands-on input checks were left to the user because computer use was
  unavailable. Automated real-GPU and native window-visibility tests now exist.
  Do not confuse hidden framebuffer captures with proof a visible window appears.

## Current verification and useful commands

Latest complete normal regression: **252 PASS, zero FAIL**.
Latest `--graphics`: **6 PASS**, including actual visible two-window verification.
These passed after the multiplayer window-visibility fix. Build had no compiler
warnings/errors; Git may emit LF-to-CRLF normalization notices.

```powershell
# Required complete suite; dotnet test DOES NOT run this custom test runner
dotnet run --project tests/Woorf.Cli.Tests --no-restore
# Real Windows/OpenGL checks; includes a brief visible two-player launch
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --graphics
# Play the obby alone
dotnet run --project src/Woorf.Cli --no-restore -- examples/obby --play
# Shared server and two visible player windows side by side
dotnet run --project src/Woorf.Cli --no-restore -- examples/obby --play --clients 2
# Separate graphical server/client processes
dotnet run --project src/Woorf.Cli --no-restore -- examples/obby --listen 127.0.0.1:7777 --characters
dotnet run --project src/Woorf.Cli --no-restore -- --connect 127.0.0.1:7777 --play
# Finite two-client capture; produces .player1/.player2 BMP and CSV files
dotnet run --project src/Woorf.Cli --no-restore -- examples/obby --play --clients 2 --hidden --frames 120 --capture .local/obby.bmp --profile .local/obby.csv
```

WASD walks relative to the camera; Space jumps once per press; arrows orbit;
R resets/kills; Escape closes. Focus a player window to control that player.
`--play --clients 2..4` starts one server and that many graphical clients; four
use a 2x2 arrangement. Closing a client ends the launched session. Ctrl+C stops
its children and server. Plain `project --clients 2` remains HEADLESS multiplayer.
Launching two independent `project --play` commands does not share a game.

Targeted flags in `tests/Woorf.Cli.Tests/Program.cs`:
`--obby`, `--case touch`, `--touch-replication`, `--multiplayer-play`,
`--multiplayer-input`, `--multiplayer-graphics`, `--multiplayer-visible`,
`--case "Sky under"`, `--sky`, `--sky-graphics`, `--graphics-scene`,
`--character-controller`, `--character-lifecycle`, `--character-input`,
`--character-view`, `--character-replication`, `--character-network`,
`--character-graphics`, `--physics-clock`, `--physics-foundation`, `--socket`.
See Program.cs for the older child-process/test modes and their arguments.

Diagnostics if still present: `.local/multiplayer-visibility-tests.log` is the
latest 252-check suite; `.local/features-regression.log` is the prior integrated
suite. `.local/multiplayer-preview.player1.bmp` and `.player2.bmp` show both avatars
and the obby sky. `.local` is ignored, disposable diagnostic output, never source.

Build prerequisites: .NET 9, CMake and a C++17 toolchain; native artifacts already
exist. After changing native bridge code, run
`cmake --build build/native --target woorf_luau`, then rebuild C# to copy the DLL.
Use `--no-restore` for ordinary builds. If restore is needed, use repository
`NuGet.Config`; README documents the restricted APPDATA workaround. Do not add
packages just to restore existing code. Concurrent agents used separate absolute
`-p:ArtifactsPath=C:/Users/Hektor/woorf-platform/.local/<name>-build` on BOTH restore
and run to avoid bin/obj locks. Do not rebuild a loaded output directory. Serialize
GPU verification; overlapping driver tests stalled one earlier run.

## Runtime, backend and project architecture

- `src/Woorf.Engine`: DataModel classes, scripting bindings, scheduling, networking,
  custom Physics and Characters. Official vendored Luau 0.745 is embedded through
  the native C++ bridge. `src/Woorf.Cli` hosts commands and the render adapter.
  `src/Woorf.Graphics` is independent of Engine/Luau and uses System.Numerics plus
  direct OpenGL/Windows interop. `tests/Woorf.Cli.Tests` is the integration runner.
- DataModel/Instance hierarchy, clone/destruction, signals, attributes/tags,
  immutable Vector3/Color3, core CFrame, pivots and bounds are implemented subsets.
  Filesystem games use `woorf.project.json` and service folders containing scripts.
- Separate Luau environments and per-ModuleScript caches support yielding require,
  cached failure, cycle errors and cooperative task waits/delays/cancellation.
  Dynamic Script/LocalScript lifecycle handles supported locations, Enabled/Disabled
  and Legacy/Server/Client RunContext; script-owned work is canceled on removal.
- Backend subsets: CollectionService, typed ValueObjects, BindableEvent, Random,
  HttpService JSON, Debris, Heartbeat, BindToClose, teams, Humanoid health/movement,
  spatial queries and local durable DataStores. BindableFunction, Tool/Backpack,
  broad Enum/datatype coverage and many services remain absent.
- Server and clients have separate VMs and object graphs. The server is authoritative;
  changing replicated client objects does not write back. Remotes preserve tuples
  and map visible Instance references; sender identity comes from the connection.
- TCP protocol is **v8**. Explicit older versions are rejected; match builds.
  Initial snapshots precede incremental property/addition/removal patches, ordered
  before related remotes. Dirty tracking avoids idle full-graph scans. Late joins,
  disconnect cleanup, yielding RemoteFunctions and reference visibility are tested.
- LocalNetworkHost supports synchronous embedding/join/leave. SocketTransport is
  the headless TCP API; SocketPlayClient supplies the pumped graphical client.
- VM budgets, inbound admission limits and bounded nonblocking writes exist.
  See docs/runtime-hardening.md before changing these contracts. Current budgets:
  250ms native execution slice and 5s outer deadline; a breach terminates the VM,
  not just one script. Managed synchronous host I/O is outside that interrupt.
  Per-peer input: 120 frames/s burst120; 1MiB/s burst4MiB+header; read cap256KiB/
  32frames/tick. Outgoing limit8MiB, pump256KiB/tick, stalled writes expire after5s.
- DataStores use project-root/.woorf/data, exclusive locks, durable flush and atomic
  replacement. UpdateAsync is a non-yielding local transaction: nil cancels,
  errors roll back, nested datastore operations are rejected. Not distributed storage.
- Server-issued bearer credentials persist hashed identities in project-root/.woorf/auth.
  `--identity-file <path>` reuses a client identity; concurrent players need separate
  files. Otherwise each join is a new guest. No external accounts/TLS/memory quotas,
  distributed session ownership, automatic reconnect/resume or production qualification.
- `examples/round-game` tests profile ownership, inventory, teams, validated requests,
  rewards and persistence across a real server restart with reversed client order.

## Physics and characters

- Custom managed oriented-box/sphere solver, fixed120Hz integration, gravity,
  friction/restitution, density-derived mass/inertia, angular motion and live edits.
  Part.Shape supports Block/Ball; sphere radius is half the smallest Size component.
  Other recognized shapes are rejected. Each Part is an independent body.
- Adaptive collision subdivision is capped64 per fixed step; it is not universal
  continuous collision detection. Distant fast bodies must not force extra substeps
  on unrelated local bodies; keep the existing performance regressions.
- Raycast/Spherecast support boxes/spheres and the documented RaycastParams subset
  (descendant include/exclude and RespectCanCollide). Spherecast ignores initial
  overlaps. CanQuery, collision groups, terrain, meshes and broader queries are absent.
- PhysicsWorld.BeforeStep/GetBody/SetRotationLocked/GetContacts are internal managed
  hooks. Controller logic uses live solver bodies; Luau queries see published state.
  GetContacts reflects the previous completed step. Publication preserves callback
  edits/teleports, and contact invalidation must remove stale support correctly.
- Characters are Models with Humanoid and a single upright2x4x2 HumanoidRootPart;
  PrimaryPart is the root. Player.Character, CharacterAdded/Removing, LoadCharacter,
  Players:GetPlayerFromCharacter, Humanoid:Move, Jump/JumpPower and WalkSpeed work.
- Defaults: WalkSpeed16, **JumpPower30** (reduced from50), CharacterAutoLoads=false
  for headless compatibility, RespawnTime3. `--play` enables automatic characters.
  Default CharacterSpawn Workspace attribute is(0,6,0); engine spawn offsets are
  four studs by UserId modulo4. Falling belowY=-100 kills. Obby overrides these.
- Controller locks root rotation, grounds at contact normal.Y>=0.7, clips wallward
  movement, caps diagonal speed, preserves airborne Y and rejects air-jump buffering.
  No step climbing, moving-platform attachment, joints/assemblies, full avatar rig,
  animations API or complete Humanoid state machine. Appearance is procedural.

## Engine touch events and the obby

- BasePart now supports Touched(otherPart), TouchEnded(otherPart), CanTouch and
  GetTouchingParts(). Touches come from physics box/sphere substeps, including
  noncolliding sensors; at least one participant must be unanchored.
- Both parts need CanTouch=true for events. Disabling it disconnects handlers;
  connecting while disabled errors. CanTouch clones and replicates locally/TCP.
  GetTouchingParts has the documented older collision/touch-interest restrictions;
  consult docs/physics.md rather than treating it as an unrestricted overlap query.
- Direct CFrame edits alone do not synthesize events. Fast scripted CFrame sweeps
  do not have continuous collision guarantees. No TouchTransmitter instances,
  collision groups or independently simulated client touch events are implemented.
- PhysicsTouchTracker queues entry/exit events after simulation, then validates
  parts against final solver state and callbacks. Destruction, ancestry changes,
  callback teleports and disabled touch must not expose stale contacts/events.
- Obby Course.luau uses normal Touched handlers for kill bricks, checkpoint and
  finish; custom overlap polling was removed. Heartbeat rotates spinners/checks
  fall height. It has staggered pads, red kill strips, two spinners, checkpoint,
  narrow balance beam and finish arch. Gravity80 gives ~5.5stud jumps at power30.
- Obby respawn is1s; its fall limit isY<-12. Stage/finish flags are player attributes,
  session-only. Stage1 CharacterAdded normalizes spawnX to +/-2 so persistent guest
  UserIds cannot spawn off the pad; stage2 respawns at checkpointZ=-52. No GUI yet:
  checkpoint/completion messages appear in the terminal, including multiplayer.

## Graphics, multiplayer and skyboxes

- First graphics host is Windows Win32/WGL OpenGL3.3; other platforms, standalone
  distribution, browser/WASM and editor remain future work. Rendering supports
  opaque boxes/spheres, lighting and sky cubemaps, not general Part textures,
  transparency, shadows, GUI or a full Roblox Camera/Lighting implementation.
- WorkspaceScene caches membership and dirty geometry; unchanged scenes reuse
  arrays. OpenGlRenderer batches primitives into two draws; a Sky adds a third.
  CharacterView produces8 visual items per avatar, not physical limb bodies.
- RenderColor Color3 attributes override the default slate/teal/orange palette.
  This is a viewer convention, NOT Part.Color support. Removing it restores defaults.
- RenderRuntime selects local authority or a pumped SocketPlayClient. Server input
  packets carry direction/jump/reset for their authenticated player only, validate
  payloads and reject forged/nonfinite input. A0.5s lease stops stalled movement.
  No client prediction/interpolation or client physics ownership is implemented.
- CharacterFrameComposer renders every player, caches membership and captures each
  pose once per frame. Physical character roots are hidden from Workspace geometry.
  Follow cameras snap after respawn. No camera collision/occlusion handling yet.
- MultiplayerLauncher owns one hidden server and client processes, places windows,
  forwards game logs, cleans up children and supports finite captures. Completed
  bounded clients stay connected until the parent releases all, preserving peers
  in captures. Graphical connect currently accepts numeric IP literals.
- `Sky` under `Lighting` has the six Roblox face IDs SkyboxBk/Dn/Ft/Lf/Rt/Up plus
  SkyboxOrientation in degrees, applied Y thenX thenZ. Clone, change signals and
  local/TCP live deltas work. Camera translation never moves the sky.
- There is NO automatic Sky instance for every game. `Instance.new("Sky",
  game:GetService("Lighting"))` loads bundled default faces. The obby already does
  this in Sky.server.luau. Without a Sky, the background is solid.
- Default Roblox sky512_*.tex IDs map to Woorf's own bundled PNGs, not Roblox artwork.
  Custom equal-square1..4096px noninterlaced8-bit RGB/RGBA PNG and uncompressed24/32bit
  BMP paths are supported. Paths are relative to process working directory or
  absolute, and files must exist on every client; image bytes are not transferred.
  Roblox CDN/asset IDs, newer Content APIs, sun/moon/stars, time-of-day, Atmosphere,
  reflections and general textures remain unsupported. See docs/skybox.md.

## Recent regressions: do not reintroduce

1. No jumping with moving spinners: synchronizing ANY changed physics Part used to
   clear ALL grounding contacts. Invalidation now affects only the changed part
   and its contacts. Preserve the moving-unrelated-hazard grounded-jump regression.
2. User reported Windows unknown software exception0xe0434352. Found/reproduced an
   uncaught remote-root cache lookup: roots excluded by WorkspaceScene.Rebuild were
   still dirtied on movement, then Capture indexed missing slots. Capture now skips
   untracked hidden roots with TryGetValue. No matching Windows crash log proved
   this was the exact dialog, but the real crash was fixed and tested. Moving-peer
   GPU fixtures exercise it. Do not dismiss a generic crash code as GPU failure.
3. Two clients initialized OpenGL but no windows appeared: launcher applied
   ProcessWindowStyle.Hidden to graphical clients as well as the server. Visible
   clients now use Normal, server/explicit --hidden clients remain Hidden.
   PlayerWindowProbe checks REAL owned native window visibility and side-by-side
   positions; framebuffer-only hidden tests previously missed this bug.
4. Double avatar capture erased walking poses; compositor now captures once and
   copies cached results. Per-frame membership/closure allocations were also fixed.
5. Repeated multiplayer sessions could place UserId4 atX12 beyond obby bounds;
   obby now supplies safe CharacterAdded spawn positions. Preserve guest4 regression.
6. Touch tests cover spinner rotation without duplicate entry, velocity edits while
   touching followed by exit, sphere-corner false positives, subtree removal cache
   cleanup, callback destruction and transform-publication teleports.
7. Graphical TCP connection timeout now uses cancellation (caught by CLI), avoiding
   an uncaught TimeoutException. Launcher cleanup tolerates already-exited children.

## Next recommended milestone: finish one small multiplayer game

The last assessment recommended completing the obby as a small end-to-end game,
using its concrete needs to guide engine work rather than collecting API names.
The engine has playable foundations; the largest gameplay usability gap is GUI
and script-facing input. Do not begin an editor or broad speculative compatibility
expansion without a user request.

Suggested order:

1. Maintain launch/lifecycle reliability: visible and hidden launches, repeated
   sessions, movement/jump/reset, joins/leaves, safe spawns, clear error diagnostics.
   The latest suite is green; do not invent a broad hardening project or repeat
   checks indefinitely without a relevant change/failure.
2. **Next implementation proposal: basic Roblox-style GUI/input needed by the obby.**
   Build a minimal useful scripted HUD with stage, timer, deaths, finish result and
   restart interaction. Inspect existing PlayerGui/StarterGui/LocalScript startup
   and render/input boundaries first. Choose a bounded documented subset of actual
   Roblox APIs, research official documentation, and write failing end-to-end tests
   before implementation. Keep UI rendering central, gameplay state authoritative,
   and per-player UI isolated. Do not hardcode obby-only widgets into the renderer
   or disguise custom methods as complete Roblox compatibility.
3. Character/network polish driven by playtests: moving-platform attachment, camera
   obstruction handling, and controlled latency tests. Choose interpolation and
   prediction only after measuring actual input/replication behavior under latency.
4. Package a runnable game with its assets, without the source tree or dotnet run.
   Windows is the currently verified target; broader targets need explicit scope.

Proposed success criterion: someone else can launch the packaged multiplayer obby,
understand its progress/status, finish it and restart, without reading terminal
logs. GUI/input work, network polish, packaging and editor work have NOT started.

## Documentation/audit caveats and navigation

`project.md`, `context.md` and some readiness/audit text still describe older
milestones and counts. This handoff and current implementation supersede those
stale statements (notably v7, JumpPower50, one-player-only viewer, and232checks).
Do not treat historical selected API counts as a fresh compatibility measurement.
The last stored inventory summary is206implemented-subset/496missing/169legacy/
59restricted across930selected documentation symbols, pinned to creator-docs commit
5a56d0d361d709aa785585b738354134ae47b4ae. It has NOT been comprehensively refreshed
for touch, Sky and graphical multiplayer. Source-manifest hashes may also be stale;
refresh audit evidence deliberately if that is part of the next requested task.
The older backend readiness workload reported SUMMARY19 0; it was not rerun for
this context save. Neither that probe nor252tests qualifies arbitrary full games.

Important references:
- README.md: native/build/run/test commands.
- project.md: vision/open decisions; implementation summary has stale details.
- docs/multiplayer-play.md: launcher, graphical TCP input, captures and visibility.
- docs/physics.md: solver, queries, touch contracts and limits.
- docs/characters.md: controller/lifecycle/input/appearance boundaries.
- docs/skybox.md: supported Sky API, official sources and asset limitations.
- docs/graphics.md and graphics-performance.md: render/cache/batching/pacing.
- docs/project-format.md, runtime-compatibility.md and scheduler.md: script projects.
- docs/client-server.md and socket-transport.md: replication/remotes/transport.
- docs/runtime-hardening.md and backend-apis.md: budgets/lifecycle/persistence.
- docs/backend-api-readiness.md and docs/api-audit/: selected audit and old probes.
- examples/obby/, character-playground/, graphics-showcase/, physics-boxes/,
  round-game/: acceptance scenes for their respective documented subsets.

No code changes or tests are required just to save this human-readable handoff.
