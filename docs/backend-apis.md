# Backend API milestone

The 15 missing entry paths from the readiness audit are now implemented, and the
RemoteFunction event-member mismatch is fixed in the Luau-facing API. All 19
synthetic readiness probes pass. These remain documented runtime subsets, not
complete Roblox class coverage or a claim that arbitrary full games can run.

## Implemented paths

| Path | Working behavior |
| --- | --- |
| warn | Formats arguments with tabs and writes a line to stderr |
| Attributes | SetAttribute/GetAttribute/GetAttributes, removal with nil, attribute signals, validation, independent clones, shared-instance replication |
| Tags | Instance tag methods and CollectionService membership queries/signals, ancestry changes, cloning, destruction, replication |
| ValueObjects | IntValue, NumberValue, BoolValue, StringValue, ObjectValue, Vector3Value, CFrameValue, Color3Value; typed Value, value-bearing Changed, property signals, cloning and replication |
| BindableEvent | Event subscriptions and tuple-preserving Fire; yielding listeners and independent cloned signals |
| Random | Seeded independent generators, NextInteger/NextNumber/NextUnitVector and Clone |
| HttpService JSON | JSONEncode/JSONDecode for finite numbers, booleans, strings, contiguous arrays and string-keyed objects; cycle/type validation |
| Debris | AddItem schedules destruction independently of the scheduling Script's lifetime |
| Heartbeat | Positive elapsed-time ticks, signal waits, yielding listeners, connection-driven headless liveness |
| Persistence | Server-only GetDataStore plus GetAsync/SetAsync/RemoveAsync/IncrementAsync/UpdateAsync against a durable local file backend |
| Shutdown | BindToClose callbacks run concurrently, may yield, and get up to 30 seconds; completion is based on registered callbacks rather than unrelated subscriptions |
| Teams | Team creation, Teams:GetTeams, Team:GetPlayers, Player.Team assignment and derived read-only Neutral state |
| Humanoid | Health/MaxHealth, TakeDamage, HealthChanged and Died, plus world-relative Move, WalkSpeed, JumpPower, MoveDirection and Jump; see characters |
| Queries | Nearest ray and swept-sphere intersections against oriented boxes and spheres; immutable RaycastResult, descendant filters and RespectCanCollide |
| Color3 | Immutable native single-precision values, new/fromRGB/fromHSV, R/G/B, Lerp and ToHSV; attributes, ValueObjects and remote payloads |
| RemoteFunction fix | FireServer/FireClient/FireAllClients/OnServerEvent/OnClientEvent lookups are rejected; invoke methods and callbacks still work |

Internal underscore-prefixed remote transport hooks are reserved implementation
details, not part of the public Roblox-compatible API.

## Local persistence contract

Projects store data below project-root/.woorf/data. Standalone scripts use
.woorf/data relative to the working directory. The directory is ignored by Git.

Store name/scope pairs are encoded unambiguously and hashed to safe filenames.
Operations use an exclusive per-store lock, durable file flush, and atomic
replacement. Reads return independent JSON values. Concurrent IncrementAsync
operations from separate processes are serialized by the lock. Keys, names and
scopes have validated nonempty length limits. Client runtimes cannot obtain stores.

