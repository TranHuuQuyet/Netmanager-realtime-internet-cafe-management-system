# DOCS Implementation Ready

This is the startup brief for the team before implementation begins.

## Confirmed Decisions

- `Code/` is the root for all source.
- The project duration baseline is `8 weeks`.
- The recommended stack is `.NET 8`, C#, Windows Forms, TCP, SQLite, and `System.Text.Json`.
- `Shared` holds DTOs, enums, constants, packet contract, and light parse/serialize helpers.
- `LOGIN` is shared for both admin and client.
- `STATUS` is both heartbeat and state-change update.
- `LOCK` / `UNLOCK` are server-driven; the client executes immediately and returns `ACK` / `STATUS`.
- `CHAT` is direct 1-1 text only.
- `NOTIFICATION` is message plus light severity.
- Each client account is bound to one `machineId`.
- The project must support both real LAN demo mode and local multi-instance mode.
- Network stability comes before polish.
- UI should stay clean, clear, and runnable from the start.

## Recommended Project Shape

```text
Code/
|-- ServerApp/
|   |-- Auth/
|   `-- Database/
|-- ClientApp/
|-- Shared/
DOCS/
|-- API.md
|-- TASKS.md
|-- LEADER_FLOW.md
|-- BUGS.md
|-- RUN_GUIDE.md
|-- TEST_MATRIX.md
|-- DEMO_CHECKLIST.md
|-- DECISIONS.md
`-- members/
```

## What We Build First

1. Scope, stack, identity rule, and contract freeze.
2. Solution and project scaffold under `Code/`.
3. `Shared` contract and packet helpers.
4. TCP connect / send / receive loop.
5. Login and auth/session flow with `machineId` validation.
6. `STATUS` heartbeat plus state change sync.
7. `LOCK` / `UNLOCK` with client ACK handling.
8. `NOTIFICATION`, timer, and minimal 1-1 chat.
9. WinForms shell wiring and integration.
10. Reconnect, disconnect, invalid packet handling, and demo-mode validation.

## Ownership Summary

- Member 1: scope, rules, and integration control
- Member 2: networking core and packet contract
- Member 3: server UI
- Member 4: client UI
- Member 5: auth and session storage
- Member 6: testing and documentation

## Team Rule Set

- One module, one owner.
- One commit, one job.
- Branch format is `member-<number>/<short-task>`, `integration/<flow-name>`, or `hotfix/<short-fix>`.
- Commit format is `<scope>: <short action>`.
- Keep packet changes documented in the same session.
- Do not let server and client diverge on schema.
- Keep runtime code inside `Code/`.
- Keep chat scope minimal unless Member 1 approves a scope change.
- Do not allow one client account to log in as another machine.

## Demo Modes

- `Mode A - Real LAN Demo`: one server machine and multiple real clients in the same LAN
- `Mode B - Local Multi-Instance Demo`: one machine running the server and multiple client instances for development and fallback demo use

## Reading Order

1. `LEADER_FLOW.md`
2. `TASKS.md`
3. `API.md`
4. `members/README.md`
5. member-specific role file

## Source of Truth

- `LEADER_FLOW.md`: phase, scope, ownership, branch rules, commit rules, and release decisions
- `API.md`: packet, auth, and session contract
- `TASKS.md`: current execution status and next actions
- `BUGS.md`: risks, unresolved issues, and runtime bugs
- `RUN_GUIDE.md`: build, run, local mode, real LAN mode, and seed account guide
- `TEST_MATRIX.md`: gate-based test cases and current test status
- `DEMO_CHECKLIST.md`: core demo path, secondary demo path, and fallback scope
- `DECISIONS.md`: accepted and pending implementation decisions

## First Week Goal

- A `.sln` and the three project files are created.
- `ServerApp` and `ClientApp` reference `Shared`.
- A client can connect.
- At least one JSON-line packet can round-trip.
- A login packet shape is implemented or stubbed through the agreed shared model.
- The server can see one client status.
- The client can receive one server command.
- The UI stays responsive.
- The team agrees on stack, machine identity, and demo modes.

## Definition of Done Before Coding Deeply

- `Code/` structure is in place.
- planned solution and project generation path is clear.
- stack is frozen
- `API.md` baseline is accepted
- account-to-`machineId` rule is accepted
- team ownership is accepted
- both demo modes are documented
