# Member 3 - Server GUI Developer

## Recovery Role

Ban own server/admin UI cho core demo. Dashboard hien van la shell cho den khi bind vao runtime service; vi vay build restoration va real service binding la uu tien.

## Write Scope

- `Code/ServerApp/Forms/`
- Server presentation bridge/service files duoc approve

## Non-Owned Scope

- Shared packet/TCP/dispatcher behavior cua M2.
- Authentication, database va session policy cua M5.
- Client-side command reaction cua M4.
- Gate approval cua M1 va test status cua M6.

## Dependencies

- Can M5 cung cap server auth startup buildable cho login path.
- Can M2 cung cap typed status/control events va command service boundary.
- Can M6 verify UI evidence tren current integration build.

## Boundary Rules

- UI khong parse packet, khong truy cap SQLite va khong invent result/error.
- Button/row/card chi duoc coi la feature khi gui action qua real service va render real response.
- Sample rows hoac placeholder feedback khong duoc nop lam runtime pass.
- Nop server UI feature/fix branch vao `develop`; khong merge truc tiep vao `main`, va cho M6 `Pass` truoc release promotion.

## Core Assignments

| Due | Task | Dependency | Required evidence |
| --- | --- | --- | --- |
| `2026-05-31` | Phoi hop khoi phuc server login/build path | M5 auth startup | Full solution build pass |
| `2026-06-07` | Render online/offline state tu real network event | M2 status route | `G2` UI evidence |
| `2026-06-14` | Gui lock/unlock qua service va show ACK/error | M2 control route | `G3` pass |
| `2026-06-21` | Render two distinct local clients | M2/M5 routing | `G4` evidence |
| `2026-06-28` | Rehearse admin core path | Core regression | `G5` rehearsal result |
| `2026-07-05` | Support frozen server demo path and final limitation reporting | RC approved; M1/M6 request | Final demo support note |

## Retained Extension Ownership

- `E1`: admin action cho direct notification sau `G3`.
- `E2`: timer state display neu M1 mo feature.
- `E3`: admin chat UI sau timely `G4`.
- Polished dashboard/customer actions van retained backlog va khong block core delivery.

## Definition Of Done

- Dashboard displays real client state and command outcomes.
- Server UI remains responsive during receive/control events.
- Extension UI is recorded honestly as pass, incomplete or continued after release.
