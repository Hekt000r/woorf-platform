# Characters and graphical play mode

The first character milestone joins the custom physics system to the OpenGL viewer.
It supplies player character lifecycle, an upright collider, walking/jumping and a
centrally posed humanoid appearance. It remains an explicit initial subset.

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/character-playground --play
```

## Input and graphics

WASD moves relative to the camera's horizontal heading. Diagonal input is normalized,
so it cannot exceed axial speed. Space requests one jump per press; R requests death
through the real Humanoid.Health property. Arrow keys orbit and tilt the follow
camera, and Escape closes the viewer. Camera input is a viewer feature rather than
a Roblox UserInputService/Camera scripting implementation.

The camera follows the current HumanoidRootPart and snaps to a replacement root
after respawn. It is a simple follow/orbit camera; collision-aware camera occlusion
handling is not implemented.

CharacterView builds a torso, spherical head, four limbs and eye markers into a
reused RenderItem array. Actual horizontal travel drives stride and facing, so a
character blocked by a wall does not walk in place. Vertical velocity selects a
simple airborne pose. Death changes the appearance to gray until respawn.

CharacterFrameComposer combines these items with the cached scene in a reusable
buffer. The engine hides the local physical root's ordinary box representation.
Parts do not render themselves and the visual limbs are not extra physics bodies;
the existing instanced renderer owns GPU submissions and shared mesh resources.
This is procedural visualization, not Animator, AnimationTrack, Motor6D or a
fully compatible Roblox avatar rig.

## Managed and scripting behavior

Players expose Character and character-added/removing signals. The character model
contains a Humanoid and a HumanoidRootPart, with PrimaryPart pointing to that root.
The first collider is an upright 2-by-4-by-2 stud box. Root transforms are supplied
by the authoritative custom physics simulation.

Humanoid.Move accepts a finite world-space horizontal direction and caps its length
at one. Its optional relativeToCamera argument must be nil/false; true is rejected
until a scripting camera exists. WalkSpeed defaults to 16 studs/second and JumpPower to 30 studs/second;
both are finite nonnegative tuning values. Setting Jump requests a jump, consumed
by the controller on a fixed physics step. Health, TakeDamage and Died retain
their existing behavior.

Character creation is opt-in for existing headless workloads through
Players.CharacterAutoLoads; graphical play mode enables it for its player.
CharacterAutoLoads defaults to false to preserve existing finite headless workloads;
this is a deliberate Woorf difference from Roblox's normal automatic characters.
Player:LoadCharacter() explicitly loads or replaces a character in an authoritative
runtime. Player.Character is read-only through Luau in this subset.
Players.RespawnTime defaults to three seconds and controls delayed replacement. Setting Workspace's
CharacterSpawn attribute to a Vector3 selects the initial spawn location used by
the playground. Falling below the supported world's death threshold kills a
character at Y < -100; player departure removes its character. CharacterSpawn defaults
to (0,6,0), with up to four X offsets spaced four studs apart by UserId. SpawnLocation
objects and collision-safe spawn searches are not implemented.

The controller uses fixed-step live solver bodies and completed contact normals
for grounding. Wall/ceiling contacts do not count as floor support. Gravity,
collision response and qualified high-speed behavior remain in the physics engine.
Walking, grounded jumping, landing, walls and the supported slope policy are covered
by controlled-clock tests. A held jump key does not create automatic repeated jumps.
Walkable support has an outward normal.Y of at least 0.7; the first controller assumes
normal downward gravity and +Y up. It provides full horizontal air control, consumes
airborne jump requests without buffering, and clips inward movement at wall contacts.

## Replication and verification

TCP protocol v7 carries Player.Character references, movement tuning/direction and
player-service spawn settings. Character references are published after the model's
replicated hierarchy and properties are initialized. Visibility changes clear and
restore references without exposing private geometry. Clients do not simulate the
server's collider or write movement back by changing a local Humanoid.

LocalScripts in the owning Player.Character can now start and stop with the existing
dynamic script lifecycle. Other players' Legacy LocalScripts remain ineligible.
An application can validate RemoteEvents and drive Humanoid:Move on the server;
the two-client TCP acceptance test does this for walking, wall collision, jumping,
death and respawn. A graphical TCP client/input transport remains future work.

The normal suite passes 232 checks; `--graphics` adds three real-GPU checks, including
play-mode character capture. Targeted switches include `--character-controller`,
`--character-lifecycle`, `--character-input`, `--character-view`,
`--character-replication`, `--character-network` and `--character-graphics`.
The 1,800-frame idle playground soak measured median 17.025 ms and p95 17.236 ms
on the existing Intel OpenGL 3.3 test machine, with two draw calls. These are workload
measurements, not a general performance guarantee. Hands-on keyboard/camera testing
is left to the user; computer-use automation was unavailable.

## Boundaries

The collider is a single independent body. Capsules, assembled rigs, joints, step
climbing, moving-platform attachment, ladders, swimming, climbing and Roblox's full
Humanoid state machine are absent. Appearance does not change the collision shape.
The viewer currently controls its local player directly; this does not establish
client prediction, network ownership or a graphical TCP input protocol. Camera and
procedural pose behavior are Woorf viewer conventions.

The [playground](../examples/character-playground/README.md) offers walls, jump
platforms, a ramp, an arch and a fall boundary as a concrete acceptance scene.
Use the normal complete regression command before shipping further character work.
