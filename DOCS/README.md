# DOCS - RECOVERY SOURCE OF TRUTH

Active recovery baseline date: `2026-05-25`
Core delivery deadline: `2026-07-05`

## Reading Order

1. `RECOVERY_REPORT_2026-05-25.md` - audit evidence and old-checkbox disposition.
2. `DECISIONS.md` - approved recovery decisions and remaining gate-time decisions.
3. `LEADER_FLOW.md` - active six-week workflow, ownership and gate controls.
4. `TASKS.md` - active status tracker; only `Verified Pass` counts.
5. `API.md` - packet/auth/data contract target.
6. `TEST_MATRIX.md`, `BUGS.md`, `DEMO_CHECKLIST.md`, `RUN_GUIDE.md`.
7. `members/` - recovery assignments for each member.

## Delivery Model

- Core local demo is mandatory before `2026-07-05`.
- Important secondary features are kept in `Retained Extension Track` and opened only after required core gates pass.
- Previous 8-week tasks are retained as audit history, not active completion evidence.
- A docs draft, UI shell or code skeleton does not count as delivered runtime behavior.

## Current Verified Baseline

- Full solution build fails in the audited working tree.
- There is no verified TCP round-trip or integrated login/control path.
- Runtime and demo test gates begin as `Fail` or `Blocked`.
- Recovery execution starts with `R1 Foundation Repair`.
