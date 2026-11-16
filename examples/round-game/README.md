# Headless round game

A small backend workload combining player profiles, currency, tagged inventory,
teams, a Heartbeat-driven round, validated RemoteFunction requests and persistence.
There is no renderer or character simulation. Clients automatically play one round.

From the repository root, start a server:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/round-game --listen 127.0.0.1:7777
```

Run this in two other terminals, starting the second after the first prints JOINED:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- --connect 127.0.0.1:7777
```

Both players must ready before the 0.1-second demo round starts. Each receives
10 coins and one completed round, then buys one potion for 3 coins. The server
rejects early purchases, unknown items, malformed payloads, extra arguments,
client-supplied rewards and repeated ready/purchase requests. Clients assert both
remote replies and replicated profile, team, attribute, tag and ValueObject state.

The first client leaves after completion; the second remains connected. Stop the
server with Ctrl+C to exercise its yielding BindToClose save. Restart the server
and connect two clients again: each profile gains another 7 coins, one round and
one potion. Data lives in examples/round-game/.woorf/data. For fresh profiles, use
a new copy of this project without its .woorf directory.

## Test identity and lifecycle limits

The server maps its first two connection slots to test-alpha and test-beta. These
are explicit demo identities, unrelated to Roblox accounts or authenticated users.
Further connection slots are kicked. Restart the server for another session;
in-session reconnect and identity recovery are outside this workload. The two
players are assigned Red and Blue teams by the server. A departure during a running
round returns it to waiting; this demo does not replace departed slots.

Profiles save on PlayerRemoving and during BindToClose. Forced termination cannot
run shutdown callbacks. There is no crash recovery, autosave, distributed profile
locking or production account authentication. Run one server per project data
folder. Errors loading or saving profiles are surfaced, without a retry policy.

## Automated acceptance test

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore
```

The suite copies the project to a fresh temporary directory and starts a separate
server process using the public SocketTransport API, plus two actual CLI clients.
After the first client's leave save, the test cancels the server gracefully while
the second remains connected. It then restarts that server against the same data
and checks literal totals: 7 coins / 1 round / 1 potion, then 14 / 2 / 2. Temporary
data is isolated from this example's manual-run saves. This is a bounded functional
workload, not a production-readiness or arbitrary-game compatibility claim.
