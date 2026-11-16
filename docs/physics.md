# Basic headless physics

Woorf now has a custom, dependency-free rigid-body solver written in managed C#.
The user explicitly chose control over Roblox behavior and platform requirements
over a third-party physics dependency. No BEPU or other physics package is referenced.
The existing official Luau bridge remains the scripting boundary.

## Implemented subset

- Oriented box Parts, including rotated ramps and dynamic/dynamic contacts.
- Fixed 120 Hz semi-implicit integration of linear and world-space angular velocity.
- Gravity in studs/second squared, default 196.2; finite negative gravity reverses its direction.
- Anchored bodies, CanCollide, live Part transforms, sizes, insertion/removal and destruction.
- Volume/density-derived mass and box inertia; normal impulses, Coulomb friction,
  restitution and positional penetration correction.
- PhysicalProperties numeric constructors with three or five arguments, immutable
  fields, documented clamps and weighted contact friction/elasticity.
- Server-authoritative transforms, velocity, collision/material state and gravity
  replicated to local and TCP clients. Clients do not independently simulate replicas.

BasePart adds AssemblyLinearVelocity (studs/second), AssemblyAngularVelocity
(radians/second), CanCollide (default true) and CustomPhysicalProperties (default nil).
Gravity and velocities must be finite; velocity values are copied through cloning.
Setting CustomPhysicalProperties to nil restores the Plastic-like default: density
0.7, friction 0.3, elasticity 0.5 and weights 1. This does not implement Material enums.

```luau
local floor = Instance.new("Part", workspace)
floor.Anchored = true
floor.Size = Vector3.new(40, 2, 40)
floor.Position = Vector3.new(0, -1, 0)
floor.CustomPhysicalProperties = PhysicalProperties.new(0.7, 0.5, 0)
local box = Instance.new("Part", workspace)
box.Size = Vector3.new(2, 2, 2)
box.Position = Vector3.new(0, 10, 0)
box.CustomPhysicalProperties = floor.CustomPhysicalProperties
task.wait(1)
assert(math.abs(box.Position.Y - 1) < 0.08)
```

Try the [two-client physics scene](../examples/physics-boxes/README.md).

## Runtime and solver design

The core is separate from DataModel, scheduling, networking and native interop:
RigidBody holds scalar state, BoxCollision performs SAT and face clipping/edge
contact generation, ContactConstraint solves impulses, and PhysicsSolver orders
broadphase pairs and constraints. PhysicsWorld adapts Workspace state to this core.
There are no architecture-specific SIMD intrinsics, native physics binaries, threads,
unsafe blocks or external packages in the solver.

A sweep along world X filters AABB pairs. Narrowphase uses 15 separating axes,
clipped face manifolds or closest edge points. Contacts use 24 sequential impulse
iterations per fixed step. Penetration correction uses a small slop, and low-speed
restitution is suppressed relative to the gravity impulse to avoid resting bounce.
Zero total material weights use an arithmetic mean. These are Woorf solver choices,
not claims about Roblox's proprietary solver.

Physics advances before Heartbeat and scheduled continuations. A server tick may
perform several fixed steps; readback publishes effective Part changes once per
host step. Synchronous callback edits to other Parts are preserved. A maximum of
0.25 seconds is accepted from one elapsed host interval, bounding catch-up work;
longer stalls drop excess simulated time. Bodies are registered from Workspace,
not storage containers. Invalid/non-finite legacy spatial transforms are excluded
from the solver rather than poisoning the scene.

Finite headless CLI runs still finish when their deferred work/timers drain.
Physics alone does not keep a finite script alive. While a timer is pending,
dynamic bodies request fixed-step wakeups. Heartbeat subscriptions and TCP hosts
have their existing lifetime rules. The supplied scheduler clock drives simulation;
there is no second independent wall clock inside the solver.

## Compatibility boundaries

This is a tested box-physics subset, not Roblox-identical physics. Current limits:

- Each Part is an independent body. Welded assemblies, joints, constraints,
  characters, Humanoid movement and network ownership are not implemented.
- No sphere/cylinder/mesh/terrain collisions, collision groups, Touched events,
  fluids, aerodynamic forces or automatic fallen-part destruction.
- Discrete collision detection only: sufficiently fast or thin objects can tunnel.
- No sleep/island system, inter-frame contact warm starting, qualified tall stacks,
  heavy-load performance guarantee or cross-platform bitwise determinism.
- PhysicalProperties supports numeric 3/5-argument construction, not Material-based
  construction or acoustic absorption. Attributes/JSON do not accept this datatype;
  supported remote payloads do. Mass uses a small numerical lower bound.
- Large scales, extreme velocities, difficult edge/edge contacts and long sessions
  need further qualification. Material coefficients and body motion are customizable,
  but this does not establish every Roblox material/solver behavior.

Roblox's [physics overview](https://create.roblox.com/docs/physics),
[assemblies](https://create.roblox.com/docs/physics/assemblies),
[units](https://create.roblox.com/docs/physics/units),
[PhysicalProperties](https://create.roblox.com/docs/reference/engine/datatypes/PhysicalProperties)
and [material properties](https://create.roblox.com/docs/parts/materials) guide the
API and units. No Studio behavior-comparison harness was added.

## Platform design

The solver is ordinary dependency-free managed C# with scalar math. It has no
platform-specific intrinsics, native libraries or OS-specific calls. Platform
build scaffolding was removed at the user's request; verification focuses on
physics behavior and the existing runtime integration suite. The complete engine
still embeds official Luau through its existing native bridge, independently of
this physics implementation.

## Verification

Run the complete suite with the normal test command. Physics checks cover falling
and settling, floors/walls/ramps, dynamic collisions, angular integration, friction,
elastic bounce, density-based momentum transfer, signed gravity, anchoring/filter
changes, immutable material validation and callback edits. A controlled clock
checks analytical free fall and independence from timer partition. The real TCP
scene verifies both clients receive motion, velocity and settled state.
