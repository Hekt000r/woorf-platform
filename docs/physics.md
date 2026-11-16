# Headless physics and character foundations

Woorf now has a custom, dependency-free rigid-body solver written in managed C#.
The user explicitly chose control over Roblox behavior and platform requirements
over a third-party physics dependency. No BEPU or other physics package is referenced.
The existing official Luau bridge remains the scripting boundary.

## Implemented subset

- Oriented box and spherical Parts, including rotated ramps and dynamic/dynamic contacts.
- Fixed 120 Hz semi-implicit integration of linear and world-space angular velocity.
- Gravity in studs/second squared, default 196.2; finite negative gravity reverses its direction.
- Anchored bodies, CanCollide, live Part transforms, sizes, insertion/removal and destruction.
- Volume/density-derived mass and box inertia; normal impulses, Coulomb friction,
  restitution and positional penetration correction.
- PhysicalProperties numeric constructors with three or five arguments, immutable
  fields, documented clamps and weighted contact friction/elasticity.
- Server-authoritative transforms, velocity, collision/material state and gravity
  replicated to local and TCP clients. Clients do not independently simulate replicas.
- Part.Shape accepts Enum.PartType.Block (default) and Enum.PartType.Ball. Other
  recognized PartType items are rejected. Shape changes and clones preserve geometry.
- Raycast and Spherecast use actual box/sphere geometry and a RaycastParams subset
  for including/excluding Instances and descendants, and RespectCanCollide.

Spheres use half the smallest Size component as their radius, including for
nonuniform Size. They remain round when rotated. Mass is density times sphere
volume; inertia is 2/5 mass times radius squared. This explicit Woorf sizing rule
does not claim support for ellipsoids or every Roblox resizing behavior.

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

Try the [two-client physics scene](../examples/physics-boxes/README.md), which now
also drops a sphere and verifies a live Block-to-Ball change. TCP protocol v6
requires matching builds; explicit older protocol versions are rejected.

## Queries and grounding

RaycastParams.new() supports FilterDescendantsInstances (copied on assignment and
read), FilterType (Enum.RaycastFilterType.Exclude by default, or Include), and
RespectCanCollide (false by default). Filters include the listed Instance itself
and its descendants. An empty Include filter hits nothing. Unsupported fields
are rejected; CanQuery, collision groups, water and Material results remain absent.
With RespectCanCollide=false, noncollidable Parts can still be queried.

```luau
local params = RaycastParams.new()
params.FilterDescendantsInstances = {collider}
params.RespectCanCollide = true
local ground = workspace:Spherecast(collider.Position, 0.5, Vector3.new(0, -4, 0), params)
```

Spherecast sweeps against rounded box faces, edges and corners and against spheres.
It returns an immutable RaycastResult with Instance, surface Position, outward
Normal and travel Distance. Initial overlaps are ignored, so contact information
or a separate overlap policy is needed for a collider already intersecting geometry.
Radius must be positive and at most 256; travel is limited to 1024 studs.

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

Sphere/sphere contacts use center distance; sphere/box contacts use the closest
point on the oriented box, with a nearest-face escape for an embedded center.
Each fixed step can use up to 64 collision substeps. The subdivision count uses
relative linear travel, gravity, angular surface travel and the smallest dynamic
collider dimension for potentially intersecting swept pairs. Distant fast bodies
do not impose subdivisions when they have no potential collision; common falling
velocity cancels in relative motion, and sphere spin does not enlarge its geometry.
It targets at most one quarter of the dimension per substep. This reduces tunnelling;
it is bounded adaptive discrete collision detection, not general time-of-impact CCD.
Tests include 2-stud boxes and spheres moving at 1200 studs/second into a 0.05-stud
wall. That fixture is evidence for that case, not a guarantee at arbitrary speeds.

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

## Managed controller integration

PhysicsWorld exposes internal engine hooks for the next character milestone:

- BeforeStep runs once before each 120 Hz step, including every catch-up step.
  A managed controller can use GetBody to modify live solver velocity/orientation.
  These are engine integration hooks, not additional Roblox scripting properties.
- SetRotationLocked disables angular integration and inverse inertia, preserving
  the current orientation under off-center impacts. Unlocking restores normal
  rotational dynamics; this does not automatically rotate a body upright.
- GetContacts returns final-step contact points, outward support normals, signed
  separation and the other Part. A controller can distinguish floors, slopes, walls
  and ceilings and choose its own walkable-slope threshold.

Inputs synchronize at host boundaries; live body state continues across catch-up
steps, and Part changes publish once per host advance. The hook receives fixed
step time, not host elapsed time. Removal during a hook excludes the collider from
that step. Contacts clear on separation and external input changes; removed or
callback-teleported geometry cannot remain valid support. GetContacts during a
hook refers to the previous completed step. Script raycasts query published Part
state, so managed substep logic should use live bodies/contact data where required.

Controlled-clock tests drive an upright collider through sustained wall sliding,
a corner, jumping, ceiling impact, stable landing and slope contact. This is enough
foundation for a first single-collider character controller. Player spawning,
Humanoid movement, step climbing, slope policy, air control, moving-platform
attachment and multiplayer character lifecycle remain the next phase.

## Compatibility boundaries

This is a tested box-physics subset, not Roblox-identical physics. Current limits:

- Each Part is an independent body. Welded assemblies, joints, constraints,
  characters, Humanoid movement and network ownership are not implemented.
- No cylinder/capsule/mesh/terrain collisions, collision groups, Touched events,
  fluids, aerodynamic forces or automatic fallen-part destruction.
- Collision substeps are capped at 64: extreme speeds, thin dynamic bodies or
  large angular motion can still tunnel. Spherecast is a query, not automatic CCD.
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
scene verifies both clients receive motion, velocity, sphere shape changes and
settled state. The complete suite passes 207 checks; the backend readiness workload
continues to report 19 available and 0 blocked checks.
