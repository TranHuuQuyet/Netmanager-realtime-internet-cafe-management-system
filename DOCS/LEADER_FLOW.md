# LEADER FLOW

## Purpose / Source of Truth

`LEADER_FLOW.md` la master working flow cho toan team trong baseline `8 tuan`.
Member mo file nay de biet project lam gi, ai own phan nao, tuan nao lam phase nao, va moi phase minh phai lam gi.

Source of truth (nguon dung duy nhat):

- `DOCS/LEADER_FLOW.md`: flow tong, ownership, working rules, gate, definition of done.
- `DOCS/TASKS.md`: tracker duy nhat co checkbox. Member tick tien do tai day.
- `DOCS/API.md`: packet, auth, session, error, va shared model contract.
- `DOCS/BUGS.md`: risk, bug, blocker, limitation.
- `DOCS/RUN_GUIDE.md`: build/run/setup cho local va LAN.
- `DOCS/TEST_MATRIX.md`: test case va status test.
- `DOCS/DEMO_CHECKLIST.md`: demo path, fallback path, demo roles.
- `DOCS/members/`: member playbook, khong phai noi tick task.

Rule quan trong:

- Neu flow va tracker lech nhau, Member 1 sua flow/tracker truoc khi team tiep tuc.
- Neu packet/schema thay doi, `API.md` phai duoc update cung ngay.
- Neu implementation va docs lech nhau, Member 6 ghi bug/docs mismatch va Member 1 chot owner sua.

## MVP Scope, Stack, Demo Modes

### MVP in scope

- TCP connection giua server va client.
- Admin login va client login.
- Account-to-`machineId` validation.
- Realtime machine status.
- Lock/unlock command.
- ACK/error handling cho command quan trong.
- Notification.
- Timer update.
- 1-1 text chat toi thieu.
- Multi-client demo co ban.
- Local multi-instance demo va real LAN demo.

### Out of scope

- File transfer.
- Voice/video.
- Advanced reporting.
- Advanced analytics.
- Cloud deployment.
- Chat group, emoji, history, file/image, resend queue.
- Late UI polish neu core demo chua on.

### Stack

- `.NET 8`
- C#
- Windows Forms
- TCP Socket
- SQLite
- `System.Text.Json`

### Demo modes

- `Mode A - Real LAN Demo`: mot server machine, nhieu client machine trong cung LAN.
- `Mode B - Local Multi-Instance Demo`: mot machine chay server va nhieu client instance, dung cho dev/fallback.

### Identity rules

- Moi client account gan voi mot `machineId`.
- Server validate `username`, `password`, va `machineId`.
- Correct account + wrong `machineId` phai bi reject ro rang.
- Vi du: `pc01` gan voi `PC-01`, `pc02` gan voi `PC-02`.

## Team Ownership Map

| Member | Role | Owns | Write scope chinh |
| --- | --- | --- | --- |
| M1 | Team Leader / System Architect | scope, architecture, task order, gate, release approval | `DOCS/LEADER_FLOW.md`, `DOCS/TASKS.md`, `DOCS/BUGS.md`, `README.md` |
| M2 | Network Engineer | packet contract implementation, TCP, parser/serializer, dispatcher, reconnect | `Code/Shared/`, `Code/ServerApp/Networking/`, `Code/ClientApp/Networking/`, `DOCS/API.md` |
| M3 | Server GUI Developer | admin login, dashboard, machine list, control panel, admin realtime display | `Code/ServerApp/`, server UI files |
| M4 | Client App Developer | connect screen, client login, main screen, lock screen, client realtime display | `Code/ClientApp/`, client UI files |
| M5 | Database & Authentication | SQLite schema, repository, auth, session, seed data, machine mapping | `Code/ServerApp/Auth/`, `Code/ServerApp/Database/` |
| M6 | Tester & Documentation | tests, bugs, run guide, demo checklist, docs consistency | `DOCS/BUGS.md`, `DOCS/RUN_GUIDE.md`, `DOCS/TEST_MATRIX.md`, `DOCS/DEMO_CHECKLIST.md`, `README.md` |

Non-owned rule:

- M2 khong sua UI/auth internals de fix nhanh.
- M3/M4 khong invent packet shape trong form.
- M5 khong embed auth behavior vao network/UI layer.
- M6 khong sua runtime logic neu chua duoc owner giao.
- M1 co quyen approve scope/contract/release, nhung khong bypass owner runtime.

