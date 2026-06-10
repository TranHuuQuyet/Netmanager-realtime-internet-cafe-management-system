# NetManager - Realtime Internet Cafe Management System

Repository audit date: `2026-06-10`

NetManager is a `.NET 8` Windows Forms project for managing an internet cafe from an Admin/Server application and multiple machine-bound Client applications. The recovery target is a real-time TCP JSON-line system with SQLite-backed authentication, admin control, billing/session tracking and a required LAN demo.

This README reflects the active recovery plan, not the superseded original 8-week roadmap. The detailed source of truth remains in [`DOCS/`](DOCS/README.md), especially [`TEST_MATRIX.md`](DOCS/TEST_MATRIX.md), [`BUGS.md`](DOCS/BUGS.md), [`DECISIONS.md`](DOCS/DECISIONS.md) and [`RUN_GUIDE.md`](DOCS/RUN_GUIDE.md).

## Project Overview

NetManager targets:

- Server management for an internet-cafe room.
- Client login bound to a documented `machineId`.
- TCP real-time communication using one UTF-8 JSON object per line.
- SQLite authentication through the canonical recovery auth path.
- Admin control for status, lock/unlock and command result visibility.
- Billing/session monitoring and restore.
- Required local multi-instance and two-physical-client LAN demo.

## Current Project Status

The table below uses the current repository audit plus the active recovery status in [`DOCS/TEST_MATRIX.md`](DOCS/TEST_MATRIX.md) and [`DOCS/BUGS.md`](DOCS/BUGS.md). A status is not marked `Pass` unless there is evidence.

| Area | Status |
| --- | --- |
| Build | `Fail in current audit`: `dotnet build Code/NetManager.sln` restores packages but fails in `Code/NetworkSmokeTest/Program.cs(26,13)` because `PacketDispatcher` now requires `IAuthService`, `ISessionRepository` and `IMachineRepository`. `ServerApp` and `ClientApp` build individually. |
| API Contract | `Pass`: `ContractSmoke` passes `G0-02`, `G0-03` and `G0-04`; API `v0.2` remains the recovery contract. |
| Authentication | `Pass`: `Auth_Test` passes canonical seed, admin login, client login, wrong password and wrong-machine cases. |
| Login Flow | `Partial`: auth/login pieces exist and pass auth tests, but `B-003` still records no accepted integrated login/status/control demo. |
| Status Flow | `Fail/Open`: `TEST_MATRIX.md` keeps `G2-05` as `Fail` through `B-008`. Current code contains `STATUS` route/sender work, but the network smoke test is not currently buildable. |
| Lock/Unlock | `Blocked`: `G3` control tests are blocked by `B-003` and the unresolved status/control readiness path. |
| ACK | `Blocked`: command ACK visibility is a `G3` requirement and has no accepted runtime pass. |
| Multi Client | `Blocked`: `G4` local multi-instance identity/routing tests remain blocked. |
| Chat | `Blocked`: required by the `2026-06-09` decision, but only packet/UI shell artifacts are visible; no accepted `G5` chat evidence exists. |
| Billing | `Blocked`: required by the `2026-06-09` decision; no verified `BillingSessions` runtime/restore path exists in the active gate evidence. |
| LAN | `Blocked`: required two-physical-client LAN demo has no accepted evidence; current `ServerApp` starts TCP on `127.0.0.1:5000`. |
| Reconnect | `Blocked`: client auto-reconnect code exists, but required billing/timer resync after ServerApp restart has no accepted `G5` evidence. |

### Completed With Evidence

- API contract smoke for packet type serialization, `LOGIN` request/response split and top-level failure envelope.
- Canonical SQLite auth seed and machine-bound auth checks.
- Individual `ServerApp` and `ClientApp` project builds in this audit.
- Client startup/login code and server status bridge are present in source, but not accepted as full demo completion.

### Not Completed

- Current full solution build, because `NetworkSmokeTest` is out of sync with `PacketDispatcher`.
- Accepted integrated login/status/control demo.
- Lock/unlock/ACK command path.
- Two-client local isolation.
- Physical LAN demo.
- Required 1-1 chat.
- Required billing/session monitor, SQLite restore and minimal reconnect/resync.

## Recovery Roadmap

