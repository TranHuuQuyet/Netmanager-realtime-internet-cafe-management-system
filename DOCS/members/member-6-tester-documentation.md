# Member 6 - Tester & Documentation

## 1. Vai tro

Ban phu trach test, bug tracking, docs, va demo support.
Muc tieu cua ban la giup team biet phan nao da on, phan nao chua on, bug nao nghiem trong, va docs nao dang lech so voi implementation.

## 2. Write Scope

Ban duoc uu tien sua:

- `DOCS/BUGS.md`
- `DOCS/TASKS.md` khi duoc Member 1 giao cap nhat tracker
- `README.md`
- cac test note hoac run guide trong `DOCS/`

Ban khong nen sua runtime code tru khi dang verify bug va da duoc owner giao.

## 3. Thu ban so huu

- bug format
- test matrix
- regression notes
- demo checklist
- run guide
- docs consistency review
- real LAN checklist
- local multi-instance checklist

## 4. Dependency cua ban

- can output va build tu ca team
- can phase/gate tu Member 1
- can contract tu Member 2
- can auth/session behavior tu Member 5 de viet test case

## 5. Nhiem vu theo phase

### Phase 0

- tao bug template
- tao test matrix template
- tao demo-note structure

### Phase 1

- derive test case tu `API.md`
- xac nhan docs khong mau thuan nhau

### Phase 2

- test connection basics
- test invalid packet basics neu co the
- test local multi-instance launch path

### Phase 3

- test login success/fail
- test inactive user va DB error handling
- test wrong `machineId` login

### Phase 4

- review GUI shell co cover du demo flow khong

### Phase 5

- test core flow end-to-end
- ghi bug co reproduction step va severity

### Phase 6

- test notification, timer, chat, multi-client
- cap nhat docs theo behavior that
- xac nhan chat scope khong vuot quy dinh

### Phase 7

- run regression checklist
- verify bug fixes
- maintain known limitation list

### Phase 8

- produce final test report
- final demo checklist
- verify clean-machine setup docs

### Phase 9

- support demo
- ghi post-release note

## 6. Ke hoach theo tuan

### Week 1

- tao bug report template
- tao test matrix template
- tao connection/contract checklist
- tao `RUN_GUIDE.md`
- tao `DEMO_CHECKLIST.md`
- tao `DECISIONS.md` baseline
- xac nhan checklist co the dung de verify runnable skeleton cuoi Week 1

### Week 2

- test connect/login co ban
- record first real bugs

### Week 3

- test login -> status -> lock/unlock flow
- record integration bugs

### Week 4

- test timer, notification, chat, multi-client

### Week 5

- run regression
- verify fixes
- maintain known limitation list

### Week 6

- run real LAN smoke test
- run local multi-instance smoke test

### Week 7

- final smoke test cho release candidate
- verify run guide
- finalize demo checklist

### Week 8

- final checklist
- hold fallback demo steps

## 7. Handoff cho nguoi khac

Ban can cung cap cho owner bug:

- module anh huong
- steps to reproduce
- expected result
- actual result
- severity
- workaround neu co

## 8. Nguyen tac tranh xung dot

- ban ghi bug, khong tu sua logic neu chua duoc giao
- ban cap nhat docs theo implementation that
- ban khong doi contract neu khong qua Member 1 va Member 2

## 9. Deliverables

- `BUGS.md`
- test matrix
- regression notes
- run guide
- demo checklist
- final test report

## 10. Definition of Done

- bug duoc ghi ro va de lap lai
- docs de chay va de hieu
- demo checklist du de support buoi demo
- team biet ro muc do on dinh cua build hien tai
