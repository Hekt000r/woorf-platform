# Graphical multiplayer play

From the repository root, launch a shared server and two player windows:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/obby --play --clients 2
```

The launcher starts one authoritative TCP server and two graphical client
processes. Windows are placed side by side, with a maximum default client width
of 640 pixels. `--clients` accepts 1–4; four clients use a two-by-two layout.
Focus a window to control its player: WASD walks, Space jumps, arrows orbit,
R resets, and Escape closes. Losing focus sends zero movement. Closing a window
ends the launched session and cleans up its other clients and server. Ctrl+C in
the launching terminal also ends the session.

All connected players use the same scene and see one another's posed characters.
The server owns movement, collisions, touch events, death and respawn. Each
client sends camera-relative world movement and jump/reset requests for its
authenticated player only. The server validates the packet shape, rejects
nonfinite vectors and forged player identifiers, caps movement through Humanoid,
and stops stale input after half a second. Existing inbound limits also apply.
Clients render replicated state; prediction and interpolation are not implemented,
so remote latency is visible. Local gameplay has been verified on loopback TCP.

For separate processes, start a server:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/obby --listen 127.0.0.1:7777 --characters
```

Then connect from each client's terminal:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- --connect 127.0.0.1:7777 --play
```

`--characters` enables automatic characters for projects that do not already
enable them. Add `--identity-file .local/player1.json` to the graphical connect
command to reuse a server-issued identity; use a different file for each player.
Client addresses currently use numeric IP literals. Authentication, TLS and
internet deployment have the [TCP transport](socket-transport.md) limits.

For a bounded automated capture of both clients:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/obby --play --clients 2 --hidden --frames 120 --capture .local/obby.bmp --profile .local/obby.csv
```

Outputs are `.local/obby.player1.bmp`, `.local/obby.player2.bmp`, and matching CSV
profiles. Completed viewers remain connected until both finish capturing; the
launcher then releases them together. This keeps both characters in each capture.
The dedicated checks are `--multiplayer-play` (composition), `--multiplayer-input`
(real TCP authority/lifecycle/input validation) and `--multiplayer-graphics`
(two real OpenGL clients and captures) in the test runner. The last requires a
Windows OpenGL driver.

`--multiplayer-visible` also checks that both player windows are visible and
positioned side by side. It briefly opens two windows and is included in `--graphics`.
