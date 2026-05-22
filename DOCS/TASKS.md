# TASKS

## 1. Current Project State

Current phase:

- `Phase 1 - Architecture and Contract Freeze`

Current gate target:

- `Gate A - Scope and Contract Gate` passed
- Next gate target: `Gate B - Connection Gate`

Current reality:

- Phase 0 da hoan thanh va duoc dong bo trong docs
- leader flow da duoc lam lai
- role docs da duoc dong bo
- `Code/` da co `NetManager.sln`, `ServerApp`, `ClientApp`, va `Shared` project files
- `ServerApp` da co GUI skeleton cho login va main dashboard cua Member 3
- Member 3 da hoan thanh UI-only follow-up cho `ServerApp/Forms`, bao gom `machineId` input, tab order cleanup, visible Vietnamese text cleanup, va resource-backed UI strings
- runtime implementation day du van chua co: networking, auth/database, va real end-to-end flow con pending
- Week 1 phai chuyen du an tu documentation-first sang runnable skeleton
- deadline hien tai la 8 tuan
- project uu tien demo chay on dinh truoc

## 2. Phase 0 Completion Checklist

These decisions are frozen before deeper coding:

- [x] Confirm final source folder structure for `ServerApp`, `ClientApp`, `Shared`, `ServerApp/Auth`, and `ServerApp/Database`.
- [x] Confirm first accepted version of `DOCS/API.md`.
- [x] Freeze tech stack: `.NET 8`, C#, WinForms, TCP, SQLite, `System.Text.Json`.
- [x] Freeze account-per-machine rule with `username <-> machineId`.
- [x] Freeze supported demo modes: real LAN and local multi-instance.
- [x] Freeze minimal chat scope: 1-1 text only, no emoji, no history, no file/image.
- [x] Confirm Week 1 task ownership for all 6 members.
- [x] Confirm branch naming rule and commit naming rule.
- [x] Confirm bug report format and test matrix format.
- [x] Confirm source-of-truth docs:
  - `LEADER_FLOW.md` for phase, scope, ownership, branch rules, commit rules, and release decisions
  - `API.md` for packet/auth/session contract
  - `TASKS.md` for current execution status
  - `BUGS.md` for risks, unresolved issues, and runtime bugs

### Member 3 Phase 0 Review

Member 3 has reviewed and accepted the server GUI scope for Phase 0.

- [x] Confirmed owned UI scope:
  - admin login form
  - dashboard shell
  - machine list and machine status rendering
  - admin control area for lock, unlock, notification, timer, and chat
  - admin-side realtime display
  - WinForms UI thread-safe update pattern for server-side events
- [x] Confirmed write scope:
  - primary runtime area: `Code/ServerApp/`
  - server-side UI folders and UI-facing services only
- [x] Confirmed non-owned areas:
  - packet schema and shared DTOs stay with Member 2
  - socket dispatcher stays with Member 2
  - auth/database internals stay with Member 5
  - client UI stays with Member 4
- [x] Confirmed dependencies:
  - Member 2 still needs to provide network-facing service/interface shape
  - Member 5 still needs to provide admin auth result shape
  - Member 1 controls phase order and scope changes
- [x] Confirmed dependency status:
  - network-facing service/interface shape is not available yet
  - admin auth result shape is not available yet
  - these are Phase 1/Phase 2 handoff dependencies, not blockers for Member 3 Phase 0 completion
- [x] Confirmed Phase 0 repo state:
  - `Code/ServerApp/` exists with planned folders
  - runtime implementation has not started yet, which is acceptable for Phase 0
- [x] Confirmed next Member 3 action:
  - review `DOCS/API.md` in Phase 1 and list exact dashboard data/actions needed from Member 2 and Member 5

## 3. Phase 1 Immediate Start Tasks

These tasks must be done next:

- [ ] Review and accept `DOCS/API.md` with Member 2 and Member 5.
- [ ] Freeze packet order and state model.
- [ ] Freeze shared DTO list, enum list, and invalid packet behavior.
- [ ] Confirm auth/session response contract with Member 5.
- [ ] Confirm local multi-instance test assumption with Member 2 and Member 6.
- [ ] Prepare first networking/auth skeleton handoff.
- [x] Create `Code/NetManager.sln`, `ServerApp`, `ClientApp`, and `Shared` project files.
- [x] Confirm `ServerApp` and `ClientApp` reference `Shared`.
- [ ] Prove one TCP JSON-line packet round trip before deep UI work.

## 4. This Week Priority

### Member 1

- [x] Freeze MVP and out-of-scope list.
- [x] Freeze ownership map and write scope boundaries.
- [x] Freeze stack and identity assumptions.
- [x] Freeze first-week tasks and announce owners.
- [ ] Review `API.md` with Member 2 and Member 5.
- [ ] Keep `LEADER_FLOW.md`, `TASKS.md`, `BUGS.md`, `RUN_GUIDE.md`, `TEST_MATRIX.md`, and `DEMO_CHECKLIST.md` aligned.
- [ ] Approve Week 1 runnable skeleton before the team moves past Gate B.

### Member 2

- [ ] Finalize packet envelope and packet type baseline.
- [ ] Draft serializer/parser rules.
- [ ] Create or support `Shared` packet models needed for packet round trip.
- [ ] Create networking skeleton for server/client communication.
- [ ] Prove first connect/send/receive flow.
- [ ] Draft local multi-instance launch assumptions.

### Member 3

