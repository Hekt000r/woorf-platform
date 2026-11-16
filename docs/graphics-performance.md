# Viewer performance regression and fixes

The reported lag was reproduced on Windows with Intel UHD Graphics, OpenGL 3.3.0
driver build 30.0.100.9864, using the Debug build and graphics-showcase project.
Measurements separate simulation, scene collection, drawing and presentation.

## Main causes

The collision solver selected the world's adaptive subdivision count from every
dynamic body's absolute speed. Parts falling far below the platform continued
accelerating and eventually forced up to 64 collision slices for all other bodies.
Slow frames then requested more fixed-step catch-up work, causing multi-second
frames and tens of megabytes of allocation per frame. Camera movement exposed the
stall because input, simulation and rendering share the host thread.

Collision subdivision now uses relative travel for potentially intersecting pairs.
Current bounds, swept relative translation, gravity and angular travel conservatively
select candidates. Distant fast bodies with no potential collision do not impose
extra slices on the rest of the world. Equal falling velocities do not create
artificial relative speed. Sphere rotation does not enlarge spherical collision
geometry. The existing maximum of 64 adaptive discrete slices and 120 Hz fixed
simulation steps remain; this does not implement continuous collision detection.
Gravity reversal within a step has a regression fixture so the swept candidate
filter cannot miss a ceiling merely because final displacement points downward.

The viewer also used coarse Windows Thread.Sleep waits. Even after fixing simulation,
these stretched otherwise fast frames to approximately 31 ms. A high-resolution
waitable timer now waits to each frame's 60 Hz deadline without a busy loop.

## Central rendering

Instances supply state; the host owns the scene cache and renderer. Scene membership
is rebuilt on hierarchy changes, while transform/size/shape/anchoring changes update
only the affected Part. Unchanged captures reuse the same array without allocations.

Boxes and spheres are grouped into two reusable instance buffers, using at most
two actual GPU draw calls per frame. Indexed mesh data is shared across Parts.
Normal matrices are computed only when instance data changes, rather than inverted
per vertex in GLSL. Uniform locations and CPU staging arrays are reused.

## Measurements

| Metric | Reproduced stalled run | Optimized 3,600-frame soak |
| --- | ---: | ---: |
| Median simulation | 28.151 ms | 4.271 ms |
| 95th-percentile simulation | 2,053.099 ms | 7.221 ms |
| Median total frame | 32.020 ms | 17.023 ms |
| 95th-percentile total frame | 2,053.596 ms | 17.217 ms |
| Worst total frame | 2,857.917 ms | 48.474 ms |
| 95th-percentile managed allocation/frame | 27,509,888 bytes | 106,760 bytes |
| GPU draw calls/frame | One per Part (25 near the end) | 2 |

The stalled run was closed normally after 763 frames over approximately 473 seconds.
The optimized soak completed 3,600 frames in approximately one minute. Frame counts,
real-time physics evolution and spawned-body states differ; these are measurements
of the reproduced failure and fixed workload, not a universal speedup percentage.
Short scheduling spikes remain; this does not promise a hard 60 FPS floor or qualify
large scenes, arbitrary games, every driver or other platforms.

Commands:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/graphics-showcase --render --frames 3600 --profile .local/graphics-profile.csv
dotnet run --project tests/Woorf.Cli.Tests --no-restore
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --graphics
```

The recorded soak used `--hidden` with the same dimensions and driver. Raw CSVs under
.local are diagnostic outputs, not required source. The default suite includes a
physics fixture with a distant collider falling at 6,000 studs/second and a static
scene capture allocation check. Real GPU tests assert 200 draw items require two
submissions, verify depth after moving the camera, and exercise the frame timer.
