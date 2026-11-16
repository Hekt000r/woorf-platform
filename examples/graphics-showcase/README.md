# OpenGL showcase

From the repository root:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/graphics-showcase --render
```

The scene runs actual Luau scripts and the custom physics engine: boxes and spheres
fall onto the floor, new bodies spawn, old ones are destroyed, and a display cube
rotates. Stairs and a ramp demonstrate transformed geometry and lighting.

WASD moves the camera, Q/E lowers/raises it, arrow keys orbit its target, Shift
moves faster, and Escape closes the window. Resizing changes the viewport and
camera aspect ratio. The viewer stays open until closed, even if scripts drain.

The initial viewer uses a diagnostic palette: anchored geometry is slate, dynamic
boxes are teal and dynamic spheres are orange. These are renderer colors, not a
new Part.Color or Material scripting implementation.

Requires Windows and a driver supporting OpenGL 3.3 core. No graphics package,
GLFW, SDL or other window toolkit is used. See [graphics foundations](../../docs/graphics.md).
