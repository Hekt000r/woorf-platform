# Headless round game

A small backend workload combining persistent profiles, currency, tagged inventory,
teams, a Heartbeat-driven round and validated RemoteFunction requests. Clients
automatically play one round; there is no renderer or character simulation.

From the repository root, start a server:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/round-game --listen 127.0.0.1:7777
```

Start two clients in separate terminals with separate credential files:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- --connect 127.0.0.1:7777 --identity-file .woorf/round-alpha.json
```

After that client prints JOINED, run:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- --connect 127.0.0.1:7777 --identity-file .woorf/round-beta.json
```

Both players must ready before the 0.1-second demo round starts. Each receives
10 coins and one completed round, then buys one potion for 3 coins. The server
rejects early purchases, unknown items, malformed payloads, extra arguments,
client-supplied rewards and repeated ready/purchase requests. Clients assert both
remote replies and replicated profile, team, attribute, tag and ValueObject state.
Teams are balanced by current team membership.

On a fresh server identity registry, the profile-1 client leaves after completion;
the other stays connected. Stop the server with Ctrl+C to exercise its yielding
BindToClose hook. Restart and reuse both credential files, in either connection
order. Each profile gains another 7 coins, one round and one potion.

## Identity, persistence and lifecycle

The server issues local bearer credentials and derives profile keys from its stable
UserIds. It no longer maps connection slots to named test profiles. Credential files
must be kept private; there is no TLS or external account login. Without a retained
identity file, a client joins as a new guest. See the [identity contract](../../docs/runtime-hardening.md#reconnect-credentials).

Profile data lives in examples/round-game/.woorf/data, and the credential registry
lives in that project's .woorf/auth. Keep them together for backups. The former
RoundGameTestProfiles slot-based store is not migrated into credential-owned
profiles. For a fresh demo, use a new project copy without its .woorf directory and
new client credential files.

Rewards and purchases use UpdateAsync transactions, including the balance check
inside the purchase transaction. Currency, rounds and inventory are durable when
the operation succeeds. Leave/shutdown hooks atomically record LastSave metadata
without writing an old balance snapshot over newer progress. Forced termination
cannot run those hooks; it does not undo already committed purchases/rewards.

There is one two-player round per server session. The transport supports credential
reconnect, but this demo does not automatically resume a completed round or reconnect
a client process. A departure during a running round returns it to waiting. It has
no distributed session ownership, retry policy or production game-session recovery.

## Automated acceptance test

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore
```

The suite copies the real project to a temporary directory and starts a separate
server process using SocketTransport plus two actual CLI clients. It waits for a
leave save, stops the server gracefully while the other player remains connected,
then restarts with the same credential files in reversed connection order. It
checks literal totals: 7 coins / 1 round / 1 potion, then 14 / 2 / 2, and verifies
leave/shutdown metadata. Temporary data is isolated from manual-run saves.

Additional tests cover same-server reconnect, forged/duplicate credential rejection,
concurrent composite profile updates, dynamic script lifecycle, VM budgets and
inbound floods. This bounded workload does not establish arbitrary-game support.