- [x] Create server login shell.
- [x] Create dashboard shell.
- [x] Create stub machine list and control area.
- [x] Bind server UI to service interfaces or placeholders only.
- [x] Prepare admin login view for `username + password + machineId`.
- [x] Move visible UI strings into a reusable resource-backed helper for `ServerApp/Forms`.

### Member 4

- [ ] Create client connect screen.
- [ ] Create client login shell.
- [ ] Create main client shell.
- [ ] Create lock screen shell.
- [ ] Prepare client login flow for bound account and machine.

### Member 5

- [ ] Draft `Users`, `Sessions`, and needed persistence fields.
- [ ] Draft auth result model.
- [ ] Create auth service skeleton.
- [ ] Define session state baseline.
- [ ] Draft validation rule for wrong `machineId`.

### Member 6

- [x] Create bug report template.
- [x] Create test matrix template.
- [x] Create first checklist for contract and connection tests.
- [ ] Review docs for contradictions.
- [x] Create separate checklists for real LAN and local multi-instance demo.
- [ ] Keep `RUN_GUIDE.md`, `TEST_MATRIX.md`, and `DEMO_CHECKLIST.md` updated as code appears.

## 5. Week-by-Week Delivery Targets

### Week 1

Target:

- scope, contract baseline, ownership, and runnable skeleton start are stable

Must be visible by end of week:

- `API.md` baseline
- source folder structure
- `NetManager.sln`
- `ServerApp`, `ClientApp`, and `Shared` project files
- `ServerApp` and `ClientApp` reference `Shared`
- chosen stack
- identity rule
- networking skeleton
- one JSON-line packet round trip
- auth skeleton
- server shell
- client shell
- `RUN_GUIDE.md`
- `TEST_MATRIX.md`
- `DEMO_CHECKLIST.md`

### Week 2

Target:

- network core and auth core start working together

Must be visible by end of week:

- TCP listener/connector
- send/receive loop
- repository/auth basics
- login flow baseline
- first real bugs recorded

### Week 3

Target:

- core end-to-end flow works

Must be visible by end of week:

- login -> status -> lock/unlock flow
- dashboard sees machine state
- client reacts to command
- ACK path visible

### Week 4

Target:

- core control flow is stable

Must be visible by end of week:

- lock/unlock
- ACK visibility
- machine-bound login behavior
- stable admin/client control flow

### Week 5

Target:

- all MVP features exist in real flow

Must be visible by end of week:

- notification
- timer
- chat
- multi-client baseline

### Week 6

Target:

- stabilization and environment validation

Must be visible by end of week:

- reconnect/disconnect handling
- timeout and invalid packet handling
- real LAN smoke test
- local multi-instance smoke test

### Week 7

Target:

- source cleanup, regression, and release candidate

Must be visible by end of week:

- release candidate
- bug severity list
- known limitation list
- cleaned architecture boundaries

### Week 8

Target:

- final demo readiness

Must be visible by end of week:

- final smoke test
- clean-machine setup pass
- demo checklist
- final docs aligned with build

## 6. Cross-Member Dependencies

- Member 3 waits on Member 2 for network-facing interface.
- Member 4 waits on Member 2 for client-facing network interface.
- Member 2 waits on Member 5 for auth interface details during login integration.
- Member 3 waits on Member 5 for admin login/auth result behavior.
- Member 4 waits on Member 2 and Member 5 for real client login behavior.
- Member 6 waits on all members for build output, feature status, and bug reproduction.
- All members wait on Member 1 for final stack, scope, and gate approval.

## 7. Conflict Watch List

These are the most likely overlap areas:

- `DOCS/API.md`: Member 2 edits, Member 1 approves, Member 5 reviews auth/session shape.
- `Shared/`: Member 2 owns packet/runtime shared transport models.
- login flow: Member 2 owns transport path, Member 5 owns auth logic, Member 3/4 own UI response.
- timer state: Member 2 owns transport, Member 5 owns persistence if stored, Member 3/4 own display only.
- account-to-`machineId` validation: Member 5 owns validation logic, Member 2 owns transport field path, Member 3/4 own display only.
- README/run guide: Member 6 updates, Member 1 reviews final top-level alignment.

## 8. Weekly Reporting Format

Each member should report in this shape:

- completed
- in progress
- blocked by
- next task
- docs that need update

## 9. Done This Turn

- [x] `LEADER_FLOW.md` restructured into full project execution flow.
- [x] member role docs aligned with the leader flow.
- [x] `TASKS.md` upgraded from short priority list to execution tracker.
- [x] Phase 0 branch and commit rules added.
- [x] Phase 0 source-of-truth docs confirmed.
- [x] Initial risk list added and linked to conflict control.
- [x] `ServerApp/Database` chosen consistently over `ServerApp/Data`.
- [x] Gate A marked passed.
- [x] Member 4 Phase 0 client GUI scope checklist confirmed by Member 4 self-review.

## 10. Next Review Checkpoint

Before moving beyond `Gate B`, Member 1 should verify:

- [ ] solution builds successfully.
- [ ] server and client can connect successfully.
- [ ] at least one packet can be exchanged.
- [ ] invalid packet does not crash the receiver.
- [ ] two local client instances can connect or the blocker is documented.
- [ ] receive loop does not freeze UI.
- [ ] local multi-instance test works.
- [ ] networking handoff does not conflict with auth ownership.
- [ ] no GUI member is parsing packet shape independently.
