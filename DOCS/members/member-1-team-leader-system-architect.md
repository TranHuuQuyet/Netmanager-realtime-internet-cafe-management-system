# Member 1 - Team Leader / System Architect

## Recovery Role

Ban own scope, decision, dependency, gate approval va release control cho deadline `2026-07-05`. Full roadmap cu da duoc thay bang recovery flow; extension quan trong van duoc giu va chi mo sau core gate.

## Write Scope

- `DOCS/DECISIONS.md`
- `DOCS/LEADER_FLOW.md`
- `DOCS/TASKS.md`
- `DOCS/BUGS.md`
- `README.md`

## Critical Responsibilities

- Chot API/auth/database recovery decision va khong cho contract drift.
- Khong approve progress dua tren shell/skeleton ma khong co runtime evidence.
- Chan extension neu core gate chua pass.
- Approve release candidate, freeze va fallback decision.

## Core Assignments

| Due | Task | Required evidence |
| --- | --- | --- |
| `2026-05-27` | Confirm recovery scope, canonical auth/API/DB va retained extension rule | Approved decision entries |
| `2026-05-31` | Review `G0/G1` foundation gate | Build, contract and TCP evidence |
| `2026-06-07` | Review `G2` auth/status | M6 test report |
| `2026-06-14` | Review `G3` control and decide whether `E1` opens | Gate approval note |
| `2026-06-21` | Review `G4`, open/retain extensions and fallback if required | Gate/priority note |
| `2026-06-30` | Approve RC/freeze | RC identity and freeze rule |
| `2026-07-05` | Lead final delivery and retained-feature report | Final demo record |

## Retained Extension Ownership

- Mo `E1-E7` chi khi gate condition trong `LEADER_FLOW.md` pass.
- Neu extension chua xong truoc deadline, ghi `Retained - Continue After Core Release`, khong xoa scope.
- Customer CRUD, shutdown, extra polish va reporting chi mo sau `G5`.

## Definition Of Done

- Core gates co decision ro va evidence.
- Khong co High/Critical blocker bi bo qua khong co owner.
- Release docs phan anh dung core pass va extension continuation.
