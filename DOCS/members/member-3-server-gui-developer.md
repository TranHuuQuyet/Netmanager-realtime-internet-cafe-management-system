# Member 3 - Server GUI Developer

## Recovery Role

Ban own server/admin UI cho core demo. Dashboard hien van la shell cho den khi bind vao runtime service; vi vay build restoration, real service binding, required chat UI va billing monitor la uu tien.

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
- Can M5 cung cap `BillingSessions` restore data va rental mode state.
- Can M6 verify UI evidence tren current integration build.

## Boundary Rules

- UI khong parse packet, khong truy cap SQLite va khong invent result/error.
- Button/row/card chi duoc coi la feature khi gui action qua real service va render real response.
- Sample rows hoac placeholder feedback khong duoc nop lam runtime pass.
- Nop server UI feature/fix branch vao `testing_branch`; khong merge truc tiep vao `main`, va cho M6 `Pass` truoc release promotion.

## Core Assignments

| Due | Task | Dependency | Required evidence |
| --- | --- | --- | --- |
| `2026-05-31` | Phoi hop khoi phuc server login/build path | M5 auth startup | Full solution build pass |
| `2026-06-07` | Render online/offline state tu real network event | M2 status route | `G2` UI evidence |
| `2026-06-14` | Gui lock/unlock qua service va show ACK/error | M2 control route | `G3` pass |
| `2026-06-21` | Render two distinct local clients, prepare LAN client visibility, implement selected-client chat UI, and define Admin rental-mode controls | M2/M5 routing; M1 scope decision | `G4`/required-scope evidence |
| `2026-06-28` | Rehearse Admin path including LAN status, chat, timed/free billing monitor, extend action and restart restore | Core regression; M5 billing restore | `G5` rehearsal result |
| `2026-07-05` | Support frozen server demo path and final limitation reporting for required and retained scope | RC approved; M1/M6 request | Final demo support note |

## Retained Extension Ownership

- `E1`: admin action cho direct notification sau `G3`.
- Polished dashboard/customer actions van retained backlog va khong block core delivery.

## Definition Of Done

- Dashboard displays real client state and command outcomes.
- Admin Panel displays required chat and billing/session monitor evidence, including restore after ServerApp restart.
- Server UI remains responsive during receive/control events.
- Extension UI is recorded honestly as pass, incomplete or continued after release.

## R3 Completion Note

- `R3-N01`: Admin machine selection sends real `LOCK`/`UNLOCK` through `IAdminCommandService`; M6 command trace is recorded as `Pass` in `DOCS/VERIFICATION/R3_M6_VERIFI.md`.
- `R3-U02`: typed command ACK/error results are mapped into `AdminCommandResult` and rendered by `MainForm`; M6 records the dashboard result boundary as `Pass`.
- `R3-Q01` remains an M6/M1 gate action and is not closed by M3.

## R4 Member 3 Handoff Status

### `R4-U01` Admin multi-client UI

- Server UI keeps one row/card per `machineId` and preserves the Admin-selected machine when another client emits a status update.
- Machine selection remains the source for Admin command and chat targeting.
- M3 implementation is ready for integrated two-client evidence; M2 must provide the two-client/LAN runtime route, M4 must provide the matching Client UI instances, and M6 must verify the final evidence.

### `R4-C01` selected-client chat UI

- `IAdminChatService` is the presentation boundary for sending chat and receiving typed `AdminChatMessage` events.
- Admin chat history is isolated per selected `machineId`; a message is displayed as sent only after the service returns success.
- Until M2 binds the real `CHAT` route, the UI returns controlled `CHAT_SERVICE_UNAVAILABLE`; M3 does not parse packets or invent network success.
- Full task completion still requires M2 routing, M4 client send/receive UI and M6 selected/non-selected client evidence.

### `R4-B01` Admin billing-control handoff

M3 owns these Admin controls for the selected machine:

- start timed rental with a `5-10` minute demo duration;
- start open-ended/free-time rental (not zero-price rental);
- extend the active timed rental and end an active rental;
- display the fixed default rate `10000` VND/hour supplied by the approved billing contract.

M3 requires a typed M5-owned service result containing:

- billing session ID, `machineId` and client/session reference;
- rental mode, start time, optional expiry, active/closed status and rate per hour;
- canonical charged minutes and amount using the approved rounded-minute formula;
- deterministic success/error code and message for start, extend, end, query-active and restore operations.

The Admin form must consume this service and must not query SQLite directly. M5 still owns `BillingSessions`, restore and billing calculation; M1 must approve the shared handoff before `R4-B01` is closed. Runtime monitor implementation remains `R5-B02` and is not pulled into R4.
