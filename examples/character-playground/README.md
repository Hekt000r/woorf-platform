# Character playground

Run from the repository root:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/character-playground --play
```

WASD walks relative to the follow camera. Space jumps; release it before another
jump. Arrow keys orbit and tilt the camera. R kills the current character, which
turns gray before respawning. Escape closes the window.

Walk through the gap between the walls, jump onto the two platforms, try the ramp,
or jump beneath the arch to test ceiling collision. Walking off the edge of the
ground eventually kills the character and returns it to the spawn pad.

The blue humanoid is a centrally generated graphical pose around one upright box
collider. Its arms and legs swing with actual traveled distance and switch pose
while airborne. These are visual pieces, not independent physics Parts, welded
assemblies, an imported avatar rig, or a Roblox animation implementation.

The existing OpenGL requirements apply: Windows with an OpenGL 3.3 core driver.
See [character behavior and boundaries](../../docs/characters.md).