## Working Rules

### Branch rules

- Main integration branch: `main`
- Member branch: `member-<number>/<short-task>`
- Integration branch: `integration/<flow-name>`
- Hotfix branch: `hotfix/<short-fix>`

Examples:

- `member-2/network-core`
- `member-5/auth-schema`
- `integration/login-flow`
- `hotfix/ack-error-display`

### Commit rules

- One commit, one logical job.
- Format: `<scope>: <short action>`
- Scopes: `docs`, `shared`, `server`, `client`, `auth`, `db`, `test`, `build`, `fix`
- Contract changes update `DOCS/API.md` in the same commit or same review batch.

Examples:

- `docs: align week 1 tracker`
- `shared: add packet envelope model`
- `auth: validate machine-bound login`
- `fix: handle invalid packet response`

### Handoff rules

Before handoff, owner phai cung cap:

- stable-enough interface shape
- input/output expected
- sample packet/data shape neu co
- known limitations
- current blockers
- next expected action

Receiving owner phai:

- consume agreed interface
- khong doi contract ngam
- bao ngay neu missing field, missing event, hoac mismatch behavior

### Conflict control

- Packet/schema: M2 lead, M1 approve, M5 review auth/session part.
- Auth/session/database: M5 lead, M1 approve khi contract anh huong module khac.
- Server UI: M3 lead.
- Client UI: M4 lead.
- Test/docs mismatch: M6 report, M1 assign owner.
- Shared files chi sua khi owner lien quan da thong nhat.

## Unified 8-Week Workflow

### Fixed phase map

| Week | Flow |
| --- | --- |
| Week 1 | `W1.P1 Kickoff & Ownership`, `W1.P2 API Contract`, `W1.P3 Scaffold & First Round Trip` |
| Week 2 | `W2.P1 Network Core`, `W2.P2 Auth/DB Core`, `W2.P3 UI Skeleton Completion` |
| Week 3 | `W3.P1 Login Integration`, `W3.P2 Status Sync`, `W3.P3 One-Client Core Demo` |
| Week 4 | `W4.P1 Lock/Unlock`, `W4.P2 ACK/Error Handling`, `W4.P3 Control Regression` |
| Week 5 | `W5.P1 Notification`, `W5.P2 Timer`, `W5.P3 1-1 Chat` |
| Week 6 | `W6.P1 Multi-Client`, `W6.P2 Stability Hardening`, `W6.P3 Demo Mode Validation` |
| Week 7 | `W7.P1 Regression & Bug Triage`, `W7.P2 Source/Docs Cleanup`, `W7.P3 Release Candidate` |
| Week 8 | `W8.P1 Final Rehearsal`, `W8.P2 Release Lock`, `W8.P3 Demo & Archive` |

### Week 1 - Kickoff, Contract, Scaffold

#### W1.P1 Kickoff & Ownership

Goal: freeze MVP, stack, scope, ownership, branch/commit rules, and demo modes.

- M1: chot MVP, out-of-scope, ownership map, source of truth, branch rule, commit rule.
- M2: confirm TCP/JSON-line feasibility, initial packet boundary, local multi-instance assumption.
- M3: confirm server GUI scope, write scope, non-owned packet/auth/client areas.
- M4: confirm client GUI scope, write scope, non-owned packet/server/auth areas.
- M5: confirm auth/database ownership, machine-bound login rule, seed data responsibility.
- M6: confirm bug format, test matrix format, run guide/demo checklist ownership.

#### W1.P2 API Contract

Goal: make `DOCS/API.md` stable enough for first implementation.

- M1: review API with M2/M5 and approve contract baseline.
- M2: define packet envelope, packet types, parser/serializer rules, invalid packet behavior.
- M3: list dashboard data/action needs against the contract.
- M4: list client input/output and command reaction needs against the contract.
- M5: define auth result, session state, user/session fields, machine validation output.
- M6: derive first test cases and flag doc contradictions.

#### W1.P3 Scaffold & First Round Trip

Goal: create runnable solution scaffold and prove one basic TCP JSON-line round trip.

