# Physics boxes

A headless acceptance scene using Woorf's custom C# box solver. An anchored floor
supports a box released from 10 studs. Two clients observe the falling and settled
box and a sphere, verify shape/velocity/material replication and acknowledge completion before leaving.
The sphere begins as a Block and changes to Ball when both clients are ready,
exercising live shape replication as well as falling and settling.

Run everything in one process:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/physics-boxes --clients 2
```

Or run a separate TCP server:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/physics-boxes --listen 127.0.0.1:7777
```

In two other terminals:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- --connect 127.0.0.1:7777
```

Both clients must join before the box is released. Each prints physics-client and
exits after acknowledgement; the server prints physics-server. Stop the TCP server
with Ctrl+C. The --clients run finishes by itself. There is no renderer in this scene.

Gravity is 50 studs/second squared, both bodies are 2 studs tall, and the floor top is
at Y=0. Both final centers are approximately Y=1. CustomPhysicalProperties disables
bounce for this scene. The test suite copies this actual project into a temporary
directory and runs separate server/client processes.

See [physics behavior and design](../../docs/physics.md) for supported
APIs, solver design and current limits. Use matching builds: physics state uses TCP
protocol v6. This scene does not claim Roblox-identical contact trajectories.
