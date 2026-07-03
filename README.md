# NetManager

NetManager is a .NET 8 Windows Forms internet-cafe management project targeting server/client control over TCP with SQLite-backed authentication.

## Current Reality - 2026-07-01

The project is a local release candidate, but it is not yet a fully accepted final demo.

- The solution builds in Debug and Release with `0` warnings and `0` errors on the audited local machine.
- `ContractSmoke`, `Auth_Test` and `NetworkSmokeTest` pass locally.
- Local runtime evidence covers TCP JSON-line login/status, lock/unlock with typed ACK/error results, two distinct local clients, selected-client chat, billing TIMER sync, expiry LOCK and billing restore/resync smoke paths.
- Required physical LAN rehearsal with two real client machines has not been executed in this workspace.
- Manual WinForms release rehearsal and M1/M6 release approval remain required before the project can be called final.

See [DOCS/RECOVERY_REPORT_2026-05-25.md](DOCS/RECOVERY_REPORT_2026-05-25.md) for the audit baseline and recovery decision.

## Recovery Delivery Target

Deadline: `2026-07-05`.

The required release path is a stable local multi-instance and physical LAN demo:

- buildable solution;
- TCP JSON-line local communication;
- machine-bound client login;
- online/offline status;
- lock/unlock command handling;
- ACK/error visibility;
- two distinct local clients;
- selected-client 1-1 chat;
- timed/open-ended billing with SQLite `BillingSessions` restore;
- minimal restart/reconnect billing resync;
- two physical LAN clients;
- disconnect stability;
- two rehearsed local release builds.

Important product features are retained behind gates rather than removed:

- direct notification and notification broadcast have local smoke evidence;
- reconnect polish remains retained/conditional.

Timer display, 1-1 chat, Real LAN and billing persistence were promoted into the required demo scope on `2026-06-09`.

Extension status is tracked in [DOCS/TASKS.md](DOCS/TASKS.md) and [DOCS/TEST_MATRIX.md](DOCS/TEST_MATRIX.md).

## Branch And Promotion Rule

- Members create feature/fix branches from `testing_branch` and open merge requests or pull requests back into `testing_branch`.
- `testing_branch` is the shared integration branch on which M6 tests candidate behavior and records evidence.
- `main` is reserved for accepted code: only a `testing_branch` candidate with an M6 `Pass` record and M1 approval is merged into `main`.
- Failed or blocked candidates are corrected through the feature/fix to `testing_branch` flow and do not enter `main`.

## Solution Shape

```text
Code/
|-- NetManager.sln
|-- ServerApp/
|   |-- Auth/
|   |-- Database/
|   |-- Forms/
|   `-- Networking/
|-- ClientApp/
|   |-- Forms/
|   `-- Networking/
`-- Shared/
DOCS/
|-- API.md
|-- BUGS.md
|-- DECISIONS.md
|-- DEMO_CHECKLIST.md
|-- LEADER_FLOW.md
|-- RECOVERY_REPORT_2026-05-25.md
|-- RUN_GUIDE.md
|-- TASKS.md
`-- TEST_MATRIX.md
```

## Recovery Architecture Target

- `Shared` owns the packet wire contract from [DOCS/API.md](DOCS/API.md).
- Server target: TCP listener, authenticated connection registry, typed dispatcher, auth/command handlers and UI event bridge.
- Client target: connection service, typed incoming event handling, lock/unlock execution and ACK sender.
- Forms render state and call services; they do not parse raw packets or access SQLite.
- Canonical recovery auth path is SQLite `AuthUsers/AuthSessions` through the current auth bootstrap direction.
- Online/offline machine state remains in memory for core delivery.

## Recovery Demo Credentials

| Role | Username | Password | MachineId |
| --- | --- | --- | --- |
| Admin | `admin` | `123` | `PC00` |
| Client | `client01` | `123` | `PC-01` |
| Client | `client02` | `123` | `PC-02` |

These values are recovery demo defaults and must be verified by the authentication gate before use in a release rehearsal.

## Build And Run Status

Approved build target command, from repository root:

```powershell
dotnet build Code/NetManager.sln
```

The repository root contains `global.json` pinning the .NET SDK to `8.0.421`, matching the project target and the current run guide. Any build or smoke-test failure should be recorded in DOCS/BUGS.md and investigated before proceeding to later gates.

Approved local verification command:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\Verify-LocalRelease.ps1
```

This runs restore, Debug build, Release build, contract smoke, auth/billing smoke and network smoke from one release-check entry point.

## Active Documents

- [DOCS/LEADER_FLOW.md](DOCS/LEADER_FLOW.md): six-week recovery flow and gate policy.
- [DOCS/TASKS.md](DOCS/TASKS.md): member submission checklist; a tick is not a verified gate pass.
- [DOCS/API.md](DOCS/API.md): recovery contract `v0.2`.
- [DOCS/TEST_MATRIX.md](DOCS/TEST_MATRIX.md): actual gate status and evidence.
- [DOCS/DEMO_CHECKLIST.md](DOCS/DEMO_CHECKLIST.md): core and retained extension demo paths.
- [DOCS/BUGS.md](DOCS/BUGS.md): active blocker register.
- [DOCS/DECISIONS.md](DOCS/DECISIONS.md): accepted recovery decisions.

## Delivery Rule

The project may be reported as `Core Demo Completed by 2026-07-05` only when core gates `G0-G5` pass with evidence and the local demo has passed two release-candidate rehearsals. Retained extensions remain part of NetManager whether they pass before the core release or continue afterward.
