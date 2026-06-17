# Member 5 - Database And Authentication

## Recovery Role

Ban own canonical SQLite auth/session runtime, seed data, account-to-`machineId` validation va required billing/session persistence. Audit thay co hai huong persistence, nen recovery quyet dinh dung auth path trong `AuthBootstrapper` lam canonical truoc deadline; tu `2026-06-09`, `BillingSessions` restore la required demo scope.

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
- Can M3/M4 consume billing session state for Admin monitor and Client timer/amount display.
- Can M6 verify seed/reset, wrong-machine va session evidence.

## Boundary Rules

- Canonical runtime schema: `AuthUsers`, `AuthSessions`.
- Required billing schema target: `BillingSessions` stores active/closed billing, machine/client session reference, rental mode, start time, optional expiry time, status and rate per hour.
- Canonical recovery accounts: `admin` / `123` / `PC00`, `client01` / `123` / `PC-01`, `client02` / `123` / `PC-02`.
- Admin machine rule for recovery: `PC00` required.
- Broader `Users/Machines/Sessions` consolidation is retained post-core work, not a parallel integration path.
- Core online/offline status remains in-memory at networking layer.
- Billing/session monitor is not in-memory-only; active billing must restore after ServerApp restart.
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
| `2026-06-21` | Decide/test duplicate active login behavior and define billing session handoff for Admin-selected rental mode | M1 approval, multi-client, M3/M4 consumers | `G4`/billing handoff evidence |
| `2026-06-28` | Verify seed/reset instructions, implement/verify `BillingSessions` active restore and rounded-minute billing rule | Stable core; M1 scope decision | `G5` setup/restore result |
| `2026-07-05` | Support frozen demo seed/auth/billing explanation and final blocker disposition | RC approved; M1/M6 request | Final auth/data report |

## Retained Extension Ownership

- Future schema consolidation and customer/reporting persistence remain retained backlog after core release.
- Billing persistence is no longer retained work; it is required `G5` scope after the `2026-06-09` decision.

## Definition Of Done

- Canonical auth path is unambiguous and callable from network flow.
- Machine-bound login and session guard pass runtime tests.
- Billing sessions restore from SQLite and support timed/free rental evidence.
- Deferred persistence outside billing remains documented without destabilizing release scope.

## RR1 Completion Note

- `R1-A01` da hoan thanh theo canonical auth/database baseline.
- `G0-05` da duoc xac minh theo runtime seed trong `internet_cafe.db`.
- `AuthUsers/AuthSessions` van la runtime schema canonical; `Users/Machines/Sessions` tiep tuc la retained/post-core direction.

## R2-A01 Evidence Note

- `Code/ServerApp/Auth/Services/AuthService.cs` da xu ly valid login, wrong password, wrong `machineId`, account-disabled, and machine-active cases theo canonical auth path.
- `Code/Auth_Test/Program.cs` da co verify cho `admin` / `123` / `PC00`, `client01` / `123` / `PC-01`, wrong password, va wrong `machineId`.
- Pham vi M5 cho `R2-A01` da san sang, con M6 runtime verification va final `G2` gate review la phan cua tester/gate owner.

## R4-N01 Evidence Note

- `Code/Auth_Test/Program.cs` da verify hai client authenticated doc lap (`client01` / `PC-01`, `client02` / `PC-02`) co session ID rieng va duplicate active login bi reject voi `MACHINE_ALREADY_ACTIVE`.
- Pham vi M5 cho `R4-N01` da duoc xac nhan o tang auth; networking/LAN routing van la phan cua owner networking va UI.