This is a synchronous local backend under Roblox-shaped method names. It is not
Roblox cloud access. UpdateAsync provides locked atomic transforms with cancellation and rollback; see
[atomic stores](runtime-hardening.md#atomic-local-stores). Version history/key metadata,
cloud quotas, distributed storage and cloud failure semantics remain unsupported.
TCP clients can retain server-issued [reconnect credentials](runtime-hardening.md#reconnect-credentials);
these are local identities rather than external account login.

## Deliberate boundaries

- Humanoid has health and [basic single-collider movement](characters.md), not a full
  avatar rig or state machine. The viewer poses visual limbs procedurally.
  TakeDamage does not implement ForceField behavior.
- Raycast and Spherecast cover boxes and spheres. RaycastParams supports
  FilterDescendantsInstances, FilterType and RespectCanCollide. Terrain, meshes,
  CanQuery, collision groups, water and the Material result field remain absent;
  unsupported parameter fields are rejected. See [physics](physics.md).
- Teams does not yet implement TeamColor/AutoAssignable or automatic balancing.
  Neutral is currently derived from whether Team is nil.
- Random uses a deterministic local generator. Its seeded sequence does not claim
  bit-for-bit correspondence to Roblox's generator.
- JSON supports JSON-compatible values only. Sparse/mixed-key tables, Instances,
  non-finite numbers and engine datatypes are rejected.
- Attribute values support strings, booleans, numbers, Vector3, CFrame and Color3.
  Unsupported types, reserved RBX names and invalid names are rejected.
- BindableEvent currently uses the supported value codec. Arbitrary Lua function,
  coroutine and custom userdata payloads are not a general-purpose bridge.
- Heartbeat is a headless clock signal, not Roblox simulation-phase fidelity.
  An active subscription keeps the in-process scheduler alive; disconnect it
  when a finite CLI program should finish.
- Shutdown callbacks should finish their own awaited work. Detached tasks are not
  a guarantee of completion after the callback returns. Unresolvable waits can
  drain early under headless lifetime rules; runaway Luau code is interrupted by the VM budget.
- Dynamic script activation, a RunContext Enum subset, execution budgets, inbound
  limits and local reconnect credentials are covered in [runtime hardening](runtime-hardening.md).
  Broader Enum/datatype coverage, characters, broader physics, import, memory quotas,
  external account authentication and TLS remain separate milestones.

## Verification

The complete suite exercises the supported APIs. Tests include all requested entry paths,
invalid member access, unsupported values, attribute/tag change behavior, clone
independence, local and TCP replication, Color3 single-precision roundtrips,
store scope isolation, separate-process durability and concurrent increments,
yielding shutdown callbacks, and cleanup after Script destruction.

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore
dotnet run --project src/Woorf.Cli --no-restore -- docs/api-audit/probes/backend-readiness.luau
```

TCP protocol v8 carries the additional replicated backend state and Color3 values.
Older explicit protocol versions are rejected. Use matching server/client builds.

The [round game](../examples/round-game/README.md) also combines these APIs in a
separate-process acceptance test with leave/shutdown saves and a server restart.

## BadgeService stubs

BadgeService exposes the Roblox method names so imported scripts can start.
Award and ownership checks return false. Batch lookups return empty tables.
GetBadgeInfoAsync returns an empty name and description, icon ID 0, and
IsEnabled=false. IsDisabled returns true and IsLegal returns false. Calls
complete synchronously and have no persistent effects.

## BrickColor

Woorf implements the documented 208 named colors and 128 palette indices,
constructor overloads, named constructors, and random palette selection.
BrickColor values are immutable. BrickColorValue stores and replicates the
typed value and passes it to Changed. BasePart.BrickColor and BasePart.Color
share the part appearance. Team.TeamColor and Player.TeamColor use BrickColor.
Roblox place imports preserve supported BrickColor properties and values.

[Roblox BrickColor reference and palette](https://create.roblox.com/docs/reference/engine/datatypes/BrickColor)

## BindableFunction

BindableFunction.OnInvoke is a write-only callback. Invoke waits for a
callback, forwards the arguments, and returns the callback's results, including
nil positions in a return tuple. Callbacks can yield. Argument and return tables
are copied without metatables. Callback errors propagate to the invoker. Callback
registrations stay in the VM that assigned them.

[Roblox BindableFunction reference](https://create.roblox.com/docs/reference/engine/classes/BindableFunction)

## MarketplaceService game-pass stubs

UserOwnsGamePassAsync always returns false. PromptGamePassPurchase immediately
fires PromptGamePassPurchaseFinished with wasPurchased=false in the calling
runtime. No purchase UI, transactions, or entitlements are involved.

DataStore operations accept string keys and finite numeric keys from Luau.
Numeric keys use invariant round-trip formatting before lookup, so the numeric
user ID 123 and string "123" address the same local entry. Store names and
scopes still require strings.
