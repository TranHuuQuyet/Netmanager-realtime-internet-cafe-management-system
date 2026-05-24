# TASKS

`TASKS.md` la tracker duy nhat co checkbox. Member tick tien do tai day theo week/phase.

Tracking rules:

- Dung dung phase label `Wn.Pm`.
- Moi task co member owner va evidence.
- Neu task bi block, de checkbox pending va ghi blocker ngan trong evidence.
- Member playbook trong `DOCS/members/` khong dung checkbox.

Current focus: Week 1 contract/scaffold handoff, chuan bi cho Week 2 network/auth core.

## Week 1

### W1.P1 Kickoff & Ownership

- [x] M1 - Chot MVP, out-of-scope, ownership, stack, identity rule, demo modes, branch rule, commit rule; evidence: kickoff decisions migrated from old completion checklist.
- [x] M2 - Confirm TCP/JSON-line feasibility va packet boundary ban dau; evidence: `DOCS/API.md` baseline accepted.
- [x] M3 - Confirm server GUI scope, write scope, non-owned areas, dependencies; evidence: Member 3 kickoff review migrated.
- [x] M4 - Confirm client GUI scope, write scope, non-owned areas, demo modes, machine-bound login rule; evidence: Member 4 self-review migrated.
- [x] M5 - Confirm auth/database ownership, machine mapping validation, seed/auth/session responsibility; evidence: Member 5 completion checklist migrated.
- [x] M6 - Confirm bug/test/doc ownership and source-of-truth docs; evidence: old tracker marked bug/test matrix format confirmed.

### W1.P2 API Contract

- [ ] M1 - Review and accept `DOCS/API.md` with M2 and M5; evidence: API approval note or decision entry.
- [x] M2 - Finalize packet envelope and packet type baseline; evidence: existing checked task migrated from old tracker.
- [ ] M3 - Review API and list dashboard data/actions needed from M2/M5; evidence: comment/update in tracker or handoff note.
- [ ] M4 - Review API and list client input/output needs; evidence: client handoff note.
- [ ] M5 - Draft `Users`, `Sessions`, auth result, session state, wrong `machineId` validation; evidence: schema/auth note or code skeleton.
- [ ] M6 - Review docs for contradictions against API contract; evidence: mismatch list or "no mismatch" note.

### W1.P3 Scaffold & First Round Trip

- [x] M1 - Confirm `Code/NetManager.sln`, `ServerApp`, `ClientApp`, `Shared`, and project references exist; evidence: solution/project files present.
- [ ] M2 - Create serializer/parser rules, networking skeleton, and first TCP JSON-line round trip; evidence: server/client connect and one packet exchange local.
- [x] M3 - Complete server login shell, dashboard shell, machine list/control placeholder, machineId input, and resource-backed UI strings; evidence: `Code/ServerApp/Forms` and `Code/ServerApp/UiStrings.*`.
- [ ] M4 - Create client connect screen, login shell, main shell, and lock screen shell; evidence: client forms/screens exist and open.
- [ ] M5 - Create auth service skeleton and schema/session draft; evidence: `Code/ServerApp/Auth` and `Code/ServerApp/Database` skeleton or documented handoff.
- [x] M6 - Create bug report template, test matrix template, contract/connection checklist, and demo-mode checklist; evidence: `BUGS.md`, `TEST_MATRIX.md`, `DEMO_CHECKLIST.md`.

## Week 2

### W2.P1 Network Core

- [ ] M1 - Remove blocker between network/auth/UI owners; evidence: blocker owner assigned.
- [ ] M2 - Finish TCP listener, connector, send/receive loop, dispatcher skeleton; evidence: one server and one client exchange packet without UI freeze.
- [ ] M3 - Consume server-side network service/interface only; evidence: dashboard does not parse packet directly.
- [ ] M4 - Consume client-side network service/interface only; evidence: client UI does not parse packet directly.
- [ ] M5 - Provide auth interface shape for network login path; evidence: interface/model documented or implemented.
- [ ] M6 - Run connection, invalid packet, and local multi-instance checks; evidence: `TEST_MATRIX.md` Gate B status updated.