- M1: confirm `Code/NetManager.sln`, project references, and Week 1 handoff order.
- M2: create shared packet baseline, networking skeleton, connect/send/receive proof.
- M3: create server login shell, dashboard shell, machine list/control placeholders.
- M4: create client connect shell, login shell, main shell, lock screen shell.
- M5: create auth service skeleton, schema/session draft, machine validation draft.
- M6: create bug template, test matrix, connection checklist, demo-mode checklist.

### Week 2 - Core Foundations

#### W2.P1 Network Core

Goal: make connection, receive loop, dispatcher, and basic status path reliable.

- M1: remove blockers and prevent contract drift.
- M2: finish TCP listener, client connector, send/receive loop, dispatcher skeleton.
- M3: consume server-side network service/interface without parsing packets in forms.
- M4: consume client-side network service/interface without parsing packets in forms.
- M5: provide auth interface shape needed by network login path.
- M6: run connection, invalid packet, and local multi-instance checks.

#### W2.P2 Auth/DB Core

Goal: make auth and persistence basics callable by networking.

- M1: review auth/network ownership boundary.
- M2: call auth interface through agreed contract only.
- M3: prepare admin login UI for real auth result.
- M4: prepare client login UI for real auth result.
- M5: implement `Users`, `Sessions`, repository skeleton, auth validation, seed baseline.
- M6: create valid/invalid login and wrong `machineId` test cases.

#### W2.P3 UI Skeleton Completion

Goal: both apps can open and show the expected demo screens.

- M1: verify UI scope stays within MVP.
- M2: provide UI-facing events/stubs for connection and state.
- M3: finish dashboard skeleton, status areas, command placeholders.
- M4: finish client connect/login/main/lock shells.
- M5: provide auth stubs or service contracts to UI owners.
- M6: verify screens cover the planned demo path.

### Week 3 - First End-to-End Flow

#### W3.P1 Login Integration

Goal: login works through UI, network, auth, and response contract.

- M1: define login integration order and assign blocker owners.
- M2: route `LOGIN` request/response and map network errors.
- M3: bind admin login UI to real auth response.
- M4: bind client login UI to real auth response.
- M5: validate username/password/role/`machineId` and create session state.
- M6: test admin login, client login, bad password, wrong `machineId`.

#### W3.P2 Status Sync

Goal: dashboard sees client state through real `STATUS` updates.

- M1: confirm status model is not drifting from API.
- M2: route `STATUS` heartbeat/state changes.
- M3: render online/offline/status state on dashboard.
- M4: send status after login and local state changes.
- M5: support session state fields if status depends on persistence.
- M6: test status after login and stale/disconnect status behavior.

#### W3.P3 One-Client Core Demo

Goal: one admin and one client can complete the core demo path.

- M1: freeze non-essential work until one-client core flow works.
- M2: connect login, status, lock/unlock route skeleton, and ACK visibility.
- M3: show one client state and command result on dashboard.
- M4: react to one command and return visible result.
- M5: support auth/session fixes found during integration.
- M6: record integration bugs with exact repro steps.

### Week 4 - Control Flow

#### W4.P1 Lock/Unlock

Goal: lock/unlock commands work from server to client.

- M1: keep team focused on control flow before secondary features.
- M2: finish `LOCK` and `UNLOCK` routing.
- M3: wire admin lock/unlock controls to service calls.
- M4: show lock screen and exit lock screen on command.
- M5: verify active session and machine ownership before command flow.
- M6: test lock/unlock in local mode and record failures.

#### W4.P2 ACK/Error Handling

Goal: success/failure is visible and invalid input fails gracefully.

- M1: approve error behavior and severity for blockers.
- M2: finish `ACK`, error packet handling, invalid packet guard.
- M3: show command result/error on dashboard.
- M4: send ACK after command execution or clear failure.
- M5: return deterministic auth/session errors where relevant.
- M6: test ACK success, ACK failure, invalid packet, unsupported packet.

#### W4.P3 Control Regression

Goal: login, status, lock/unlock, ACK remain stable together.

- M1: decide whether control gate can pass.
- M2: fix network regressions in core control path.
- M3: fix server UI stale state or cross-thread display issues.
- M4: fix client lock-state or command-state issues.
- M5: fix session consistency issues.
- M6: run Gate D regression and update bug status.

### Week 5 - Remaining MVP Features

#### W5.P1 Notification

Goal: admin can send simple notification to client(s).

