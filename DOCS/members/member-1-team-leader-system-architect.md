# Member 1 - Team Leader / System Architect

## Role

Ban giu huong di tong the cua project. Viec chinh la chot scope, chot ownership, dieu phoi dependency, approve gate, va giu demo on dinh trong baseline 8 tuan.

Tracker tien do nam trong `DOCS/TASKS.md`. File nay chi la playbook ca nhan, khong tick task tai day.

## Write Scope

Ban duoc uu tien sua:

- `DOCS/LEADER_FLOW.md`
- `DOCS/TASKS.md`
- `DOCS/BUGS.md`
- `README.md`
- docs tong quan khi can dong bo flow

## Non-Owned Scope

Ban khong own runtime implementation cua:

- packet/network code cua M2
- server UI cua M3
- client UI cua M4
- auth/database logic cua M5
- detailed test execution cua M6

## Dependencies

- M2 can API/packet approval som de code network.
- M3 va M4 can service/interface ro de khong parse packet trong UI.
- M5 can login/auth/session boundary ro voi M2.
- M6 can build output va decision ro de update tests/docs.

## Handoff Rules

- Chot owner chinh cho moi cross-module blocker.
- Ghi decision vao docs trong cung ngay.
- Khong cho scope moi vao khi current gate chua qua.
- Neu team doi packet/auth/session, yeu cau update `API.md` truoc hoac cung review batch.

## My 8-Week Flow

### Week 1

- `W1.P1`: chot MVP, out-of-scope, stack, ownership, source of truth, branch/commit rules.
- `W1.P2`: review `API.md` voi M2/M5 va approve contract baseline.
- `W1.P3`: confirm scaffold, project references, handoff order, va first round-trip target.

### Week 2

- `W2.P1`: remove blocker cho network core va ngan contract drift.
- `W2.P2`: approve auth/network boundary va login integration order.
- `W2.P3`: verify UI skeleton scope van nam trong MVP.

### Week 3

- `W3.P1`: define login integration order va owner cho blocker.
- `W3.P2`: confirm status model khop API.
- `W3.P3`: freeze non-essential work den khi one-client core demo chay.

### Week 4

- `W4.P1`: giu control flow la uu tien chinh.
- `W4.P2`: approve error behavior va severity cho ACK/error blockers.
- `W4.P3`: decide control gate pass/defer.

### Week 5

- `W5.P1`: giu notification scope nho.
- `W5.P2`: decide timer persistence co can cho MVP hay khong.
- `W5.P3`: enforce chat 1-1 text-only.

### Week 6

- `W6.P1`: prioritize multi-client blockers.
- `W6.P2`: freeze feature work va rank bugs by severity.
- `W6.P3`: decide accepted fallback neu real LAN khong on.

### Week 7

- `W7.P1`: classify bugs as fix, accept, or defer.
- `W7.P2`: approve cleanup scope va block unrelated refactors.
- `W7.P3`: approve release candidate va code freeze rules.

### Week 8

- `W8.P1`: lead final rehearsal va fallback trigger.
- `W8.P2`: lock release scope, approve emergency fixes only.
- `W8.P3`: lead final demo va archive final decisions.

## Definition of Done

- Team biet viec tiep theo theo dung `Wn.Pm`.
- Ownership ro, khong tranh sua cung module.
- Contract va docs khop implementation.
- Demo path co fallback ro va khong con blocker high severity chua accept.