### W2.P2 Auth/DB Core

- [ ] M1 - Approve auth/network boundary and login integration order; evidence: tracker note.
- [ ] M2 - Call auth service through agreed interface; evidence: login route invokes auth layer.
- [ ] M3 - Prepare admin login UI for real auth result; evidence: UI handles success/failure state.
- [ ] M4 - Prepare client login UI for real auth result; evidence: UI handles success/failure/wrong machine state.
- [ ] M5 - Implement schema, repository skeleton, auth validation, session basics; evidence: auth/db code and seed baseline.
- [ ] M6 - Test valid login, invalid login, disabled account, wrong `machineId`; evidence: Gate C cases updated.

### W2.P3 UI Skeleton Completion

- [ ] M1 - Verify UI scope stays inside MVP; evidence: scope review note.
- [ ] M2 - Provide UI-facing connection/state events or stubs; evidence: service/event contract available.
- [ ] M3 - Finish dashboard status areas and command placeholders; evidence: admin demo path visible.
- [ ] M4 - Finish client connect/login/main/lock shells; evidence: client demo path visible.
- [ ] M5 - Provide auth stubs/service contracts to UI owners; evidence: auth result shape consumable.
- [ ] M6 - Verify screens cover final demo path; evidence: `DEMO_CHECKLIST.md` updated.

## Week 3

### W3.P1 Login Integration

- [ ] M1 - Define login integration order and blocker owners; evidence: integration note.
- [ ] M2 - Route `LOGIN` request/response and network errors; evidence: login packet round trip.
- [ ] M3 - Bind admin login UI to real auth response; evidence: admin login uses real result.
- [ ] M4 - Bind client login UI to real auth response; evidence: client login uses real result.
- [ ] M5 - Validate username/password/role/`machineId` and create session; evidence: auth tests pass.
- [ ] M6 - Test admin/client login, bad password, wrong `machineId`; evidence: Gate C status updated.

### W3.P2 Status Sync

- [ ] M1 - Confirm status model still matches API; evidence: API/status review note.
- [ ] M2 - Route `STATUS` heartbeat/state changes; evidence: server receives status.
- [ ] M3 - Render online/offline/status state on dashboard; evidence: dashboard updates from real state.
- [ ] M4 - Send status after login and local state changes; evidence: client emits `STATUS`.
- [ ] M5 - Support session fields needed by status flow; evidence: session state available.
- [ ] M6 - Test status after login and stale/disconnect behavior; evidence: Gate D status updated.

### W3.P3 One-Client Core Demo

- [ ] M1 - Freeze non-essential work until one-client core flow works; evidence: priority note.
- [ ] M2 - Connect login, status, lock/unlock route skeleton, ACK visibility; evidence: one-client flow trace.
- [ ] M3 - Show one client state and command result on dashboard; evidence: admin can inspect one client.
- [ ] M4 - React to one command and return visible result; evidence: client executes command path.
- [ ] M5 - Fix auth/session issues found in integration; evidence: login/session blockers closed.
- [ ] M6 - Record integration bugs with exact repro steps; evidence: `BUGS.md` entries.

## Week 4

### W4.P1 Lock/Unlock

- [ ] M1 - Keep control flow priority and block secondary feature detours; evidence: priority note.
- [ ] M2 - Finish `LOCK` and `UNLOCK` routing; evidence: command packets reach target client.
- [ ] M3 - Wire admin lock/unlock controls to service calls; evidence: buttons trigger real commands.
- [ ] M4 - Show lock screen and exit on unlock; evidence: client UI changes state correctly.
- [ ] M5 - Verify active session and machine ownership before command flow; evidence: session guard works.
- [ ] M6 - Test lock/unlock in local mode; evidence: Gate D cases updated.

### W4.P2 ACK/Error Handling

- [ ] M1 - Approve error behavior and blocker severity; evidence: error policy note.
- [ ] M2 - Finish `ACK`, error packet handling, invalid packet guard; evidence: ACK/error packets visible.
- [ ] M3 - Show command result/error on dashboard; evidence: admin sees success/failure.
- [ ] M4 - Send ACK after command execution or clear failure; evidence: client ACK path works.
- [ ] M5 - Return deterministic auth/session errors where relevant; evidence: error codes match API.
- [ ] M6 - Test ACK success, ACK failure, invalid packet, unsupported packet; evidence: test matrix updated.

