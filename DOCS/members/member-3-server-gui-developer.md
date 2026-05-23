# Member 3 - Server GUI Developer

## Role

Ban own admin/server UI. Muc tieu la dashboard de admin login, xem machine state, gui lock/unlock/notification/timer/chat, va thay ket qua command ro rang.

Tracker tien do nam trong `DOCS/TASKS.md`. File nay chi la playbook ca nhan, khong tick task tai day.

## Write Scope

Ban duoc uu tien sua:

- `Code/ServerApp/`
- server-side forms/views
- server-side UI bridge/services neu chi phuc vu presentation

## Non-Owned Scope

Ban khong own:

- packet schema hoac shared DTO contract cua M2
- socket dispatcher cua M2
- client UI cua M4
- database/auth internals cua M5
- release/gate decision cua M1

## Dependencies

- Can M2 cung cap network-facing service/interface va state events.
- Can M5 cung cap admin auth result va session behavior.
- Can M1 chot scope va phase order.
- Can M6 feedback tu UI/demo/regression tests.

## Handoff Rules

- Noi ro UI can nhan state nao, goi action nao, hien loading/error/success ra sao.
- UI chi consume service/interface; khong parse packet trong form.
- Neu control con placeholder, ghi ro de M6 test dung expectation.
- Khong sua `Shared` hoac auth/db de "fix nhanh" neu chua duoc giao.

## My 8-Week Flow

### Week 1

- `W1.P1`: confirm server GUI scope, write scope, non-owned areas, dependencies.
- `W1.P2`: review API va list dashboard data/action needs.
- `W1.P3`: create server login shell, dashboard shell, machine list/control placeholders.

### Week 2

- `W2.P1`: consume server-side network service/interface, khong parse packet in forms.
- `W2.P2`: prepare admin login UI for real auth result.
- `W2.P3`: finish dashboard status areas and command placeholders.

### Week 3

- `W3.P1`: bind admin login UI to real auth response.
- `W3.P2`: render online/offline/status state on dashboard.
- `W3.P3`: show one client state and command result on dashboard.

### Week 4

- `W4.P1`: wire admin lock/unlock controls to service calls.
- `W4.P2`: show command result/error on dashboard.
- `W4.P3`: fix stale state, cross-thread display, and control regression issues.

### Week 5

- `W5.P1`: add admin notification action and result display.
- `W5.P2`: show timer state on admin side if required.
- `W5.P3`: send/receive admin chat messages.

### Week 6

- `W6.P1`: render multiple clients clearly on dashboard.
- `W6.P2`: harden server UI thread safety and error states.
- `W6.P3`: verify server UI path in local and real LAN modes.

### Week 7

- `W7.P1`: fix release-blocking server UI bugs only.
- `W7.P2`: clean server UI/service boundaries without behavior change.
- `W7.P3`: rehearse server dashboard demo path.

### Week 8

- `W8.P1`: rehearse admin dashboard operations.
- `W8.P2`: avoid server UI changes unless release-blocking.
- `W8.P3`: demonstrate server dashboard.

## Definition of Done

- Admin login flow is visible and clear.
- Dashboard shows machine state from real service data.
- Admin command actions show result/error.
- Server UI remains responsive when network events arrive.
