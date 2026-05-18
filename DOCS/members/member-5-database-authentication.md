# Member 5 - Database & Authentication

## 1. Vai tro

Ban phu trach persistence, authentication, va session state.
Muc tieu cua ban la tao mot auth/data layer don gian, on dinh, de debug, va de cac module khac goi den ma khong can doan internal behavior.

## 2. Write Scope

Ban duoc uu tien sua:

- `ServerApp/Auth/`
- `ServerApp/Database/`
- persistence va repository files

Ban co the cap nhat:

- `DOCS/API.md` neu can bo sung auth/session response, sau khi thong nhat voi Member 1 va Member 2

Ban khong nen sua:

- UI forms
- packet dispatcher implementation
- client runtime

## 3. Thu ban so huu

- SQLite schema
- repository/data access layer
- auth service
- session service
- seed data
- config persistence neu can
- account-to-`machineId` validation

## 4. Dependency cua ban

- login packet flow tu Member 2
- admin/client login screens cua Member 3 va Member 4 se dung output cua ban
- scope va release order tu Member 1

## 5. Nhiem vu theo phase

### Phase 0 Completion Checklist

- [x] Review and confirm auth/database scope aligns with project rules in `LEADER_FLOW.md` and `API.md`.
- [x] Confirm ownership of `ServerApp/Auth/`, `ServerApp/Database/`, persistence, and repository files.
- [x] Confirm dependency on login packet flow from Member 2 and login screens from Member 3 and Member 4.
- [x] Confirm account-to-machineId validation rule: server validates username, password, machineId; reject if wrong machineId.
- [x] Confirm SQLite schema, auth service, session service, and seed data ownership.

### Phase 0

- review auth/database scope

### Phase 1

- define schema assumptions
- define auth result shape
- define session state shape
- define account-to-machine mapping rule

### Phase 2

- expose auth interface cho network layer goi

### Phase 3

- implement `Users`
- implement `Sessions`
- implement repositories
- implement auth validation
- implement session tracking
- reject login neu sai `machineId`

### Phase 4

- provide auth stubs hoac service contracts cho GUI team neu can

### Phase 5

- support login integration
- support session-to-server-state behavior

### Phase 6

- finish timer/session persistence neu can
- finish machine mapping neu can

### Phase 7

- harden invalid login, missing user, DB error, consistency issue

### Phase 8

- chi sua release-blocking auth/DB bug
- verify demo accounts va setup

### Phase 9

- giai thich auth/session/database flow khi demo

## 6. Ke hoach theo tuan

### Week 1

- draft schema
- draft auth result model
- draft session model
- create auth service skeleton
- chot demo seed account baseline voi Member 1 va Member 6
- cung cap interface du de Member 2 noi login path trong Week 2

### Week 2

- finish schema
- finish repository layer
- finish auth validation
- finish session basics

### Week 3

- support login integration
- fix auth/session mismatch trong core flow

### Week 4

- finish timer/session persistence rules neu co

### Week 5

- fix DB/auth edge cases
- fix consistency and error handling

### Week 6

- verify real LAN va local multi-instance account behavior

### Week 7

- verify release candidate data va cleanup auth/data boundaries

### Week 8

- verify final demo accounts va persistence behavior

## 7. Handoff cho nguoi khac

Ban phai cung cap:

- auth service interface
- auth result shape
- session state shape
- seed account thong tin can thiet cho demo
- expected error cases

## 8. Nguyen tac tranh xung dot

- schema change phai thong nhat truoc
- Member 2 own packet transport, ban own auth/data logic
- UI khong duoc chua SQL
- khong de password plaintext trong flow production-like

## 9. Deliverables

- SQLite schema
- repository layer
- auth service
- session service
- seed data

## 10. Definition of Done

- admin/client login on dinh
- data doc ghi duoc
- DB/auth error khong lam sap demo
- module khac co the goi auth layer ma khong can doan internals