### W4.P3 Control Regression

- [ ] M1 - Decide whether control gate can pass; evidence: gate decision note.
- [ ] M2 - Fix network regressions in core control path; evidence: no open high network blocker.
- [ ] M3 - Fix server UI stale state or cross-thread display issues; evidence: dashboard remains responsive.
- [ ] M4 - Fix client lock-state or command-state issues; evidence: client state remains consistent.
- [ ] M5 - Fix session consistency issues; evidence: active session state stable.
- [ ] M6 - Run Gate D regression and update bug status; evidence: `TEST_MATRIX.md` and `BUGS.md`.

## Week 5

### W5.P1 Notification

- [ ] M1 - Keep notification scope small; evidence: no scope expansion.
- [ ] M2 - Route direct/broadcast `NOTIFICATION`; evidence: packet reaches intended client(s).
- [ ] M3 - Add admin notification action and result display; evidence: admin can send notification.
- [ ] M4 - Show notification with simple severity; evidence: client notification visible.
- [ ] M5 - Confirm no persistence needed unless approved; evidence: decision note if persistence added.
- [ ] M6 - Test direct and broadcast notification behavior; evidence: Gate E status updated.

### W5.P2 Timer

- [ ] M1 - Decide timer persistence requirement for MVP; evidence: decision note.
- [ ] M2 - Route `TIMER` updates; evidence: timer packet reaches client.
- [ ] M3 - Show timer state on admin side if required; evidence: dashboard timer state visible.
- [ ] M4 - Show remaining time on client; evidence: client timer display updates.
- [ ] M5 - Support timer/session persistence if approved; evidence: persistence behavior documented.
- [ ] M6 - Test timer update, expiry path, and reset assumptions; evidence: Gate E status updated.

### W5.P3 1-1 Chat

- [ ] M1 - Enforce 1-1 text-only chat scope; evidence: no chat scope creep.
- [ ] M2 - Route direct `CHAT` messages; evidence: chat packet reaches receiver.
- [ ] M3 - Send/receive admin chat messages; evidence: admin chat panel works.
- [ ] M4 - Send/receive client chat messages; evidence: client chat UI works.
- [ ] M5 - Avoid chat history unless approved; evidence: no unintended persistence.
- [ ] M6 - Test one admin/client chat path; evidence: Gate E status updated.

## Week 6

### W6.P1 Multi-Client

- [ ] M1 - Prioritize multi-client blockers; evidence: blocker list ranked.
- [ ] M2 - Manage multiple connections, session routing, duplicate login behavior; evidence: 2 to 3 clients connect distinctly.
- [ ] M3 - Render multiple clients clearly on dashboard; evidence: dashboard distinguishes clients.
- [ ] M4 - Keep each client instance isolated by account and `machineId`; evidence: no cross-client state leak.
- [ ] M5 - Enforce account-machine mapping and duplicate active session rules; evidence: duplicate/wrong mapping tests handled.
- [ ] M6 - Test 2 to 3 clients in local multi-instance mode; evidence: Gate F/local notes updated.

### W6.P2 Stability Hardening

- [ ] M1 - Freeze feature work and rank bugs by severity; evidence: bug triage note.
- [ ] M2 - Harden disconnect, reconnect, timeout, malformed packet handling; evidence: stability tests pass.
- [ ] M3 - Harden server UI thread safety and error states; evidence: no UI freeze in receive events.
- [ ] M4 - Harden client reconnect/disconnect UX; evidence: client recovers or errors clearly.
- [ ] M5 - Harden DB/auth failure behavior and consistency; evidence: auth/db failures do not crash demo.
- [ ] M6 - Run regression and verify fixed bugs; evidence: bug statuses updated.

### W6.P3 Demo Mode Validation

