# OpenGL graphics foundations

Woorf's first graphical path is a custom C# OpenGL 3.3 core renderer. It adds no
NuGet dependencies. The existing native Luau library remains the only bundled
native runtime dependency; graphics calls use the operating system's OpenGL driver.

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/graphics-showcase --render
dotnet run --project src/Woorf.Cli --no-restore -- examples/parts.luau --render
```

This mode runs a standalone script or a filesystem project's server scripts on
the window's owning thread. The existing scheduler and server physics advance
before each scene capture. Script errors return failure. Closing the window runs
the existing shutdown callbacks. Graphics keep the viewer alive after script work
drains; normal headless commands retain their existing lifetime behavior.

## Library and integration

- `Woorf.Graphics` has no dependency on Woorf.Engine, Luau, or a window toolkit.
  It uses the .NET standard library, System.Numerics and direct OS/driver interop.
- `Platform/OpenGlWindow` owns a resizable Win32 window, device context and WGL
  core context. A temporary legacy context loads the modern context constructor.
  OpenGL access and disposal require the creating thread. Dispose renderer resources
  before disposing their window. Other OS window/context backends remain future work.
- `OpenGL/Gl`, `ShaderProgram` and `GpuMesh` load driver entry points, compile/link
  GLSL with diagnostics, upload indexed geometry once and release GPU resources.
  Uniform locations are cached. Per-instance transforms, normal transforms and
  colors use reusable buffers and instanced vertex attributes.
- `MeshData` supplies outward-wound unit boxes and unit-radius spheres with normals.
  `RenderItem` supplies shape, model transform and linear RGB color. Mesh and program
  ownership are currently internal; arbitrary asset mesh/shader APIs are not exposed.
- `PerspectiveCamera` supplies a right-handed view and perspective projection with
  OpenGL's -1/+1 clip depth. It uses radians and looks along local negative Z.
  System.Numerics row-vector matrices map to GLSL column-vector matrices on upload.
- `OpenGlRenderer` draws opaque geometry with depth testing, backface culling,
  ambient/directional illumination, inverse-transpose normals and output gamma.
  Singular/nonfinite model transforms are rejected. Its initial API owns the GL
  state it uses; it does not preserve arbitrary external rendering state.
  The engine groups draw data by mesh: at most one indexed instanced draw for boxes
  and one for spheres per frame. Parts own no rendering methods or GPU resources.
- The CLI's `WorkspaceScene` adapts CFrame and Size to draw items, excluding storage
  geometry and invalid transforms. Spheres use the physics radius rule: half the
  smallest Size component. Each frame observes insertion, removal, shape, transform
  and size changes. CanCollide=false Parts remain visible. Membership is cached;
  hierarchy changes trigger a rescan and property changes recapture only dirty Parts.
  Unchanged scene captures reuse their array without allocations. The returned array
  is borrowed by the host and updated in place at the next capture.
- `Platform/FramePacer` uses a disposable high-resolution Windows waitable timer
  for the 60 Hz viewer target. It avoids coarse Thread.Sleep timing and does not
  busy-spin or change the system-wide multimedia timer period. This timer backend
  requires Windows 10 version 1803 or later.

The viewer uses slate for anchored Parts, teal for dynamic boxes and orange for
dynamic spheres. The library accepts draw colors; Part.Color is not implemented.
WASD/QE moves the viewer camera, arrows orbit, Shift speeds movement and Escape
closes it. These are viewer controls, not a Roblox Camera or input scripting API.

## Capture and verification

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/graphics-showcase --render --frames 120 --capture .local/graphics.bmp
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --graphics
```

`--width N --height N` sets initial client dimensions (1..8192). `--frames N` stops
after N actual rendered frames. `--capture path.bmp` writes the final backbuffer
before presentation. It uses padded bottom-up 24-bit BMP rows. `--hidden` creates
an invisible window for automated GPU checks. Capture and hidden mode require a
finite frame count. Frame counts do not select a virtual simulation clock.

Add `--profile report.csv` to collect per-frame simulation, scene capture, drawing,
presentation, total frame duration, managed allocations, Part count and actual
draw-call count. Rows are buffered and written on normal viewer exit. Use a finite
frame count for profiling. [Performance findings](graphics-performance.md) describe
the reproduced slowdown and the qualified before/after measurements.

The optional graphics test command needs an interactive Windows desktop and a real
OpenGL 3.3 driver. It checks actual framebuffer pixels for geometry/deletion and
order-independent depth occlusion, camera movement, two-draw submission for 200 Parts,
singular-transform validation, odd-width capture and high-resolution frame waits.
GPU tests are separate from the ordinary headless regression command. Camera
projection, mesh winding/normals, dirty-scene caching and physics isolation checks
run in the complete default suite.

## Boundaries

The first window backend is Windows only; the engine and mesh/camera math remain
platform-neutral. No cross-platform window host, browser/WebGL path, renderer for
TCP clients, client prediction, GUI, shadows, textures, asset loading, transparency,
postprocessing, skeletal animation or Roblox Camera/Lighting service API exists yet.
Only opaque boxes and spheres are drawn. Frustum culling, asset batching beyond the
two primitive meshes and broader sustained-load performance qualification remain
future work. OpenGL failures are reported rather than silently
falling back to an older fixed-function renderer.

Context setup follows [Khronos WGL guidance](https://wikis.khronos.org/opengl/Creating_an_OpenGL_Context_%28WGL%29)
and [Microsoft's pixel-format contract](https://learn.microsoft.com/en-us/windows/win32/opengl/pixel-format-functions).
