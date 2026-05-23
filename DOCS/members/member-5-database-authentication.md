# Member 5 - Database & Authentication

## Role

Ban own persistence, authentication, session state, seed data, va account-to-`machineId` validation. Muc tieu la auth/data layer don gian, on dinh, de debug, va de module khac goi qua interface ro.

Tracker tien do nam trong `DOCS/TASKS.md`. File nay chi la playbook ca nhan, khong tick task tai day.

## Write Scope

Ban duoc uu tien sua:

- `Code/ServerApp/Auth/`
- `Code/ServerApp/Database/`
- persistence/repository files
- auth/session model docs khi da thong nhat voi M1/M2

## Non-Owned Scope

Ban khong own:

- socket dispatcher cua M2
- packet schema implementation cua M2
- server UI cua M3
- client UI cua M4
- release/gate decision cua M1

## Dependencies

- Can M2 cung cap login packet flow va auth interface call path.
- Can M3/M4 consume auth result dung contract.
- Can M1 approve auth/session contract neu anh huong flow.
- Can M6 feedback tu valid/invalid login tests.

## Handoff Rules

- Cung cap auth service interface, auth result shape, session shape, seed account baseline, expected error cases.
- Schema change phai thong nhat truoc khi module khac depend.
- UI khong chua SQL; network layer khong embed DB logic.
- Wrong `machineId` phai co deterministic error result.

## My 8-Week Flow

### Week 1

- `W1.P1`: confirm auth/database scope, machine mapping validation, seed/auth/session responsibility.
- `W1.P2`: draft `Users`, `Sessions`, auth result, session state, wrong `machineId` validation.
- `W1.P3`: create auth service skeleton and schema/session draft.

### Week 2

- `W2.P1`: provide auth interface shape needed by network login path.
- `W2.P2`: implement schema, repository skeleton, auth validation, session basics.
- `W2.P3`: provide auth stubs/service contracts to UI owners.

### Week 3

- `W3.P1`: validate username/password/role/`machineId` and create session state.
- `W3.P2`: support session fields needed by status flow.
- `W3.P3`: fix auth/session issues found in integration.

### Week 4

- `W4.P1`: verify active session and machine ownership before command flow.
- `W4.P2`: return deterministic auth/session errors where relevant.
- `W4.P3`: fix session consistency issues.

### Week 5

- `W5.P1`: confirm notification does not need persistence unless approved.
- `W5.P2`: support timer/session persistence if approved.
- `W5.P3`: avoid chat history unless approved.

### Week 6

- `W6.P1`: enforce account-machine mapping and duplicate active session rules.
- `W6.P2`: harden DB/auth failure behavior and data consistency.
- `W6.P3`: verify seed accounts and machine mapping in both demo modes.

### Week 7

- `W7.P1`: fix release-blocking auth/DB bugs only.
- `W7.P2`: clean auth/data boundaries without behavior change.
- `W7.P3`: verify demo accounts, seed data, auth/session behavior.

### Week 8

- `W8.P1`: rehearse auth/DB/session explanation.
- `W8.P2`: avoid DB/auth changes unless release-blocking.
- `W8.P3`: explain auth/session/database flow.

## Definition of Done

- Admin/client login behavior is stable.
- Correct account + wrong `machineId` is rejected clearly.
- Session state is deterministic.
- DB/auth errors do not crash the demo.
