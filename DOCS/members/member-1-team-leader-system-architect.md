# Member 1 - Team Leader / System Architect

## Recovery Role

Ban own scope, decision, dependency, gate approval va release control cho deadline `2026-07-05`. Full roadmap cu da duoc thay bang recovery flow; extension quan trong van duoc giu va chi mo sau core gate.

## Write Scope

- `DOCS/DECISIONS.md`
- `DOCS/LEADER_FLOW.md`
- `DOCS/TASKS.md`
- `DOCS/BUGS.md`
- `README.md`

## Non-Owned Scope

- Core runtime implementation owned by M2-M5.
- Runtime test execution and evidence records owned by M6.
- Extension implementation before an approved opening decision.

## Dependencies

- Can owner submissions and M6 verification before approving a gate.
- Can M2/M5 handoff for API, auth, session and canonical database decisions.
- Can M3/M4 runtime evidence before accepting UI completion.

## Boundary Rules

- Chot API/auth/database recovery decision va khong cho contract drift.
- Khong approve progress dua tren shell/skeleton ma khong co runtime evidence.
- Chan extension neu core gate chua pass.
- Approve release candidate, freeze va fallback decision.
- Bao ve `main`: chi approve/promotion tu `develop` sau khi M6 da ghi `Pass` cho candidate tich hop; tu choi feature branch merge truc tiep vao `main`.

## Core Assignments

| Due | Task | Dependency | Required evidence |
| --- | --- | --- | --- |
| `2026-05-27` | Confirm recovery scope, canonical auth/API/DB, retained extension va `develop` -> test -> `main` rule | Recovery report | Approved decision entries and team branch notice |
| `2026-05-31` | Review `G0/G1` foundation gate | Owner submissions; M6 verification | Build, contract and TCP evidence |
| `2026-06-07` | Review `G2` auth/status | `G0/G1` pass; M6 report | Gate approval note |
| `2026-06-14` | Review `G3` control and decide whether `E1` opens | `G2` pass; M6 report | Gate/extension decision |
| `2026-06-21` | Review `G4`, open/retain extensions and select documented fallback if required | `G3` pass; M6 report | Gate/priority note |
| `2026-06-30` | Approve RC/freeze | `G5` candidate | RC identity and freeze rule |
| `2026-07-05` | Lead final delivery and retained-feature report | RC rehearsals; final checklist | Final demo record |

## Retained Extension Ownership

- Mo `E1-E7` chi khi gate condition trong `LEADER_FLOW.md` pass.
- Neu extension chua xong truoc deadline, ghi `Retained - Continue After Core Release`, khong xoa scope.
- Customer CRUD, shutdown, extra polish va reporting chi mo sau `G5`.

## Definition Of Done

- Core gates co decision ro va evidence.
- Khong co High/Critical blocker bi bo qua khong co owner.
- `main` chi chua candidate da duoc M6 verify `Pass` tren `develop` va duoc M1 approve.
- Release docs phan anh dung core pass va extension continuation.
