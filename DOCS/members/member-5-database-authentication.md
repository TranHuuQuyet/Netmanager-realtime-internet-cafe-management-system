# Member 5 - Database And Authentication

## Recovery Role

Ban own canonical SQLite auth/session runtime, seed data va account-to-`machineId` validation. Audit thay co hai huong persistence, nen recovery quyet dinh dung auth path trong `AuthBootstrapper` lam canonical truoc deadline.

## Write Scope

- `Code/ServerApp/Auth/`
- `Code/ServerApp/Database/` khi thay doi phuc vu canonical path duoc M1 approve
- Auth/data sections in `DOCS/API.md` and `DOCS/RUN_GUIDE.md`

## Non-Owned Scope

- Shared wire/TCP dispatcher va client registry cua M2.
- Server/client presentation behavior cua M3/M4.
- Gate/release approval cua M1 va verified test status cua M6.

## Dependencies

- Can M1 approve canonical schema, seed va session policy changes.
- Can M2 consume approved auth result shape through runtime dispatcher.
- Can M6 verify seed/reset, wrong-machine va session evidence.

## Boundary Rules

- Canonical runtime schema: `AuthUsers`, `AuthSessions`.
- Canonical recovery accounts: `admin` / `123` / `PC00`, `client01` / `123` / `PC-01`, `client02` / `123` / `PC-02`.
- Admin machine rule for recovery: `PC00` required.
- Broader `Users/Machines/Sessions` consolidation is retained post-core work, not a parallel integration path.
- Core online/offline status remains in-memory at networking layer.
- Provide `IAuthService`/session result behavior for M2; do not move socket logic into auth.
- Wrong machine, disabled account and server error must map deterministically to API error codes.
- Changes to seed/schema/session contract need M1 approval and M6 test updates.
- Nop auth/database feature/fix branch vao `develop`; khong merge truc tiep vao `main`, va cho M6 `Pass` truoc release promotion.

## Core Assignments

| Due | Task | Dependency | Required evidence |
| --- | --- | --- | --- |
| `2026-05-31` | Normalize canonical SQLite path/schema/seed/admin behavior and help restore server startup build | M1 decision | Auth handoff + `G0` evidence |
| `2026-06-07` | Provide runtime login and wrong-machine result through network call path | M2 dispatcher | `G2` cases pass |
| `2026-06-14` | Enforce active session/machine control guard | Authenticated command route | `G3` evidence |
| `2026-06-21` | Decide/test duplicate active login behavior | M1 approval, multi-client | `G4` evidence |
| `2026-06-28` | Verify seed/reset instructions for rehearsal | Stable core | `G5` setup result |
| `2026-07-05` | Support frozen demo seed/auth explanation and final blocker disposition | RC approved; M1/M6 request | Final auth/data report |

## Retained Extension Ownership

- `E6 Timer persistence` remains owned by M5 after timer display and session stability are proven.
- Future schema consolidation and customer/reporting persistence remain retained backlog after core release.

## Definition Of Done

- Canonical auth path is unambiguous and callable from network flow.
- Machine-bound login and session guard pass runtime tests.
- Deferred persistence work remains documented without destabilizing release scope.