- M1: keep notification scope small.
- M2: route direct/broadcast `NOTIFICATION`.
- M3: add admin notification action and result display.
- M4: show notification with simple severity.
- M5: persist nothing unless already required.
- M6: test direct and broadcast notification behavior.

#### W5.P2 Timer

Goal: timer updates are visible and consistent.

- M1: decide whether timer persistence is needed for MVP demo.
- M2: route `TIMER` updates.
- M3: show timer state on admin side if needed.
- M4: show remaining time on client.
- M5: support timer/session persistence only if approved.
- M6: test timer update, expiry path, and reset assumptions.

#### W5.P3 1-1 Chat

Goal: minimal admin-client text chat works.

- M1: enforce 1-1 text-only chat scope.
- M2: route direct `CHAT` messages.
- M3: send/receive admin chat messages in dashboard.
- M4: send/receive client chat messages.
- M5: avoid chat history unless explicitly approved.
- M6: test one admin/client chat path and confirm no scope creep.

### Week 6 - Multi-Client and Stability

#### W6.P1 Multi-Client

Goal: 2 to 3 clients can connect and remain distinct.

- M1: prioritize multi-client blockers.
- M2: manage multiple connections, session routing, duplicate login behavior.
- M3: render multiple clients clearly on dashboard.
- M4: keep each client instance isolated by account and `machineId`.
- M5: enforce account-machine mapping and duplicate active session rules.
- M6: test 2 to 3 clients in local multi-instance mode.

#### W6.P2 Stability Hardening

Goal: remove demo-breaking instability.

- M1: freeze feature work and rank bugs by severity.
- M2: harden disconnect, reconnect, timeout, malformed packet handling.
- M3: harden server UI thread safety and error states.
- M4: harden client reconnect/disconnect UX.
- M5: harden DB/auth failure behavior and data consistency.
- M6: run regression and verify fixed bugs.

#### W6.P3 Demo Mode Validation

Goal: both demo modes are documented and at least smoke-tested.

- M1: decide accepted fallback if real LAN is unstable.
- M2: verify local and real LAN network assumptions.
- M3: verify server UI path in both modes.
- M4: verify client setup path in both modes.
- M5: verify seed accounts and machine mapping in both modes.
- M6: run local multi-instance and real LAN smoke checks.

### Week 7 - Release Candidate

#### W7.P1 Regression & Bug Triage

Goal: close or explicitly accept all demo-impacting bugs.

- M1: classify bugs and decide fix/accept/defer.
- M2: fix release-blocking network bugs only.
- M3: fix release-blocking server UI bugs only.
- M4: fix release-blocking client UI bugs only.
- M5: fix release-blocking auth/DB bugs only.
- M6: maintain bug severity list and regression status.

#### W7.P2 Source/Docs Cleanup

Goal: clean boundaries and docs without risky redesign.

- M1: approve cleanup scope and block unrelated refactors.
- M2: clean network/shared boundaries without changing behavior.
- M3: clean server UI/service boundaries without changing behavior.
- M4: clean client UI/service boundaries without changing behavior.
- M5: clean auth/data boundaries without changing behavior.
- M6: align README, run guide, test matrix, demo checklist with build.

#### W7.P3 Release Candidate

Goal: create release candidate for final rehearsal.

- M1: approve release candidate and code freeze rules.
- M2: support setup and network verification.
- M3: rehearse server dashboard demo path.
- M4: rehearse client demo path.
- M5: verify demo accounts, seed data, auth/session behavior.
- M6: prepare final regression report draft and known limitations.

### Week 8 - Final Demo

#### W8.P1 Final Rehearsal

Goal: run the demo exactly as planned.

- M1: lead rehearsal and decide fallback trigger.
- M2: validate network setup and fallback local mode.
- M3: rehearse admin dashboard operations.
- M4: rehearse client behavior and lock/unlock reaction.
- M5: rehearse explanation of auth, DB, session, machine mapping.
- M6: run final checklist and record rehearsal issues.

#### W8.P2 Release Lock

Goal: freeze final build and allow only approved emergency fixes.

- M1: lock release scope and approve any emergency fix.
- M2: avoid network changes unless release-blocking.
- M3: avoid UI changes unless release-blocking.
- M4: avoid client changes unless release-blocking.
- M5: avoid DB/auth changes unless release-blocking.
- M6: verify docs match final build and limitation list.

