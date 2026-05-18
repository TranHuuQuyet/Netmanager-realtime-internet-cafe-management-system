# Member 2 - Network Engineer

## 1. Vai tro

Ban phu trach toan bo lop networking va shared transport contract.
Muc tieu cua ban la giu cho server va client noi chuyen voi nhau on dinh, dung schema, khong block UI, va de cac module khac tich hop duoc.

## 2. Write Scope

Ban duoc uu tien sua:

- `Shared/`
- `ServerApp/Networking/`
- `ClientApp/Networking/`
- `DOCS/API.md`

Ban khong nen sua:

- server UI cua Member 3
- client UI cua Member 4
- database/auth implementation cua Member 5

## 3. Thu ban so huu

- packet envelope
- packet types
- serializer/parser
- dispatcher
- TCP listener
- TCP connector
- connection/session object
- disconnect/reconnect handling

## 4. Dependency cua ban

- login flow can auth interface tu Member 5
- GUI members can interface de bind UI
- contract change can duoc Member 1 approve
- can ho tro ca real LAN va local multi-instance

## 5. Nhiem vu theo phase

### Phase 0

- review networking feasibility
- xac nhan framing va packet assumptions

### Phase 1

- chot packet contract trong `API.md`
- chot DTO va parse rule
- chot invalid packet behavior
- chot field `machineId` trong login flow

### Phase 2

- implement TCP listener
- implement client connector
- implement send/receive loop
- implement serializer/parser
- tao dispatcher skeleton
- tao cach test local multi-instance de dev hang ngay

### Phase 3

- noi login packet flow voi auth interface
- tra auth result theo contract

### Phase 4

- expose network-facing interface cho GUI
- ho tro stub hoac service shape de Member 3/4 noi vao

### Phase 5

- route status, lock, unlock, ack
- noi event tu network sang UI-facing service

### Phase 6

- finish notification, timer, chat, broadcast, direct message
- finish ack handling
- giu chat o muc 1-1 text toi thieu, khong history

### Phase 7

- harden timeout, disconnect, reconnect
- harden malformed packet handling
- improve log output

### Phase 8

- chi sua release-blocking network bug
- support final setup test

### Phase 9

- support demo network setup
- support live troubleshooting neu co

## 6. Ke hoach theo tuan

### Week 1

- draft packet contract
- tao hoac ho tro `Shared` packet model baseline
- tao networking skeleton
- tao connect/send/receive proof
- ghi ro local multi-instance connection assumption trong docs

### Week 2

- finish listener, connector, parser, dispatcher skeleton
- expose login/status path

### Week 3

- noi core integration: login, status, lock/unlock, ack

### Week 4

- finish notification, timer, chat, broadcast/direct flow

### Week 5

- fix disconnect, reconnect, timeout, malformed packet bugs

### Week 6

- chay real LAN smoke test
- chay local multi-instance smoke test

### Week 7

- support release candidate
- cleanup boundaries neu can ma khong doi behavior

### Week 8

- support final demo va fallback mode

## 7. Handoff cho nguoi khac

Ban phai cung cap cho Member 3, 4, 5:

- packet shape ro rang
- service interface ro rang
- event hoac callback ro rang
- sample input/output ro rang

## 8. Nguyen tac tranh xung dot

- ban la owner duy nhat cua packet implementation
- GUI khong parse packet truc tiep trong form neu da co service
- auth duoc goi qua interface, khong embed SQL vao network layer
- doi packet thi update `API.md` cung ngay

## 9. Deliverables

- TCP server/client core
- serializer/parser
- dispatcher
- connection state handling
- reconnect/disconnect handling
- multi-client note

## 10. Definition of Done

- 2 den 3 clients connect on dinh
- packet dung schema da chot
- UI khong bi block boi socket work
- disconnect/invalid packet khong lam sap app