- [ ] M1 - Decide accepted fallback if real LAN is unstable; evidence: fallback decision note.
- [ ] M2 - Verify local and real LAN network assumptions; evidence: mode test notes.
- [ ] M3 - Verify server UI path in both modes; evidence: server demo path checked.
- [ ] M4 - Verify client setup path in both modes; evidence: client demo path checked.
- [ ] M5 - Verify seed accounts and machine mapping in both modes; evidence: demo accounts work.
- [ ] M6 - Run local multi-instance and real LAN smoke checks; evidence: `DEMO_CHECKLIST.md` updated.

## Week 7

### W7.P1 Regression & Bug Triage

- [ ] M1 - Classify bugs as fix/accept/defer; evidence: bug triage complete.
- [ ] M2 - Fix release-blocking network bugs only; evidence: no high network blocker.
- [ ] M3 - Fix release-blocking server UI bugs only; evidence: no high server UI blocker.
- [ ] M4 - Fix release-blocking client UI bugs only; evidence: no high client UI blocker.
- [ ] M5 - Fix release-blocking auth/DB bugs only; evidence: no high auth/db blocker.
- [ ] M6 - Maintain bug severity list and regression status; evidence: `BUGS.md` and `TEST_MATRIX.md` current.

### W7.P2 Source/Docs Cleanup

- [ ] M1 - Approve cleanup scope and block unrelated refactors; evidence: cleanup decision note.
- [ ] M2 - Clean network/shared boundaries without changing behavior; evidence: build still passes.
- [ ] M3 - Clean server UI/service boundaries without changing behavior; evidence: server demo path unchanged.
- [ ] M4 - Clean client UI/service boundaries without changing behavior; evidence: client demo path unchanged.
- [ ] M5 - Clean auth/data boundaries without changing behavior; evidence: auth tests unchanged.
- [ ] M6 - Align README, run guide, test matrix, demo checklist with build; evidence: docs review complete.

### W7.P3 Release Candidate

- [ ] M1 - Approve release candidate and code freeze rules; evidence: RC note.
- [ ] M2 - Support setup and network verification; evidence: setup/network check done.
- [ ] M3 - Rehearse server dashboard demo path; evidence: rehearsal note.
- [ ] M4 - Rehearse client demo path; evidence: rehearsal note.
- [ ] M5 - Verify demo accounts, seed data, auth/session behavior; evidence: demo account check done.
- [ ] M6 - Prepare final regression report draft and known limitations; evidence: report draft ready.

## Week 8

### W8.P1 Final Rehearsal

- [ ] M1 - Lead rehearsal and decide fallback trigger; evidence: rehearsal decision note.
- [ ] M2 - Validate network setup and fallback local mode; evidence: network rehearsal pass.
- [ ] M3 - Rehearse admin dashboard operations; evidence: server demo path pass.
- [ ] M4 - Rehearse client behavior and lock/unlock reaction; evidence: client demo path pass.
- [ ] M5 - Rehearse auth/DB/session explanation; evidence: explanation notes ready.
- [ ] M6 - Run final checklist and record rehearsal issues; evidence: `DEMO_CHECKLIST.md` current.

### W8.P2 Release Lock

- [ ] M1 - Lock release scope and approve emergency fixes only; evidence: release lock note.
- [ ] M2 - Avoid network changes unless release-blocking; evidence: no unapproved network changes.
- [ ] M3 - Avoid server UI changes unless release-blocking; evidence: no unapproved server UI changes.
- [ ] M4 - Avoid client changes unless release-blocking; evidence: no unapproved client changes.
- [ ] M5 - Avoid DB/auth changes unless release-blocking; evidence: no unapproved auth/db changes.
- [ ] M6 - Verify docs match final build and limitation list; evidence: final docs review complete.

### W8.P3 Demo & Archive

- [ ] M1 - Lead final demo and final decision notes; evidence: demo complete.
- [ ] M2 - Support live network troubleshooting; evidence: network notes archived.
- [ ] M3 - Demonstrate server dashboard; evidence: server demo complete.
- [ ] M4 - Demonstrate client app; evidence: client demo complete.
- [ ] M5 - Explain auth/session/database flow; evidence: module explanation complete.
- [ ] M6 - Archive demo notes, bug summary, final docs status; evidence: final archive notes complete.
