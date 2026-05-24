# Member 2 - Network Engineer

## Role

Ban own networking va shared transport contract implementation. Muc tieu la server/client noi chuyen dung schema, khong freeze UI, support multi-client, va de UI/auth layer tich hop qua interface ro.

Tracker tien do nam trong `DOCS/TASKS.md`. File nay chi la playbook ca nhan, khong tick task tai day.

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

## Handoff Rules

- Cung cap packet shape, service interface, event/callback, va sample input/output.
- Packet/schema doi thi update `DOCS/API.md` cung ngay.
- Auth duoc goi qua interface, khong embed SQL vao network layer.
- GUI khong parse packet truc tiep trong form khi da co service boundary.

## My 8-Week Flow

### Week 1

- `W1.P1`: confirm TCP/JSON-line feasibility, packet boundary, local multi-instance assumption.
- `W1.P2`: define packet envelope, packet types, parser/serializer rules, invalid packet behavior.
- `W1.P3`: create shared packet baseline, networking skeleton, connect/send/receive proof.

### Week 2

- `W2.P1`: finish TCP listener, client connector, send/receive loop, dispatcher skeleton.
- `W2.P2`: call auth interface through agreed login contract.
- `W2.P3`: provide UI-facing connection/state events or stubs.

### Week 3

- `W3.P1`: route `LOGIN` request/response and network errors.
- `W3.P2`: route `STATUS` heartbeat/state changes.
- `W3.P3`: connect login, status, lock/unlock route skeleton, and ACK visibility.

### Week 4

- `W4.P1`: finish `LOCK` and `UNLOCK` routing.
- `W4.P2`: finish `ACK`, error packet handling, invalid packet guard.
- `W4.P3`: fix network regressions in core control path.

### Week 5

- `W5.P1`: route direct/broadcast `NOTIFICATION`.
- `W5.P2`: route `TIMER` updates.
- `W5.P3`: route direct `CHAT` messages.

### Week 6

- `W6.P1`: manage multiple connections, session routing, duplicate login behavior.
- `W6.P2`: harden disconnect, reconnect, timeout, malformed packet handling.
- `W6.P3`: verify local and real LAN network assumptions.

### Week 7

- `W7.P1`: fix release-blocking network bugs only.
- `W7.P2`: clean network/shared boundaries without behavior change.
- `W7.P3`: support setup and network verification for release candidate.

### Week 8

- `W8.P1`: validate network setup and fallback local mode.
- `W8.P2`: avoid network changes unless release-blocking.
- `W8.P3`: support live network troubleshooting.

## Definition of Done

- 2 to 3 clients connect distinctly.
- Packet schema matches `DOCS/API.md`.
- UI thread does not block on socket work.
- Disconnect, timeout, invalid packet, and reconnect cases fail gracefully.