| Recovery stage | Status | Current note |
| --- | --- | --- |
| `R1 Foundation Repair` | `Completed with regression` | `G0/G1` were verified historically, but current solution build is regressed by the `NetworkSmokeTest` compile error. |
| `R2 Authenticated Status` | `In Progress` | Login/auth cases pass; `G2-05` status remains open in `TEST_MATRIX.md` through `B-008`. |
| `R3 Core Control` | `Blocked` | Requires accepted status policy and lock/unlock/ACK routing. |
| `R4 Multi Client` | `Blocked` | Requires `G3` pass before two-client routing, disconnect stability and duplicate-login behavior can be accepted. |
| `R5 Release Readiness` | `Blocked` | Requires local regression, physical LAN, chat, billing, restore and minimal resync evidence. |
| `R6 Demo` | `Blocked` | Requires `G0` through `G5` pass and release-candidate rehearsals. |

## Architecture

Target recovery architecture:

```text
ClientApp
  |
  | TCP JSON-Line
  v
ServerApp
  |
  v
Auth Service
  |
  v
SQLite
```

- `Code/Shared/` owns packet enums, DTOs, packet factory, JSON helper behavior and the API wire contract.
- `Code/ServerApp/` owns the WinForms admin UI, TCP listener, dispatcher wiring, status bridge and auth composition root.
- `Code/ClientApp/` owns the WinForms client UI, TCP client connection, login request and client status/reconnect behavior.
- `Code/ServerApp/Auth/` owns canonical authentication and session service behavior.
- `Code/ServerApp/Database/` owns SQLite bootstrap, `AuthUsers`, `Machines` and `AuthSessions`.
- Forms should consume typed services/events. They should not parse raw JSON or access SQLite directly.

Current implementation notes:

- `ServerApp` currently starts `TcpJsonLineServer` on `127.0.0.1:5000` after admin login.
- `PacketDispatcher` currently accepts `LOGIN` and `STATUS`.
- `LOCK`, `UNLOCK`, `ACK`, `CHAT` and `TIMER` packet models exist in `Shared`, but accepted runtime routing for the required demo is not complete.
- `BillingSessions` is a required target from docs, but is not present in the current SQLite schema shown by `DatabaseSchema.sql`.

## Technology Stack

- `.NET 8`
- C#
- Windows Forms
- TCP sockets
- SQLite
- `Microsoft.Data.Sqlite`
- `System.Text.Json`

## API Summary

The active API contract is [`DOCS/API.md`](DOCS/API.md), version `v0.2`.

| Packet | Purpose | Current acceptance |
| --- | --- | --- |
| `LOGIN` | Machine-bound admin/client authentication | Auth evidence exists. |
| `STATUS` | Online/offline machine state updates | Open in active matrix through `B-008`. |
| `LOCK` | Admin locks a selected client | Blocked under `G3`. |
| `UNLOCK` | Admin unlocks a selected client | Blocked under `G3`. |
| `ACK` | Client/server result acknowledgment | Blocked under `G3`. |
| `CHAT` | Required 1-1 Admin/client chat | Blocked under `G5`. |
| `TIMER` | Required billing/timer display and expiry behavior | Blocked under `G5`. |

`NOTIFICATION` remains a retained extension packet and is not a replacement for the required `CHAT` path.

## Demo Scope (Current Required Scope)

The `2026-06-09` decision promoted LAN, chat and billing persistence into required demo scope. These are not optional extensions.

Required:

- Two physical LAN clients.
- Local multi-instance regression/fallback.
- 1-1 Admin/client chat.
- Admin-owned timed and open-ended billing.
- SQLite billing restore after ServerApp restart.
- Minimal reconnect/resync so running clients can sync billing/timer and receive extend/`LOCK` after restart.

Local multi-instance does not replace the physical LAN requirement, and physical LAN does not replace local regression.

## Demo Accounts

| Role | Username | Password | MachineId |
| --- | --- | --- | --- |
| Admin | `admin` | `123` | `PC00` |
| Client | `client01` | `123` | `PC-01` |
| Client | `client02` | `123` | `PC-02` |

Admin login requires `PC00` in the recovery baseline. Client accounts are bound to their assigned `machineId`.

## Build Instructions

Approved build command from repository root:

```powershell
dotnet build Code/NetManager.sln
```

Current audit result: this command currently fails because `NetworkSmokeTest` does not compile against the current `PacketDispatcher` constructor. Fix that regression before claiming `G0` as current pass.

