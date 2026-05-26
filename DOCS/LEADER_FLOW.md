# LEADER FLOW - RECOVERY DELIVERY

## Purpose / Source Of Truth

Active delivery window: `2026-05-25` to `2026-07-05`.
Mandatory release result: stable `Core Local Demo`.
Important secondary features remain in the `Retained Extension Track`.

Source of truth:

- `DOCS/LEADER_FLOW.md`: active flow, ownership, gates, stop-work and release rules.
- `DOCS/TASKS.md`: active checklist for member work submitted with required evidence.
- `DOCS/RECOVERY_REPORT_2026-05-25.md`: audit baseline and disposition of previous checkbox progress.
- `DOCS/DECISIONS.md`: approved scope/contract/delivery decisions.
- `DOCS/API.md`: packet, auth, error and recovery persistence contract.
- `DOCS/BUGS.md`: active blockers, risks and accepted limitations.
- `DOCS/TEST_MATRIX.md`: runtime evidence and gate status.
- `DOCS/RUN_GUIDE.md`: approved setup and demo seed assumptions.
- `DOCS/DEMO_CHECKLIST.md`: core demo and retained extension presentation path.
- `DOCS/members/`: individual recovery assignments and handoff rules.

Rules:

- Previous 8-week checkboxes are historical evidence only; they no longer express active delivery completion.
- A checked item in `DOCS/TASKS.md` records submitted member work; it is not a runtime or gate pass.
- Delivery pass is counted only from verified results in `DOCS/TEST_MATRIX.md` and `DOCS/DEMO_CHECKLIST.md`, with M1 approval where a gate requires it.
- If code and docs differ, runtime is not approved until the owning contract/docs and tests are aligned.
- If a core gate is red, extension work cannot begin unless M1 records an exception that does not delay core recovery.

## Current Baseline

Audit date: `2026-05-25`.

- Full solution build is failing in the current working tree.
- No TCP JSON-line round-trip has been verified.
- No UI -> network -> auth integration proof exists.
- Legacy matrix has `0/33` tests marked `Pass`; demo checklist has no passed step.
- Client shell work and auth/network artifacts exist, but are not runtime-complete.
- The project is `Documentation-heavy but implementation-light` with `No integration proof yet`.

## Delivery Lanes

### Core Delivery Lane - Required By 2026-07-05

- Full solution build passes.
- TCP local server/client JSON-line connection and valid/invalid handling work.
- Client login validates username/password/`machineId`; wrong mapping fails visibly.
- Server receives and shows client online/offline status.
- Admin triggers `LOCK` and `UNLOCK`; client executes them.
- Client returns `ACK`; server shows result/error.
- Two local clients remain distinct.
- Disconnect does not crash the server.
- Local multi-instance demo rehearses successfully on release build.

### Retained Extension Lane - Kept In Project, Gate Controlled

| Priority | Feature | Open condition |
| --- | --- | --- |
| `E1` | Direct notification | `G3 Core Control` pass |
| `E2` | Timer display | `E1` pass and no open High/Critical core blocker |
| `E3` | 1-1 text chat | `G4 Multi-Client` pass by `2026-06-21` |
| `E4` | Real LAN smoke test | Local rehearsal pass by `2026-06-28` |
| `E5` | Notification broadcast | `E1` stable |
| `E6` | Timer persistence | `E2` and core session stable |
| `E7` | Reconnect polish | Disconnect stability pass |

Extension work not completed by `2026-07-05` becomes `Retained - Continue After Core Release`; it is not removed from the roadmap.

### Retained Product Backlog

- Customer CRUD.
- Shutdown control.
- Extra dashboard polish.
- Reporting and analytics.

These features do not enter development before `G5 Release` unless M1 formally changes the delivery commitment.

## Stack And Recovery Architecture

- Stack: `.NET 8`, C#, Windows Forms, TCP, SQLite, `System.Text.Json`.
- Transport: UTF-8 JSON-line packets following `DOCS/API.md` recovery baseline `v0.2`.
- Shared boundary: `Code/Shared/` owns wire types and light serializer/parser behavior.
- Server runtime target: listener -> authenticated connection registry -> typed dispatcher -> auth/command handler -> UI event bridge.
- Client runtime target: connection service -> login/status sender -> typed receiver -> command execution -> ACK sender -> UI binding.
- Auth/data target: SQLite implementation through `AuthBootstrapper`, with `AuthUsers/AuthSessions` canonical for recovery.
- Machine presence/status target: in-memory authenticated registry for core demo; no required status persistence.
- UI rule: forms do not parse raw JSON or call SQLite directly.

## Recovery Demo Identity

| Role | Username | Password | MachineId |
| --- | --- | --- | --- |
| Admin | `admin` | `123` | `PC00` |
| Client | `client01` | `123` | `PC-01` |
| Client | `client02` | `123` | `PC-02` |

- Admin requires `PC00` in the recovery baseline.
- A client account is bound to exactly one `machineId`.
- Wrong `machineId` must fail deterministically through the shared error contract.

## Team Ownership

| Member | Core delivery ownership | Retained extension ownership |
| --- | --- | --- |
| M1 | scope, API/auth decisions, dependency order, gate and release approval | open/continue/defer extension decision |
| M2 | shared wire implementation, TCP, dispatcher, login/status/control routing, client registry | notification/timer/chat routing, LAN/reconnect support |
| M3 | buildable server UI, status display, command action and ACK/error display | admin notification/timer/chat UI |
| M4 | buildable client UI, real login result, lock/unlock reaction and ACK | client notification/timer/chat UI |
| M5 | canonical SQLite auth/session, seed data, machine validation and session guard | timer persistence and extended session policy |
| M6 | test evidence, blockers, docs alignment, rehearsal reporting | extension verification and continuation report |

