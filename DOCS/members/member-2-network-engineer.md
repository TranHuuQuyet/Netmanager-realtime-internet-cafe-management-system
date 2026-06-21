# Member 2 - Network Engineer

## Recovery Role

Ban own networking va shared transport contract implementation. Muc tieu la server/client noi chuyen dung schema, support multi-client/LAN, required 1-1 chat, billing timer transport, minimal reconnect/resync, va de UI/auth layer tich hop qua interface ro. Recovery phai bam gate/evidence, khong approve progress dua tren skeleton hay local stub.

## Write Scope

Ban duoc uu tien sua:

- `Code/Shared/`
- `Code/ServerApp/Networking/`
- `Code/ClientApp/Networking/`
- `DOCS/API.md`

## Non-Owned Scope

Ban khong own:

- server UI cua M3
- client UI cua M4
- auth/database internals cua M5
- release approval cua M1
- bug/test status cua M6

## Dependencies

- Can M1 approve contract/scope change.
- Can M5 cung cap auth service/result/session shape.
- Can M3/M4 noi ro UI event/action can consume.
- Can M6 feedback tu connection, invalid packet, multi-client tests.

## Boundary Rules

- Cung cap packet shape, service interface, event/callback, va sample input/output.
- Packet/schema doi thi update `DOCS/API.md` cung ngay.
- Auth duoc goi qua interface, khong embed SQL vao network layer.
- GUI khong parse packet truc tiep trong form khi da co service boundary.
- R1 scope stops at shared/server network foundation and local JSON-line evidence; ClientApp UI login integration starts in R2.
- Nop thay doi networking/contract qua feature/fix branch vao `testing_branch`; khong merge truc tiep vao `main`, va cho M6 `Pass` truoc release promotion.

## Core Assignments

| Due | Task | Dependency | Required evidence |
| --- | --- | --- | --- |
| `2026-05-31` | Freeze/implement API `v0.2` transport behavior: string packet type, `LOGIN` response/error envelope, listener/dispatcher baseline and valid/invalid JSON-line round-trip | M1 contract approval; M5 auth handoff | `G0/G1` contract and network evidence |
| `2026-06-07` | Route `LOGIN` through canonical auth service and emit `STATUS` after authenticated login/disconnect | `G0/G1` pass; M5 auth service | `G2` login/status trace |
| `2026-06-14` | Route `LOCK`, `UNLOCK`, typed `ACK` and controlled command errors | `G2` pass; M3/M4 command consumers | `G3` command/ACK trace |
| `2026-06-21` | Route two authenticated clients distinctly, keep routing LAN-capable, verify disconnect stability, coordinate duplicate-login behavior, and implement required selected-client `CHAT` route | `G3` pass; M5 session rule; M1 scope decision | `G4` routing/disconnect/chat evidence |
| `2026-06-28` | Support core regression, local rehearsal, physical LAN rehearsal, timer/expiry `LOCK` transport and minimal reconnect/resync after ServerApp restart | `G4` pass; M5 billing session handoff | `G5` LAN/chat/billing/restart network evidence |
| `2026-07-05` | Support frozen release/demo and submit final network report covering LAN/chat/billing/restart; introduce no new core feature after freeze | `G5` candidate; M1 release freeze | Release support and final network report |

## Retained Extension Ownership

- `E1`: route direct notification after `G3` passes and M1 opens the extension.
- `E5`: route notification broadcast after `E1` is stable.
- `E7`: support reconnect polish after required minimal reconnect/resync passes.

## Definition Of Done

- Packet serialization and routing match `DOCS/API.md` recovery baseline `v0.2`.
- Listener/connector valid and invalid JSON-line behavior is verified.
- `LOGIN`, `STATUS`, `LOCK`, `UNLOCK` and `ACK` function through real runtime boundaries.
- Two local clients and two physical LAN clients connect and route distinctly.
- Disconnect does not crash the server and socket work does not block the UI.
- Required chat, LAN, timer/billing transport and minimal reconnect/resync pass `G5`; only notification broadcast and reconnect polish remain extension outcomes.
