# DECISIONS

This file records project decisions that affect implementation, scope, or demo readiness.

## Decision Format

Each decision should include:

- date
- decision
- owner
- affected docs/code
- reason

## Accepted Decisions

### 2026-05-13 - Use Documentation-First Startup

- Owner: Member 1
- Affected docs/code: `DOCS/`, `Code/`
- Decision: start by freezing scope, ownership, API baseline, and source layout
- Reason: reduce overlap and contract drift before implementation begins

### 2026-05-13 - Use .NET 8, WinForms, TCP, SQLite, and System.Text.Json

- Owner: Member 1
- Affected docs/code: all runtime projects
- Decision: use `.NET 8`, C#, Windows Forms, TCP socket transport, SQLite, and `System.Text.Json`
- Reason: practical stack for an 8-week Windows LAN demo project

### 2026-05-13 - Use One JSON Object Per Line

- Owner: Member 2
- Affected docs/code: `DOCS/API.md`, `Code/Shared/`, networking modules
- Decision: each TCP packet is framed as one UTF-8 JSON object per line
- Reason: simple framing that is easy to debug and implement

### 2026-05-13 - Bind Each Client Account to One MachineId

- Owner: Member 5
- Affected docs/code: `DOCS/API.md`, auth/database modules, client login
- Decision: client login validates `username`, `password`, and `machineId`
- Reason: avoid demo confusion and prevent one account from representing multiple machines

### 2026-05-16 - Make Week 1 a Runnable Skeleton Week

- Owner: Member 1
- Affected docs/code: `DOCS/TASKS.md`, `DOCS/LEADER_FLOW.md`, `Code/`
- Decision: Week 1 must produce a buildable solution and a basic TCP packet round trip, not only docs
- Reason: the project cannot safely reach an 8-week demo if runnable integration starts too late

### 2026-05-25 - Replace Active 8-Week Cadence With Recovery Gates

- Owner: Member 1
- Affected docs/code: `DOCS/LEADER_FLOW.md`, `DOCS/TASKS.md`, `DOCS/TEST_MATRIX.md`, `DOCS/DEMO_CHECKLIST.md`, all runtime modules
- Decision: the active delivery plan is a six-week recovery plan from `2026-05-25` through the hard deadline `2026-07-05`
- Reason: audit found a broken solution build, no verified TCP round trip, no integration proof, and no passing runtime/demo tests near the end of original Week 2

### 2026-05-25 - Require Core Local Demo Before Extensions

- Owner: Member 1
- Affected docs/code: all project modules and demo documents
- Decision: completion by `2026-07-05` requires build, TCP local round trip, machine-bound login, status, lock/unlock, ACK/error, two-client local isolation, disconnect stability, and local rehearsal
- Reason: this is the minimum credible cafe-management control demo that can be stabilized within remaining time

### 2026-05-25 - Retain Important Extensions Behind Gates

- Owner: Member 1
- Affected docs/code: roadmap, tracker, API and feature modules
- Decision: notification, timer, chat, Real LAN, broadcast, timer persistence, and reconnect polish remain in the project as `Retained Extension Track`; they are not removed, but only open when the required core gate passes
- Reason: preserve important product scope without allowing unproven secondary work to break the deadline-critical path

### 2026-05-25 - Use Runtime Evidence For Completion

- Owner: Member 1 + Member 6
- Affected docs/code: `DOCS/TASKS.md`, `DOCS/TEST_MATRIX.md`, `DOCS/BUGS.md`
- Decision: `DOCS/TASKS.md` uses checkboxes for submitted member work/evidence; delivery completion still requires verified runtime/test results in `DOCS/TEST_MATRIX.md` and `DOCS/DEMO_CHECKLIST.md`; existing shells, drafts and unverified skeletons are historical artifacts or partial work
- Reason: allow simple member checklist updates without overstating demo readiness

### 2026-05-25 - Select Canonical Recovery Auth And Persistence Path

- Owner: Member 1 + Member 5
- Affected docs/code: `DOCS/API.md`, `DOCS/RUN_GUIDE.md`, `Code/ServerApp/Auth/`, `Code/ServerApp/Database/`
- Decision: the SQLite auth implementation centered on `AuthBootstrapper` and `AuthUsers/AuthSessions` is the canonical runtime path for the recovery delivery; machine status remains in-memory for core scope
- Reason: it is the closest available implementation path to usable authentication and avoids integrating two competing database directions before release

### 2026-05-25 - Freeze Recovery Demo Credentials And Admin Machine Rule

- Owner: Member 1 + Member 5
- Affected docs/code: `DOCS/API.md`, `DOCS/RUN_GUIDE.md`, auth implementation
- Decision: recovery seed baseline is `admin` / `123` / `PC00`, `client01` / `123` / `PC-01`, and `client02` / `123` / `PC-02`; admin login requires `PC00` in the recovery baseline
- Reason: align demo instructions with the selected canonical runtime path before network/UI integration

## Pending Decisions

These are retained decisions that must be closed only at their owning recovery gate:

- `R1/G0`: exact runtime database file location for canonical `AuthUsers/AuthSessions`, while retaining the selected SQLite path.
- `R2/G2`: heartbeat or status emission interval.
- `R4/G4`: duplicate active login behavior.
- `E7`: exact reconnect retry/backoff behavior after disconnect stability passes.
- Post-core: logging implementation and future consolidation from `AuthUsers/AuthSessions` to broader machine/customer schema.
