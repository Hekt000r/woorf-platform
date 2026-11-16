# Woorf continuation context

Updated: 2026-11-16.

Read [context-2.md](context-2.md) for the current continuation context, completed
milestones, verification baseline, working-tree precautions and proposed next work.
It supersedes the historical milestone notes previously held in this file.

Current technical references:

- [Runtime compatibility](docs/runtime-compatibility.md)
- [Backend API subsets and persistence contract](docs/backend-apis.md)
- [Backend readiness audit and roadmap](docs/backend-api-readiness.md)
- [Client/server runtime](docs/client-server.md) and [TCP transport](docs/socket-transport.md)
- [Scheduler](docs/scheduler.md) and [filesystem projects](docs/project-format.md)
- [Build and test commands](README.md)
- [Project goals and open decisions](project.md)

The representative headless round game is implemented with a separate-process
restart acceptance test. See examples/round-game/README.md. Backend/runtime
readiness remains the priority.

The finishing milestone adds dynamic script lifecycle, VM budgets, inbound limits,
atomic UpdateAsync and saved TCP credentials. See [runtime hardening](docs/runtime-hardening.md).

Basic custom managed box physics is now implemented; see [physics](docs/physics.md).
The user prefers a custom solver and explicitly rejects per-platform build/restore checks.