#### W8.P3 Demo & Archive

Goal: present, preserve evidence, and leave repo reviewable.

- M1: lead final demo and final decision notes.
- M2: support live network troubleshooting.
- M3: demonstrate server dashboard.
- M4: demonstrate client app.
- M5: explain auth/session/database flow.
- M6: archive demo notes, bug summary, final docs status.

## Gate Checklist By Week

### Week 1 Gate - Scope, Contract, Scaffold

- MVP, out-of-scope, stack, ownership, branch/commit rules are frozen.
- `DOCS/API.md` has packet baseline.
- `Code/NetManager.sln`, `ServerApp`, `ClientApp`, `Shared` project files exist.
- `ServerApp` and `ClientApp` reference `Shared`.
- Server/client UI shells can open or blocker is documented.
- One TCP JSON-line round trip works or remains the top W2 blocker.
- `RUN_GUIDE.md`, `TEST_MATRIX.md`, `DEMO_CHECKLIST.md`, `BUGS.md` are usable.

### Week 2 Gate - Network/Auth/UI Core

- TCP listener and client connector exist.
- Send/receive loop does not freeze UI.
- Auth interface and repository skeleton exist.
- Login UI screens can consume auth/network service shape.
- First real bugs are recorded.

### Week 3 Gate - One-Client Core

- Admin/client login path works through real contract.
- Client sends status and dashboard shows state.
- One-client core command route is visible.
- Integration bugs have owner and repro steps.

### Week 4 Gate - Control Flow

- Lock/unlock works.
- ACK/error result is visible.
- Wrong `machineId` behavior is deterministic.
- Invalid packet does not crash receiver.
- Core flow passes regression in local mode.

### Week 5 Gate - MVP Feature Completion

- Notification works.
- Timer works at MVP level.
- 1-1 text chat works.
- Feature scope did not expand beyond MVP.

### Week 6 Gate - Stability and Demo Modes

- 2 to 3 clients can be tested or blocker is documented.
- Disconnect/reconnect/timeout/malformed packet handling is acceptable.
- Local multi-instance smoke test passes.
- Real LAN smoke test passes or accepted fallback is documented.

### Week 7 Gate - Release Candidate

- No unaccepted high-severity demo blocker remains.
- Release candidate build is identified.
- Docs match current build.
- Known limitations are explicit.
- Code freeze rule is active.

### Week 8 Gate - Final Done

- Final smoke test passes.
- Demo checklist core path passes.
- Fallback path is ready.
- Final docs and archive notes are complete.
- Team can explain architecture, flow, limitations, and responsibilities.

## Leader Operating Routine

Daily routine for M1:

1. Check `DOCS/TASKS.md` by current week/phase.
2. Ask each member: done, in progress, blocked by, next action.
3. Confirm packet/state/auth assumptions did not change silently.
4. Assign one owner for each blocker.
5. Update docs when decisions change.
6. Stop work that exceeds MVP or conflicts with ownership.

Weekly routine for M1:

1. Compare the current build against the week gate.
2. Confirm next week can start without hidden blockers.
3. Review `BUGS.md` severity and accepted risks.
4. Confirm both demo modes remain supported or document fallback.
5. Announce next week's phase priority using `Wn.Pm` labels.

Release routine for M1:

1. Freeze feature work.
2. Approve release candidate.
3. Allow only release-blocking fixes.
4. Review demo checklist with M6.
5. Confirm every member can explain their module.
6. Confirm final build follows `RUN_GUIDE.md`.

## Final Definition of Done

Project is done when:

- Server and client connect reliably.
- Login works through chosen auth flow.
- Client account is bound to correct `machineId`.
- Dashboard shows realtime machine state.
- Client receives lock/unlock, notification, timer, and chat correctly.
- ACK/error result is visible for important commands.
- Multi-client demo works on Windows.
- Disconnect/reconnect/invalid packet cases do not crash the app.
- Bugs and limitations are documented honestly.
- `RUN_GUIDE.md`, `TEST_MATRIX.md`, `DEMO_CHECKLIST.md`, and README match the final build.
- Local multi-instance mode is usable.
- Real LAN mode is usable or accepted fallback is documented.
- Team can present architecture, ownership, flow, and limitations clearly.