Useful project-level checks:

```powershell
dotnet build Code/ServerApp/ServerApp.csproj --no-restore
dotnet build Code/ClientApp/ClientApp.csproj --no-restore
```

Both project-level build commands passed in the `2026-06-10` audit after package restore.

## Run Instructions

Run commands from the repository root so the recovery SQLite path stays unambiguous.

Start ServerApp:

```powershell
dotnet run --project Code/ServerApp/ServerApp.csproj
```

Log in as:

```text
admin / 123 / PC00
```

After accepted admin login, the current code starts the TCP server on `127.0.0.1:5000`.

Start local client `PC-01`:

```powershell
dotnet run --project Code/ClientApp/ClientApp.csproj -- --machine-id PC-01 --server-host 127.0.0.1 --server-port 5000
```

Start local client `PC-02`:

```powershell
dotnet run --project Code/ClientApp/ClientApp.csproj -- --machine-id PC-02 --server-host 127.0.0.1 --server-port 5000
```

For the required LAN demo, the documented target is:

```powershell
dotnet run --project Code/ClientApp/ClientApp.csproj -- --machine-id PC-01 --server-host <server-lan-ip> --server-port 5000
```

The LAN path is still blocked because the accepted `G5` evidence does not exist and current server startup binds loopback.

## Testing

Run contract smoke:

```powershell
dotnet run --project Code/ContractSmoke/ContractSmoke.csproj --no-restore
```

Current audit result: passed.

Run network smoke:

```powershell
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj
```

Current audit result: fails to compile at `Program.cs(26,13)` because the test still calls `new PacketDispatcher(authRuntime.Auth)` while the current constructor requires auth, session repository and machine repository.

Run auth smoke:

```powershell
dotnet run --project Code/Auth_Test/Auth_Test.csproj
```

Current audit result: passed `G0-05`, `G2-01`, `G2-02`, `G2-03` and `G2-04`.

## Open Bugs

| Bug | Severity | Impact | Owner | Gate |
| --- | --- | --- | --- | --- |
| `B-003` | `Critical` | No accepted integrated login/status/control demo; blocks runtime proof for UI -> TCP -> auth and command/ACK flow. | M2 + M3 + M4 + M5 | `R2-R3` |
| `B-008` | `High` | Active matrix records missing/failed client-sent `STATUS` path; blocks clean `G2` status acceptance. | M2 + M4 | `R2/G2` |
| `B-009` | `Critical` | Promoted required LAN/chat/billing persistence scope lacks verified runtime evidence. | M2 + M3 + M4 + M5 + M6; M1 approves | `R4-R6/G5` |

## Branch Workflow

Required flow:

```text
feature/fix branch
  -> develop
  -> M6 verify integrated candidate
  -> M1 approve promotion
  -> main
```

Member feature branches must not merge directly into `main`. A checked task in [`DOCS/TASKS.md`](DOCS/TASKS.md) records submitted work/evidence only; it is not a runtime pass.

## Team Structure

| Member | Responsibility |
| --- | --- |
| M1 | Scope, architecture decisions, gate order, promoted-scope approval and release promotion. |
| M2 | Shared packet contract, TCP, dispatcher, routing, LAN/chat/timer transport and minimal reconnect/resync support. |
| M3 | Server/admin UI, status display, command result UI, admin chat UI and billing monitor. |
| M4 | Client UI, login binding, lock/unlock reaction, ACK, chat, timer/billing display and minimal resync UI. |
| M5 | Canonical SQLite auth/session, seed data, machine validation, command guard and `BillingSessions` restore target. |
| M6 | Test evidence, bugs, run/demo docs, local/LAN/chat/billing rehearsal reporting and final continuation status. |

## Release Criteria

The project is complete only when:

- `G0` through `G5` are marked `Pass` with evidence.
- Required local multi-instance regression passes.
- Required two-physical-client LAN demo passes.
- Required 1-1 chat passes.
- Required timed/open-ended billing passes.
- Required SQLite billing restore passes.
- Required minimal reconnect/resync passes.
- No unaccepted High/Critical blocker remains.
- README, run guide, test matrix, demo checklist and limitation report are aligned to the release candidate.

Until then, NetManager is an active recovery project, not a completed demo release.