## Working And Handoff Rules

### Contract And Merge Rules

- Packet/schema changes require M2 proposal, M1 approval and M5 review when auth/session is affected.
- Auth/schema/seed changes require M5 proposal and M1 approval before M2 or UI owners consume them.
- Merge candidates for core runtime must build and carry the relevant gate evidence.
- Uncommitted local forms, docs or database files are artifacts, not delivered integration.

### Handoff Evidence

Before handoff, an owner provides:

- interface or packet/result shape;
- expected input/output and error behavior;
- build/test evidence or explicit blocker;
- next consumer and due date.

Receiving owners:

- consume the approved boundary only;
- do not silently change packet, seed or auth rules;
- report missing behavior to M1/M6 immediately.

### Stop-Work Rules

- Until `G0` passes, work is limited to build, canonical contract, canonical auth/database and round-trip foundation.
- Until `G3` passes, do not implement extension routing.
- Until `G4` passes, chat and LAN cannot be opened.
- From `2026-07-01`, only approved demo-blocker fixes may enter the release build.

## Active Six-Week Workflow

### R1 Foundation Repair - `2026-05-25` to `2026-05-31`

Goal: remove build and contract blockers and produce the first verified packet round-trip.

- M1: approve recovery decisions and gate rules.
- M2: align shared packet behavior with API, implement listener/dispatcher baseline and prove local valid/invalid handling.
- M3/M5: restore server build path and align auth startup.
- M4: preserve/integrate client forms as buildable shell only; no runtime completion claim until bound to service.
- M5: declare canonical auth database, seed, admin rule and auth handoff.
- M6: mark baseline fail/blocked states and verify `G0/G1`.

Gate: solution build passes, contract is approved, packet tests pass and local round-trip is verified.

### R2 Authenticated Status - `2026-06-01` to `2026-06-07`

Goal: one real client logs in and appears online/offline through actual runtime services.

- M2/M5: route `LOGIN` through canonical auth and map errors.
- M4: bind login UI to real client service.
- M2/M4: send status after authenticated login/disconnect.
- M3: show real status on dashboard.
- M6: verify valid/invalid/wrong-machine login and state update.

Gate: `G2 Auth & Status` passes.

### R3 Core Control - `2026-06-08` to `2026-06-14`

Goal: one-client command demo works repeatedly.

- M2/M3: route admin `LOCK`/`UNLOCK`.
- M4: execute lock/unlock state and send ACK.
- M3: display command result/error.
- M5: enforce active session/machine command guard.
- M6/M1: approve repeated one-client demo.

Gate: `G3 Core Control` passes. Passing this gate may open `E1 Direct Notification`.

### R4 Multi-Client And Notification - `2026-06-15` to `2026-06-21`

Goal: stabilize local multi-client identity and disconnect behavior.

- M2/M5: route two authenticated clients and settle duplicate-login behavior.
- M3/M4: display and maintain separated client states.
- M2/M6: verify disconnect stability.
- Extension team: implement direct notification only if opened.

Gate: `G4 Multi-Client` passes. If passed on time, M1 may open timer display and 1-1 chat.

### R5 Stabilization And Opened Extensions - `2026-06-22` to `2026-06-28`

Goal: obtain a reliable local rehearsal and finish only extensions already opened.

- All owners: close core blockers and run regression.
- M6: verify clean setup and local rehearsal.
- Extension owners: implement/test opened notification, timer or chat work.
- M2/M6: attempt LAN smoke only after local rehearsal passes.

Gate: `G5 Release` candidate is ready.

### R6 Release And Demo - `2026-06-29` to `2026-07-05`

Goal: freeze and deliver core safely while reporting retained extension state honestly.

- M1: approve RC and freeze by target date `2026-06-30`.
- M6/team: execute two rehearsals on the RC build.
- Team: deliver demo no later than `2026-07-05`.
- M6: record every extension as pass, opened/incomplete, or retained after release.

## Gate Summary

| Gate | Must pass before | Required outcome |
| --- | --- | --- |
| `G0 Build & Contract` | Further integration | Build pass and shared wire/auth contract verified |
| `G1 Network` | Login UI integration claim | Local listener/connector round-trip and invalid handling |
| `G2 Auth & Status` | Control work | Real login/machine validation/status path |
| `G3 Core Control` | Extension routing | Lock/unlock/ACK/error repeated demo |
| `G4 Multi-Client` | Optional chat/LAN opening | Two local clients and disconnect stability |
| `G5 Release` | RC approval | Regression, clean setup and local rehearsal |

## Deadline Guardrails

- If `R1` misses `2026-05-31`, all extension work remains closed.
- If `R3` misses `2026-06-14`, all extensions continue after the core release rather than consume demo stabilization time.
- If `R4` misses `2026-06-21`, the mandatory demo falls back to one-client local while two-client work remains retained.
- The intended code freeze is `2026-06-30`.

## Final Definition Of Done

`Core Demo Completed by 2026-07-05` requires:

- `G0` through `G5` marked `Pass` with evidence;
- two successful local rehearsals on the release candidate;
- no unaccepted High/Critical demo blocker;
- README, run guide, test matrix, demo checklist and limitation report aligned to release;
- retained extensions reported accurately without being erased from the roadmap.

## Historical Baseline - Superseded On 2026-05-25

The original eight-week sequence planned Week 1 scaffold/round-trip, Week 2 foundations, Week 3 login/status, Week 4 control, Week 5 notification/timer/chat, Week 6 multi-client and Week 7-8 release/demo. It is retained as project history in version control and summarized in `DOCS/RECOVERY_REPORT_2026-05-25.md`, but it is no longer the active schedule because its Week 1 runtime gate was not verified near the end of original Week 2.
