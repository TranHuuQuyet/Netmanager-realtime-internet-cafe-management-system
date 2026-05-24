# DOCS Implementation Ready

Day la startup brief cho team truoc va trong implementation.

## Source of Truth

- `LEADER_FLOW.md`: master working flow, ownership, working rules, 8-week phases, gates, definition of done.
- `TASKS.md`: tracker duy nhat co checkbox; member tick tien do tai day theo `Wn.Pm`.
- `API.md`: packet, auth, session, error, va shared model contract.
- `BUGS.md`: risks, unresolved issues, runtime bugs, accepted limitations.
- `RUN_GUIDE.md`: build, run, local mode, real LAN mode, seed account guide.
- `TEST_MATRIX.md`: gate-based test cases and test status.
- `DEMO_CHECKLIST.md`: core demo path, fallback path, demo roles.
- `DECISIONS.md`: accepted and pending implementation decisions.
- `members/`: member playbooks, khong tick task.

## Reading Order

1. `LEADER_FLOW.md`
2. `TASKS.md`
3. `API.md`
4. `members/README.md`
5. member-specific playbook
6. `RUN_GUIDE.md`, `TEST_MATRIX.md`, `DEMO_CHECKLIST.md`

## Confirmed Decisions

- `Code/` is the root for all source.
- Project duration baseline is 8 weeks.
- Flow labels are fixed as `W1.P1` through `W8.P3`.
- Stack is `.NET 8`, C#, Windows Forms, TCP, SQLite, and `System.Text.Json`.
- `Shared` holds DTOs, enums, constants, packet contract, and light parse/serialize helpers.
- `LOGIN` is shared for both admin and client.
- `STATUS` is both heartbeat and state-change update.
- `LOCK` / `UNLOCK` are server-driven; the client executes and returns `ACK` / `STATUS`.
- `CHAT` is direct 1-1 text only.
- `NOTIFICATION` is message plus light severity.
- Each client account is bound to one `machineId`.
- Project must support both real LAN demo mode and local multi-instance mode.

## Recommended Project Shape

```text
Code/
|-- NetManager.sln
|-- ServerApp/
|   |-- Forms/
|   |-- Auth/
|   `-- Database/
|-- ClientApp/
`-- Shared/
DOCS/
|-- API.md
|-- BUGS.md
|-- DECISIONS.md
|-- DEMO_CHECKLIST.md
|-- LEADER_FLOW.md
|-- README.md
|-- RUN_GUIDE.md
|-- TASKS.md
|-- TEST_MATRIX.md
`-- members/
```

## 8-Week Flow Summary

- Week 1: `W1.P1` kickoff/ownership, `W1.P2` API contract, `W1.P3` scaffold/first round trip.
- Week 2: `W2.P1` network core, `W2.P2` auth/DB core, `W2.P3` UI skeleton completion.
- Week 3: `W3.P1` login integration, `W3.P2` status sync, `W3.P3` one-client core demo.
- Week 4: `W4.P1` lock/unlock, `W4.P2` ACK/error handling, `W4.P3` control regression.
- Week 5: `W5.P1` notification, `W5.P2` timer, `W5.P3` 1-1 chat.
- Week 6: `W6.P1` multi-client, `W6.P2` stability hardening, `W6.P3` demo mode validation.
- Week 7: `W7.P1` regression/bug triage, `W7.P2` source/docs cleanup, `W7.P3` release candidate.
- Week 8: `W8.P1` final rehearsal, `W8.P2` release lock, `W8.P3` demo/archive.

## Team Ownership Summary

- M1: scope, rules, flow, gate, release approval.
- M2: networking core and packet contract.
- M3: server/admin UI.
- M4: client UI and client-side reactions.
- M5: auth, DB, session, seed data.
- M6: testing, bugs, run guide, demo checklist, docs consistency.

## Current Implementation Notes

- `Code/NetManager.sln`, `ServerApp`, `ClientApp`, and `Shared` project files exist.
- `ServerApp` has login/dashboard shell work in place.
- `ClientApp` still needs real client workflow screens.
- TCP runtime, auth/database runtime, and real end-to-end flow are still pending.
- Current tracker status is in `TASKS.md`.
