# Member 1 - Team Leader / System Architect

## 1. Vai tro

Ban la nguoi giu huong di tong the cua project.
Ban khong can code nhieu nhat, nhung ban phai dam bao ca team biet dang lam gi, phu thuoc vao ai, va khi nao duoc chuyen phase.

Muc tieu cua ban:

- giu dung MVP
- giu dung contract
- giu dung ownership
- giu dung timeline
- giu cho integration khong vo
- giu duoc demo on dinh trong deadline 8 tuan

## 2. Write Scope

Ban duoc uu tien sua:

- `DOCS/LEADER_FLOW.md`
- `DOCS/TASKS.md`
- `DOCS/BUGS.md`
- `README.md`
- cac file docs tong quan khac neu can

Ban khong nen sua runtime code trong `Shared`, `ServerApp`, `ClientApp`, `Data`, `Auth` tru khi dang review integration va da co owner dong y.

## 3. Thu ban so huu

Ban la owner chinh cua:

- scope MVP
- phase va gate
- task assignment
- integration order
- release approval
- conflict resolution
- decision log

## 4. Thu ban khong so huu

Ban khong la owner runtime cua:

- packet implementation cua Member 2
- server UI cua Member 3
- client UI cua Member 4
- database/auth logic cua Member 5
- test execution chi tiet cua Member 6

## 5. Dependency can theo doi

Ban phai biet cac dependency chinh sau:

- Member 3 va Member 4 phu thuoc contract cua Member 2
- Member 2 phu thuoc auth interface cua Member 5 cho login flow
- Member 3 va Member 4 phu thuoc network/auth interface de noi UI
- Member 6 phu thuoc output cua ca team de test va cap nhat docs
- ca team phu thuoc quyet dinh som ve stack, machine identity, va demo mode

## 6. Nhiem vu theo phase

### Phase 0 - Kickoff and Scope Freeze

- chot MVP va out-of-scope
- chot ownership map
- chot branch va commit rule
- chot nguon truth cho docs
- chot risk list dau tien
- chot stack: `.NET 8`, C#, WinForms, TCP, SQLite, `System.Text.Json`
- chot account rieng gan voi `machineId`
- chot 2 mode demo: real LAN va local multi-instance

### Phase 1 - Architecture and Contract Freeze

- review `API.md`
- chot packet order va state model
- chot architecture direction
- giai quyet conflict contract truoc khi code di xa

### Phase 2 - Networking Foundation

- theo doi Member 2 co bi block boi auth hay khong
- dam bao GUI members khong tu viet protocol rieng
- kiem tra output network co the handoff duoc

### Phase 3 - Authentication and Persistence

- theo doi Member 5 va Member 2 noi login flow
- dam bao session rule ro rang
- dam bao auth fail co behavior ro

### Phase 4 - GUI Skeletons and Local Stub Flow

- dam bao Member 3 va Member 4 chi build theo interface
- ngan pham vi UI vuot MVP
- review skeleton da du cho demo flow chua

### Phase 5 - End-to-End Integration

- chot thu tu integrate: login -> status -> lock/unlock -> ack
- chi dinh lead owner cho moi blocker cross-module
- ngan contract drift

### Phase 6 - Realtime Feature Completion

- xac nhan notification, timer, chat duoc lam dung uu tien
- xac nhan chat chi la 1-1 text scope toi thieu
- ngan team danh thoi gian vao polish som
- chap nhan feature chi khi da qua test co ban

### Phase 7 - Stabilization and Hardening

- freeze feature
- uu tien bug theo severity
- quyet dinh bug nao phai fix, bug nao duoc chap nhan tam thoi

### Phase 8 - Testing and Release Candidate

- approve code freeze
- approve release candidate
- doi chieu build voi docs

### Phase 9 - Final Demo and Post-Release Review

- dan demo
- quyet dinh fallback neu co su co
- chot tong ket va limitation

## 7. Ke hoach theo tuan

### Week 1

- chot MVP, scope, ownership, branch rule, commit rule
- dong bo `LEADER_FLOW.md`, `API.md`, `TASKS.md`, `RUN_GUIDE.md`, `TEST_MATRIX.md`, va `DEMO_CHECKLIST.md`
- giao task dau tien cho tung member
- dam bao `Code/NetManager.sln`, `ServerApp`, `ClientApp`, va `Shared` project files duoc tao
- xac nhan Week 1 ket thuc bang runnable skeleton va mot TCP JSON-line packet round trip

### Week 2

- theo doi network core va auth core
- go blocker giua Member 2 va Member 5
- giu contract on dinh

### Week 3

- dieu phoi integration core flow
- chot owner cho bug cross-module
- ngan task moi ngoai luong

### Week 4

- chot feature completion theo MVP
- buoc team test end-to-end thay vi chi test module rieng

### Week 5

- freeze feature
- tap trung stabilization va regression
- cap nhat risk va limitation

### Week 6

- kiem tra multi-client, reconnect, invalid packet, va 2 mode demo

### Week 7

- approve release candidate
- chot cleanup source va docs
- chot known limitations

### Week 8

- chot demo checklist
- chot final docs va archive state
- khoa pham vi phat hanh cuoi

## 8. Hanh dong hang ngay

- doc `TASKS.md`
- xac nhan ai dang block ai
- xac nhan packet/state co doi khong
- cap nhat quyet dinh vao docs
- nhac owner khi thay overlap write scope

## 9. Nguyen tac tranh xung dot

- moi task chi co 1 owner chinh
- file chung phai co nguoi chot cuoi
- packet doi thi docs doi cung ngay
- khong cho 2 nguoi sua cung runtime area neu khong co ly do ro
- merge integration theo tung buoc nho

## 10. Deliverables

- `LEADER_FLOW.md` on dinh
- `TASKS.md` ro rang theo tuan
- `BUGS.md` co severity va owner
- release decision ro rang
- demo flow ro rang

## 11. Definition of Done

- moi member biet ngay viec tiep theo cua minh
- team khong tranh cai vi ownership mo ho
- contract va docs khop implementation
- project di dung phase va kip timeline
