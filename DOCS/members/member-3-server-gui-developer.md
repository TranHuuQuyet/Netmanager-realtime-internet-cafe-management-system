# Member 3 - Server GUI Developer

## 1. Vai tro

Ban phu trach giao dien admin cua server.
Muc tieu cua ban la tao dashboard de admin dang nhap, xem trang thai may, va thao tac lock/unlock/notification/timer/chat mot cach ro rang, nhanh, de demo.

## 2. Write Scope

Ban duoc uu tien sua:

- `ServerApp/`
- server-side forms
- server-side views
- server-side UI bridge/services neu can cho UI

Ban khong nen sua:

- packet schema
- socket dispatcher
- client UI
- database/auth internals

## 3. Thu ban so huu

- admin login form
- dashboard shell
- machine list
- machine status rendering
- admin control area
- admin-side realtime display
- UI thread-safe update pattern cho server app

## 4. Dependency cua ban

- packet/service interface tu Member 2
- auth result tu Member 5
- phase order va scope tu Member 1
- machine-bound login rule tu Member 1, 2, 5

## 5. Nhiem vu theo phase

### Phase 0

- review scope giao dien admin

Phase 0 completion note:

- Da review va accept scope giao dien admin.
- Server GUI scope gom: admin login form, dashboard shell, machine list/status view, control panel, admin-side realtime display.
- Write scope chinh la `Code/ServerApp/`, uu tien forms/views/UI-facing services.
- Khong own packet schema, socket dispatcher, client UI, database/auth internals.
- Dependency da xac nhan nhung chua duoc cung cap: Member 2 chua co network-facing interface, Member 5 chua co auth result shape.
- Day la dependency cho Phase 1/Phase 2 handoff, khong phai blocker cho viec hoan thanh Phase 0 cua Member 3.
- Phase 0 khong yeu cau runtime form code; `Code/ServerApp/` hien moi la folder skeleton va se duoc build trong phase/week tiep theo.

### Phase 1

- review `API.md` de dam bao dashboard can gi se co trong contract

### Phase 2

- khong tu viet protocol
- chuan bi UI service boundary cho network input

### Phase 3

- noi admin login UI vao auth result thuc
- hien loi ro neu account dung nhung sai `machineId`

### Phase 4

- [x] build login form
- [x] build dashboard shell
- [x] build machine list stub
- [x] build control panel
- [x] prepare admin login view for `username + password + machineId`
- [x] clean visible Vietnamese text/encoding in the forms
- [x] move remaining visible UI strings into reusable resource-backed helper

### Phase 5

- bind dashboard vao state thuc
- hien ket qua command va machine state thuc
- hien ack neu co

### Phase 6

- finish notification area
- finish timer display
- finish chat/admin interaction area
- giu chat o muc text don gian, khong history, khong file/image

### Phase 7

- fix cross-thread, lag, stale state, reconnect UI issues

### Phase 8

- chi sua release-blocking server UI bug
- rehearse dashboard demo path

### Phase 9

- demo admin dashboard

## 6. Ke hoach theo tuan

### Week 1

- [x] create server login shell
- [x] create dashboard shell
- [x] create machine list stub
- [x] dam bao server shell mo duoc trong runnable skeleton
- [x] chi bind UI qua service/interface hoac placeholder, khong parse packet truc tiep trong form
- [x] add missing `machineId` input if leader keeps it required for admin login view
- [x] remove temporary login textbox test values
- [x] fix mojibake/encoding in visible UI strings
- [x] move visible UI strings into reusable `UiStrings` resource helper

### Week 2

- bind UI vao service interfaces
- chuan bi status rendering

### Week 3

- bind dashboard vao login/status/lock/unlock flow thuc

### Week 4

- finish notification/timer/chat/admin controls

### Week 5

- fix server UI stability, thread-safety, reconnect display

### Week 6

- test server UI trong ca real LAN va local multi-instance

### Week 7

- rehearse release candidate va cleanup UI boundary

### Week 8

- rehearse final admin-side demo

## 7. Handoff cho nguoi khac

Ban can noi ro:

- UI can nhan state nao
- UI can goi action nao
- state nao la loading, error, success
- control nao da noi that, control nao con stub

## 8. Nguyen tac tranh xung dot

- chi goi service/interface, khong parse packet trong form neu co the tach
- Member 2 own transport, ban own presentation
- auth flow phai theo result cua Member 5
- khong sua `Shared` de "cho nhanh"

## 9. Deliverables

- admin login form
- dashboard shell
- machine list/state view
- control panel
- realtime admin UI

## 10. Definition of Done

- admin dang nhap duoc
- machine state thay duoc tren dashboard
- admin thao tac command thay ket qua ro
- UI khong freeze va khong vo khi network event den
