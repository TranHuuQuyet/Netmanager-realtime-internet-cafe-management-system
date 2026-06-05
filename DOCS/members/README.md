# MEMBER PLAYBOOKS - RECOVERY DELIVERY

Active window: `2026-05-25` to `2026-07-05`.
Read `../RECOVERY_REPORT_2026-05-25.md`, `../LEADER_FLOW.md` and `../TASKS.md` before taking new work.

## Shared Rules

- Core local demo is mandatory; retained extensions remain part of the project but are gate controlled.
- Tick work in `../TASKS.md` after the member submits the required work/evidence; a tick is not a runtime pass.
- Runtime and gate completion are counted only from verified results in `../TEST_MATRIX.md` and `../DEMO_CHECKLIST.md`.
- Do not claim shell, draft or local uncommitted artifact as runtime delivery.
- Packet/schema changes follow `../API.md` and require owner approval.
- Feature work must follow the dependency and gate order in `../LEADER_FLOW.md`.
- Moi member tao feature/fix branch tu `develop` va nop PR/merge vao `develop`; khong merge feature truc tiep vao `main`.
- `develop` la nhanh integration de M6 test; chi candidate duoc M6 ghi `Pass` va M1 approve moi duoc merge/promote vao `main`.

## Standard Playbook Form

Every member playbook uses the same planning form in this order:

1. `Recovery Role` - delivery responsibility for the recovery window.
2. `Write Scope` - files or areas the member may change.
3. `Non-Owned Scope` - boundaries that require handoff rather than silent edits.
4. `Dependencies` - upstream inputs or approvals needed before delivery.
5. `Boundary Rules` - contract, evidence and integration limits.
6. `Core Assignments` - a table with `Due`, `Task`, `Dependency` and `Required evidence`.
7. `Retained Extension Ownership` - gated secondary scope only.
8. `Definition Of Done` - evidence-based completion criteria for the member.

The playbooks describe ownership and handoff. Current submission status remains in `../TASKS.md`; runtime pass/fail status remains in `../TEST_MATRIX.md` and `../DEMO_CHECKLIST.md`.

## Files

- `member-1-team-leader-system-architect.md`
- `member-2-network-engineer.md`
- `member-3-server-gui-developer.md`
- `member-4-client-app-developer.md`
- `member-5-database-authentication.md`
- `member-6-tester-documentation.md`

## Handoff Checklist

Each handoff includes interface/result shape, evidence or blocker, consumer, due date and affected gate. M6 verifies pass status on `develop`; M1 approves gate transitions, extension opening and promotion from `develop` to `main`.
