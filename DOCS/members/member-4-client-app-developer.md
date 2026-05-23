# Member 4 - Client App Developer

## Role

Ban own client app. Muc tieu la client co the connect, login voi `machineId`, nhan realtime command, hien lock screen, notification, timer, chat, va giu UX on dinh cho demo.

Tracker tien do nam trong `DOCS/TASKS.md`. File nay chi la playbook ca nhan, khong tick task tai day.

## Write Scope

Ban duoc uu tien sua:

- `Code/ClientApp/`
- client-side forms/views
- client-side UI bridge/services neu chi phuc vu presentation

## Non-Owned Scope

Ban khong own:

- packet schema hoac shared DTO contract cua M2
- server dashboard cua M3
- database/auth internals cua M5
- release/gate decision cua M1

## Dependencies

- Can M2 cung cap client network/service interface.
- Can M2/M5 cung cap login result flow va error codes.
- Can M1 chot scope va phase order.
- Can M6 feedback tu client/demo/regression tests.

## Handoff Rules

- Noi ro client can nhan event nao, render state nao, action nao con placeholder.
- UI chi render state va goi service; khong invent packet shape trong form.
- Sai `machineId` phai hien loi ro khi auth flow co result.
- Khong sua server-side code de fix client issue neu chua duoc giao.

## My 8-Week Flow

### Week 1

- `W1.P1`: confirm client GUI scope, write scope, demo modes, machine-bound login rule.
- `W1.P2`: review API va list client input/output needs.
- `W1.P3`: create connect screen, login shell, main shell, lock screen shell.

### Week 2

- `W2.P1`: consume client-side network service/interface, khong parse packet in forms.
- `W2.P2`: prepare client login UI for real auth result.
- `W2.P3`: finish client connect/login/main/lock shells.

### Week 3

- `W3.P1`: bind client login UI to real auth response.
- `W3.P2`: send status after login and local state changes.
- `W3.P3`: react to one command and return visible result.

### Week 4

- `W4.P1`: show lock screen and exit lock screen on unlock.
- `W4.P2`: send ACK after command execution or clear failure.
- `W4.P3`: fix lock-state, stale state, and command-state regression issues.

### Week 5

- `W5.P1`: show notification with simple severity.
- `W5.P2`: show remaining timer on client.
- `W5.P3`: send/receive client chat messages.

### Week 6

- `W6.P1`: keep each client instance isolated by account and `machineId`.
- `W6.P2`: harden reconnect/disconnect UX.
- `W6.P3`: verify client setup path in local and real LAN modes.

### Week 7

- `W7.P1`: fix release-blocking client UI bugs only.
- `W7.P2`: clean client UI/service boundaries without behavior change.
- `W7.P3`: rehearse client demo path.

### Week 8

- `W8.P1`: rehearse client behavior and lock/unlock reaction.
- `W8.P2`: avoid client changes unless release-blocking.
- `W8.P3`: demonstrate client app.

## Definition of Done

- Client connect/login path is visible and clear.
- Client receives and reacts to commands correctly.
- Lock screen, notification, timer, and chat display match MVP.
- Reconnect/disconnect cases do not break the basic demo experience.
