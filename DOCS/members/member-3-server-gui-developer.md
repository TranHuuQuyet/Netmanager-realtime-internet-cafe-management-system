# Member 3 - Server GUI Developer

## Recovery Role

Ban own server/admin UI cho core demo. Audit thay server dashboard la shell va current working tree build bi vo quanh login form; vi vay build restoration va real service binding la uu tien.

## Write Scope

- `Code/ServerApp/Forms/`
- Server presentation bridge/service files duoc approve

## Boundary Rules

- UI khong parse packet, khong truy cap SQLite va khong invent result/error.
- Button/row/card chi duoc coi la feature khi gui action qua real service va render real response.
- Sample rows hoac placeholder feedback khong duoc nop lam runtime pass.

## Core Assignments

| Due | Task | Dependency | Required evidence |
| --- | --- | --- | --- |
| `2026-05-31` | Phoi hop khoi phuc server login/build path | M5 auth startup | Full solution build pass |
| `2026-06-07` | Render online/offline state tu real network event | M2 status route | `G2` UI evidence |
| `2026-06-14` | Gui lock/unlock qua service va show ACK/error | M2 control route | `G3` pass |
| `2026-06-21` | Render two distinct local clients | M2/M5 routing | `G4` evidence |
| `2026-06-28` | Rehearse admin core path | Core regression | `G5` rehearsal result |

## Retained Extension Ownership

- `E1`: admin action cho direct notification sau `G3`.
- `E2`: timer state display neu M1 mo feature.
- `E3`: admin chat UI sau timely `G4`.
- Polished dashboard/customer actions van retained backlog va khong block core delivery.

## Definition Of Done

- Dashboard displays real client state and command outcomes.
- Server UI remains responsive during receive/control events.
- Extension UI is recorded honestly as pass, incomplete or continued after release.
