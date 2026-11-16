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
| Persistence | Server-only GetDataStore plus GetAsync/SetAsync/RemoveAsync/IncrementAsync against a durable local file backend |
| Shutdown | BindToClose callbacks run concurrently, may yield, and get up to 30 seconds; completion is based on registered callbacks rather than unrelated subscriptions |
| Teams | Team creation, Teams:GetTeams, Team:GetPlayers, Player.Team assignment and derived read-only Neutral state |
| Humanoid | Health/MaxHealth, TakeDamage, HealthChanged and Died; health-only headless state |
| Raycast | Nearest ray intersection against oriented box Parts; immutable RaycastResult with Instance, Position, Normal and Distance |
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
Roblox cloud access. UpdateAsync, version history/key metadata, quotas/budgets,
distributed storage, authenticated account identity and cloud failure semantics
remain unsupported. Local session UserIds are not durable authenticated accounts.

## Deliberate boundaries

- Humanoid has health behavior, not a character rig, state machine, movement,
  animation or physics. TakeDamage does not implement ForceField behavior.
- Raycasts cover current box Parts. RaycastParams, filtering, terrain, meshes,
  collision groups and the Material result field are not implemented. Passing
  RaycastParams is rejected rather than silently ignored.
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
  drain early under headless lifetime rules; non-yielding code is not preempted.
- Script activation rules, Enum coverage, additional datatype helpers, characters,
  physics, import, authentication and execution quotas remain separate milestones.

## Verification

The complete suite passes 160 checks. Tests include all requested entry paths,
invalid member access, unsupported values, attribute/tag change behavior, clone
independence, local and TCP replication, Color3 single-precision roundtrips,
store scope isolation, separate-process durability and concurrent increments,
yielding shutdown callbacks, and cleanup after Script destruction.

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore
dotnet run --project src/Woorf.Cli --no-restore -- docs/api-audit/probes/backend-readiness.luau
```

TCP protocol v3 carries the additional replicated backend state and Color3 values.
Older explicit protocol versions are rejected. Use matching server/client builds.
