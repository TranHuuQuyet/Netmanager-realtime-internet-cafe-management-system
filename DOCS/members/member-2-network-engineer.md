# Member 2 - Network Engineer

## Recovery Role

Ban own shared wire implementation, TCP runtime, dispatcher, routing va multi-client connection behavior. Hien tai networking co skeleton nhung chua co verified round-trip, nen `R1` la blocker lon nhat cua team.

## Write Scope

- `Code/Shared/`
- `Code/ServerApp/Networking/`
- `Code/ClientApp/Networking/`
- `DOCS/API.md` khi contract duoc M1 approve

## Boundary Rules

- Implement dung API `v0.2`: string `type`, distinguishable `LOGIN` response va top-level error.
- Khong embed auth SQL hoac UI behavior vao dispatcher.
- Forms consume typed service/events; khong gui raw parsing work cho M3/M4.
- Moi packet/routing handoff co sample input/output va error result.

## Core Assignments

| Due | Task | Dependency | Required evidence |
| --- | --- | --- | --- |
| `2026-05-31` | Align serializer/parser; implement listener/dispatcher va local valid/invalid round-trip | Build/contract freeze | `G0/G1` test evidence |
| `2026-06-07` | Route `LOGIN` through auth va `STATUS` to state events | `G1` pass, M5 interface | `G2` trace |
| `2026-06-14` | Route `LOCK`, `UNLOCK`, `ACK` va controlled errors | `G2` pass | `G3` trace |
| `2026-06-21` | Route two clients va disconnect safely | `G3` pass, duplicate rule | `G4` pass |
| `2026-06-28` | Support core regression va local rehearsal | `G4` pass | `G5` evidence |

## Retained Extension Ownership

| Feature | Open rule | Network responsibility |
| --- | --- | --- |
| Direct notification | After `G3` | Route direct message and result |
| Timer display | After `E1` and stable core | Route timer update |
| Chat | After timely `G4` | Route direct text message only |
| Real LAN | After local rehearsal | Support endpoint/config smoke |
| Broadcast/reconnect | After prerequisite pass | Harden extension behavior |

## Definition Of Done

- Core routing works through typed contract with no UI freeze/crash evidence.
- Two local clients remain isolated.
- Extension work remains tracked even when continued after core release.
