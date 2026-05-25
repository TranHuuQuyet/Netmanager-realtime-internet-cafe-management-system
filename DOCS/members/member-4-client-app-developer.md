# Member 4 - Client App Developer

## Recovery Role

Ban own client UI va client reaction trong core demo. Audit ghi nhan connect/login/main/lock forms dang ton tai duoi dang local artifact, nhung chua phai runtime completion vi chua consume network/auth service.

## Write Scope

- `Code/ClientApp/`
- Client presentation bridge/service files duoc approve

## Historical Handoff Preserved

- Client input for MVP: `LOGIN` and `STATUS`.
- Client must render/handle: `LOGIN` response, `LOCK`, `UNLOCK`, `ACK`, `NOTIFICATION`, `TIMER`, `CHAT`.
- Client login rule: `machineId` is required and a wrong machine must be rejected visibly.
- M4 does not own packet schema; UI consumes the approved shared contract.

## Boundary Rules

- Khong parse raw JSON trong form va khong invent packet shape.
- Form shell chi la artifact den khi duoc bind vao real client service.
- UI result phai dua tren auth/command event that, khong dung dialog placeholder lam evidence.

## Core Assignments

| Due | Task | Dependency | Required evidence |
| --- | --- | --- | --- |
| `2026-05-31` | Hop nhat forms local vao buildable client path va report shell status trung thuc | Server/build gate | Build/UI smoke note |
| `2026-06-07` | Bind connect/login vao real service va render auth/wrong-machine result | M2/M5 login route | `G2` UI evidence |
| `2026-06-14` | Execute lock/unlock state va send real ACK | M2 control route | `G3` pass |
| `2026-06-21` | Keep two local client instances isolated | M2/M5 routing | `G4` pass |
| `2026-06-28` | Rehearse client core path | Core regression | `G5` result |

## Retained Extension Ownership

- `E1`: show direct notification after `G3`.
- `E2`: show timer update after notification/stability gate.
- `E3`: send/receive direct text chat after timely `G4`.
- `E4/E7`: support LAN/reconnect UX only after their gates open.

## Definition Of Done

- Client logs in and reacts to commands through real runtime boundaries.
- Lock/unlock/ACK path passes verification.
- Existing and future client features remain visible in extension tracking without overstating completion.
