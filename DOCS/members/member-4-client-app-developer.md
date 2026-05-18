# Member 4 - Client App Developer

## 1. Vai tro

Ban phu trach ung dung client.
Muc tieu cua ban la tao client co the connect, login, nhan lenh realtime, hien lock screen khi can, va giu trai nghiem on dinh cho may client.

## 2. Write Scope

Ban duoc uu tien sua:

- `ClientApp/`
- client-side forms
- client-side views
- client-side UI bridge/services neu can cho UI

Ban khong nen sua:

- packet schema
- server dashboard
- database/auth internals

## 3. Thu ban so huu

- connect form
- client login screen
- main client screen
- lock screen
- notification/timer/chat display
- client-side command reaction
- client-side reconnect UX

## 4. Dependency cua ban

- network/service interface tu Member 2
- login result flow tu Member 2 va Member 5
- scope va phase order tu Member 1
- machine-bound login rule tu Member 1, 2, 5

## 5. Nhiem vu theo phase

### Phase 0

- review client scope

#### Phase 0 checklist/xac nhan cua Member 4

- [x] Da doc va doi chieu `DOCS/LEADER_FLOW.md` muc Phase 0.
- [x] Da xac nhan scope client GUI gom connect form, client login screen, main client screen, lock screen, notification/timer/chat display, client-side command reaction, va reconnect UX.
- [x] Da xac nhan write scope chinh la `Code/ClientApp/`, gom `Forms/`, `Models/`, `Networking/`, va `Services/` theo boundary hien co.
- [x] Da xac nhan khong sua packet schema, server dashboard, database/auth internals trong Phase 0.
- [x] Da xac nhan client UI chi render state va goi service/interface, khong tu invent packet shape hoac parse packet truc tiep trong form.
- [x] Da xac nhan dependency can cho cac phase sau: network/service interface tu Member 2, login/auth result tu Member 2 va Member 5, machine-bound login rule tu Member 1/2/5.
- [x] Da xac nhan client login phai dung `username + password + machineId` va phai hien loi ro khi sai `machineId`.
- [x] Da xac nhan chat scope chi la 1-1 text toi thieu, khong emoji, khong history, khong file/image.
- [x] Da xac nhan client phai ho tro ca `Mode A - Real LAN Demo` va `Mode B - Local Multi-Instance Demo`.
- [x] Da xac nhan Phase 0 khong yeu cau runtime implementation; Week 1 bat dau client shell trong runnable skeleton, Phase 4 se hoan thien skeleton theo service/interface.

#### Bao cao Phase 0 cua Member 4

Ket qua ra soat:

- Phase 0 chung da duoc danh dau hoan thanh trong `DOCS/TASKS.md`.
- `DOCS/LEADER_FLOW.md` da ghi ro Member 4 can confirm client GUI scope trong Phase 0.
- `Code/README.md` da khoa folder boundary va ownership: `ClientApp/` la khu vuc chinh cua Member 4.
- `Code/ClientApp/` da co cau truc nen tang de bat dau cac task tiep theo.

Ket luan cua Member 4: Phase 0 da hoan thanh sau khi xac nhan lai client scope, ownership, dependency, demo mode, machine-bound login rule, va gioi han MVP UI.

### Phase 1

- review contract de biet client nhan/gui gi

### Phase 2

- chuan bi service boundary cho client UI

### Phase 3

- noi client login UI vao auth result thuc
- hien loi ro neu login sai `machineId`

### Phase 4

- build connect form
- build login shell
- build main client shell
- build lock screen shell

### Phase 5

- bind client UI vao login/status/lock/unlock flow thuc
- hien ket noi va trang thai ro rang

### Phase 6

- finish notification/timer/chat display
- finish unlock reaction
- finish status presentation can thiet
- giu chat o muc 1-1 text toi thieu, khong history, khong emoji

### Phase 7

- fix disconnect/reconnect UX
- fix invalid packet crash risk
- fix lock state hoac stale UI

### Phase 8

- chi sua release-blocking client UI bug
- rehearse client demo path

### Phase 9

- demo client behavior

## 6. Ke hoach theo tuan

### Week 1

- create connect screen
- create login shell
- create main shell
- create lock screen shell
- dam bao client shell mo duoc trong runnable skeleton
- client UI chi goi service/interface hoac placeholder, khong parse packet truc tiep trong form

### Week 2

- bind UI vao service interfaces
- chuan bi receive-state rendering

### Week 3

- bind client vao core flow: login, status, lock/unlock

### Week 4

- finish notification, timer, chat, lock flow

### Week 5

- fix reconnect, disconnect, invalid state, UI stability

### Week 6

- test client trong ca real LAN va local multi-instance

### Week 7

- rehearse release candidate va cleanup UI boundary

### Week 8

- rehearse final client-side demo

## 7. Handoff cho nguoi khac

Ban can noi ro:

- client can nhan event nao
- client can render state nao
- lock screen can input gi
- phan nao da noi that, phan nao con placeholder

## 8. Nguyen tac tranh xung dot

- UI chi render state va goi action, khong tu invent packet
- Member 2 own networking core
- Member 5 own auth logic
- ban khong sua server-side code de fix client issue neu chua duoc giao

## 9. Deliverables

- connect form
- login flow
- main client UI
- full-screen lock screen
- notification/timer/chat handling

## 10. Definition of Done

- client connect va login duoc
- client nhan lenh realtime dung
- lock screen chay dung
- reconnect/disconnect khong lam vo trai nghiem co ban
