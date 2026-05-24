# Member 6 - Tester & Documentation

## Role

Ban own testing, bug tracking, run guide, demo checklist, va docs consistency. Muc tieu la giup team biet cai gi pass, cai gi fail, bug nao nghiem trong, va docs nao dang lech build.

Tracker tien do nam trong `DOCS/TASKS.md`. File nay chi la playbook ca nhan, khong tick task tai day.

## Write Scope

Ban duoc uu tien sua:

- `DOCS/BUGS.md`
- `DOCS/RUN_GUIDE.md`
- `DOCS/TEST_MATRIX.md`
- `DOCS/DEMO_CHECKLIST.md`
- `README.md`
- `DOCS/TASKS.md` khi M1 giao cap nhat tracker

## Non-Owned Scope

Ban khong own:

- runtime network code cua M2
- server UI code cua M3
- client UI code cua M4
- auth/database code cua M5
- scope/release approval cua M1

## Dependencies

- Can M1 chot gate, priority, accepted risk.
- Can M2 cung cap API/connection behavior de test.
- Can M3/M4 cung cap UI build de verify.
- Can M5 cung cap auth/session behavior va seed accounts.

## Handoff Rules

- Bug report phai co module, mode, steps, expected, actual, severity, workaround neu co.
- Docs update theo implementation that, khong theo assumption cu.
- Contract mismatch phai report cho M1/M2 truoc khi sua API.
- Test status doi tu pass sang fail thi tao bug entry.

## My 8-Week Flow

### Week 1

- `W1.P1`: confirm bug/test/doc ownership and source-of-truth docs.
- `W1.P2`: derive first test cases and flag doc contradictions.
- `W1.P3`: create bug template, test matrix, connection checklist, demo-mode checklist.

### Week 2

- `W2.P1`: run connection, invalid packet, local multi-instance checks.
- `W2.P2`: create valid/invalid login and wrong `machineId` test cases.
- `W2.P3`: verify screens cover planned demo path.

### Week 3

- `W3.P1`: test admin/client login, bad password, wrong `machineId`.
- `W3.P2`: test status after login and stale/disconnect behavior.
- `W3.P3`: record integration bugs with exact repro steps.

### Week 4

- `W4.P1`: test lock/unlock in local mode.
- `W4.P2`: test ACK success, ACK failure, invalid packet, unsupported packet.
- `W4.P3`: run Gate D regression and update bug status.

### Week 5

- `W5.P1`: test direct and broadcast notification behavior.
- `W5.P2`: test timer update, expiry path, and reset assumptions.
- `W5.P3`: test one admin/client chat path and confirm scope remains 1-1 text.

### Week 6

- `W6.P1`: test 2 to 3 clients in local multi-instance mode.
- `W6.P2`: run regression and verify fixed bugs.
- `W6.P3`: run local multi-instance and real LAN smoke checks.

### Week 7

- `W7.P1`: maintain bug severity list and regression status.
- `W7.P2`: align README, run guide, test matrix, demo checklist with build.
- `W7.P3`: prepare final regression report draft and known limitations.

### Week 8

- `W8.P1`: run final checklist and record rehearsal issues.
- `W8.P2`: verify docs match final build and limitation list.
- `W8.P3`: archive demo notes, bug summary, final docs status.

## Definition of Done

- Bug entries are reproducible and actionable.
- Test matrix reflects real status.
- Run guide and demo checklist can be followed by the team.
- Final docs match the build and accepted limitations.
