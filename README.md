# NetManager

NetManager is a .NET 8 Windows Forms internet-cafe management project targeting server/client control over TCP with SQLite-backed authentication.

## Current Reality - 2026-05-25

The project is in recovery, not feature-complete delivery.

- Docs, UI shells, shared packet artifacts, networking wrappers and auth/database code exist.
- The current working tree does not build: `ServerApp/Forms/LoginForm.Designer.cs(7,29)` fails because the server login code-behind is missing locally.
- No verified TCP JSON-line round-trip, integrated login/status flow, lock/unlock/ACK path or multi-client demo exists yet.
- The audit found `0/33` passing legacy runtime tests and no passing demo checklist step.
- Local client forms currently represent shell artifacts, not runtime integration proof.

See [DOCS/RECOVERY_REPORT_2026-05-25.md](DOCS/RECOVERY_REPORT_2026-05-25.md) for the audit baseline and recovery decision.

## Recovery Delivery Target

Deadline: `2026-07-05`.

The required release path is a stable local multi-instance core demo:

- buildable solution;
- TCP JSON-line local communication;
- machine-bound client login;
- online/offline status;
- lock/unlock command handling;
- ACK/error visibility;
- two distinct local clients;
- disconnect stability;
- rehearsed local release build.

Important product features are retained behind gates rather than removed:

- direct notification;
- timer display and later timer persistence;
- 1-1 text chat;
- Real LAN smoke validation;
- notification broadcast and reconnect polish.

Extension status is tracked in [DOCS/TASKS.md](DOCS/TASKS.md) and [DOCS/TEST_MATRIX.md](DOCS/TEST_MATRIX.md).

## Branch And Promotion Rule

- Members create feature/fix branches from `develop` and open merge requests or pull requests back into `develop`.
- `develop` is the shared integration branch on which M6 tests candidate behavior and records evidence.
- `main` is reserved for accepted code: only a `develop` candidate with an M6 `Pass` record and M1 approval is merged into `main`.
- Failed or blocked candidates are corrected through the feature/fix to `develop` flow and do not enter `main`.

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

This command is currently known to fail in the working tree and is the first core blocker. Runtime commands and seed/reset details are maintained in [DOCS/RUN_GUIDE.md](DOCS/RUN_GUIDE.md); do not claim a demo step passes until the corresponding gate is verified.

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
