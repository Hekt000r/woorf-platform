# Spinner Obby

Run from the repository root:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/obby --play
```

WASD walks, Space jumps, arrows orbit the camera, R resets, and Escape closes.
Follow the course away from the spawn pad: jump across the staggered pads, hop
over three low kill strips, and land on the wide checkpoint pad. Then dodge two
counter-rotating bars, cross the narrow balance beam, and reach the finish arch.
Progress and completion messages appear in the terminal.

Touching a kill strip or spinner, or falling below the course, kills the player.
Respawn takes one second. The checkpoint saves progress for the current session;
after reaching it, deaths and R return you there. Restart the example to start over.
The scene uses gravity 80 and the default jump speed 30, giving roughly 5.5 studs
of jump height. The course gaps fit that jump at the default walk speed.

`Course.luau` builds the geometry and connects ordinary `Part.Touched` handlers
for kill bricks, the checkpoint and finish. The engine detects actual box/sphere
contact; the game does not poll custom overlap geometry. Heartbeat animates the
anchored spinners and checks fall height. Moving-platform attachment, UI widgets
and persistence remain outside this example.
Hazards use noncolliding geometry so contact kills instead of blocking movement.
Red bars are lethal; the green pad after the hurdles is the checkpoint. Jump pads
are blue, purple and pink; the balance beam is orange and the finish arch is gold.
The course supplies `RenderColor` Color3 attributes, a viewer convention that
overrides the default palette. The course is intended for local `--play`.

Run its headless acceptance check with:

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --obby
```

Like the graphical showcase, the example has an ongoing Heartbeat connection.
For an automated rendering run, add `--hidden --frames 120 --capture .local/obby.bmp`.
