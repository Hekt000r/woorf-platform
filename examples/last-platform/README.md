# Last Platform Standing

A sky-island knockout arena for one to four human players. Solo play automatically
adds BOLT, GLITCH and NOVA. Two humans get two bots; four humans fill the arena.
Bots are server-controlled Humanoid models, not fake Player connections.

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/last-platform --play
dotnet run --project src/Woorf.Cli --no-restore -- examples/last-platform --play --clients 2
```

Browser local single-player with bots:

```powershell
./scripts/build-browser.ps1 -Project examples/last-platform -Output .local/browser-arena
python -m http.server 8081 --directory .local/browser-arena/wwwroot
```

Open http://localhost:8081. The verified package is already generated at
`.local/browser-arena/wwwroot`. Browser multiplayer remains unsupported.

## Play

- WASD moves; Space jumps; arrow keys orbit the camera.
- **E / SHOVE:** visible amber wind-up, then a short-range frontal shove. Cooldown:
  1.6 seconds. Evade the warning, approach from the side or counterattack.
- **Q / SKY JUMP:** higher jump with a six-second cooldown. Dodge a shove or escape
  a cracking tile; it does not permit additional jumps while airborne.
- Flashing orange/red tiles fall after a 1.6-second warning. Outer rings collapse
  first, forcing everyone toward the center.
- Falling or dying eliminates you for the round and returns you to the viewing
  deck. Last survivor earns a win. First to three becomes champion; the match resets.
- **B / BOTS:** the first connected human can toggle bots for the next round.
  Solo play with bots off is a collapse-survival practice mode.

Rounds begin after a six-second opening countdown and last up to 65 seconds.
Results lead automatically into the next round. Late joins spectate until that
round. R retains the viewer's normal character reset behavior.

## Implementation and boundaries

Rules, tile timing, AI, attacks, scoring and rounds are server Luau. The LocalScript
builds the HUD and requests actions through a RemoteEvent. The server validates
participation, cooldowns, payload shape, hit distance/facing and bot-toggle ownership.
Clients cannot choose the victim or push strength. Shoves use short scripted server
displacement with a small vertical launch; they do not implement impulse APIs.

Bots pursue opponents, avoid unsafe tiles, shove at close range and attempt escape
jumps. They do not use PathfindingService or guarantee optimal play. NPC physics,
eight-piece posing and keyboard state are shared C# on desktop and WASM.

Player attributes `ViewDistance` and `ViewPitch` configure the viewer's wider arena
framing. These are Woorf viewer conventions, like `RenderColor`, not Roblox Camera
properties. Other games retain their existing camera defaults.

## Verification

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --arena-engine
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --arena
node scripts/test-arena-runtime.mjs
node scripts/test-browser-graphics.mjs --arena
```

Tests cover NPC movement/jumping/removal/posing, typed keyboard events, solo bots,
elimination, scoring, restart, validated combat and collapsing tiles. Actual WASM
checks keyboard-to-server actions, bot toggling and subsequent rounds. Chrome runs
the complete graphical game and compares native/WebGL reference frames. Run the
complete custom native suite too; `dotnet test` is not the project's test runner.
