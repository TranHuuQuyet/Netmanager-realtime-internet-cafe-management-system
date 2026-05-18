# LEADER FLOW

## 1. Purpose

This document is the master execution flow for the whole project.
It is written for Member 1, but it must be readable and actionable for all members.

This file exists to answer these questions clearly:

- what the team is building
- what order the team should follow
- who owns each part
- what each member should do each week
- what must be finished before the next step starts
- how to avoid overlap, conflict, and wasted effort
- how to finish the project on time with a stable demo

Project duration baseline:

- 8 weeks

## 2. Project Target

The target is a LAN-based Windows Forms client-server internet cafe management system for demo.

The MVP must support:

- TCP connection between server and clients
- admin login and client login
- realtime machine status
- lock and unlock control
- notification delivery
- timer updates
- basic chat

Out of scope for this project:

- file transfer
- voice call
- video streaming
- advanced reporting
- advanced analytics
- cloud deployment

## 3. Chosen Technical Stack

The recommended implementation stack for this project is:

- .NET 8
- C#
- Windows Forms
- TCP Socket
- SQLite
- `System.Text.Json`

Reason for this choice:

- fast enough for an 8-week academic or team delivery
- stable for LAN-based Windows desktop apps
- simple enough to keep source clean
- suitable for demo-first development without overengineering

## 4. Identity and Demo Rules

### Client identity rule

Each client machine must use its own account and its own `machineId`.

Examples:

- `pc01` <-> `PC-01`
- `pc02` <-> `PC-02`

Login rule:

- the server validates `username`, `password`, and `machineId`
- one client account is mapped to one machine
- a login with the correct account but the wrong `machineId` must be rejected

### Chat scope rule

Chat in this project is only:

- 1-1 text between client and admin
- no emoji
- no history
- no file or image
- no group room
- no resend queue after reconnect

### Supported demo modes

The project must support both of these:

- `Mode A - Real LAN Demo`: one server machine and multiple real client machines in the same LAN
- `Mode B - Local Multi-Instance Demo`: one machine running server and multiple client instances for development and fallback demo use

## 5. Core Working Rules

- Scope must be frozen before feature expansion.
- `API.md` is the shared contract source of truth.
- `LEADER_FLOW.md` is the source of truth for phase, scope, ownership, branch rules, commit rules, and release decisions.
- `TASKS.md` is the source of truth for current execution status and next actions.
- `BUGS.md` is the source of truth for risks, unresolved issues, and runtime bugs once code exists.
- Server and client must never invent different packet shapes.
- Shared packet or state changes must be documented on the same day.
- Each file or module must have one clear owner.
- Each task must have one main owner.
- UI, networking, and database work should be separated unless integration is required.
- No member should block another member because of hidden assumptions.
- Small stable progress is better than large unfinished progress.
- A stable demo is more important than extra features.
- Source cleanliness matters, but never through risky late refactors.

## 6. Team Ownership Map

### Member 1 - Team Leader / System Architect

Owns:

- scope control
- architecture direction
- task assignment
- integration order
- milestone review
- release approval
- control documents

Write scope:

- `DOCS/LEADER_FLOW.md`
- `DOCS/TASKS.md` if used
- `DOCS/BUGS.md`
- `README.md` for top-level project alignment

### Member 2 - Network Engineer

Owns:

- shared packet contract implementation
- TCP server/client communication
- serializer/parser
- dispatcher
- multi-client support
- disconnect/reconnect handling

Write scope:

- `Shared/`
- `ServerApp/Networking/`
- `ClientApp/Networking/`
- `DOCS/API.md`

### Member 3 - Server GUI Developer

Owns:

- server login form
- dashboard shell
- machine list
- machine status rendering
- control panel
- admin-side realtime display

Write scope:

- `ServerApp/`
- server-side UI folders and form files

### Member 4 - Client App Developer

Owns:

- connect form
- client login flow
- main client screen
- lock screen
- timer/notification/chat display
- client-side command reaction

Write scope:

- `ClientApp/`
- client-side UI folders and form files

### Member 5 - Database & Authentication

Owns:

- SQLite schema
- repository/data access layer
- auth service
- session tracking
- configuration persistence

Write scope:

- `ServerApp/Auth/`
- `ServerApp/Database/`
- database and persistence files

### Member 6 - Tester & Documentation

Owns:

- test matrix
- regression tracking
- bug reports
- run guide
- demo checklist
- documentation consistency review

Write scope:

- `DOCS/BUGS.md`
- `README.md`
- test notes and run-guide documents

## 7. Conflict Prevention Model

To keep the team moving without overlap, these rules are mandatory:

- Member 2 owns packet schema implementation and shared DTO changes.
- Member 5 owns database schema and auth logic changes.
- Member 3 owns server UI files.
- Member 4 owns client UI files.
- Member 1 approves scope changes, contract changes, and cross-module integration order.
- Member 6 reports bugs and documentation mismatches but does not rewrite runtime logic without assignment.

When two modules must touch each other:

- one member is assigned as lead owner
- one member is assigned as support owner
- the lead owner decides the exact handoff shape
- the support owner adapts their module to the agreed interface

Members must not:

- edit another member's runtime area casually
- merge undocumented packet changes
- hard-code fake behavior after real integration has started
- combine unrelated work in one commit

## 7.1 Branch and Commit Rules

Branch naming rule:

- main integration branch: `main`
- member work branch: `member-<number>/<short-task>`
- integration branch when needed: `integration/<flow-name>`
- hotfix branch after release candidate: `hotfix/<short-fix>`

Examples:

- `member-2/network-skeleton`
- `member-5/auth-schema`
- `integration/login-flow`
- `hotfix/login-machineid-error`

Commit naming rule:

- one commit must represent one logical job
- use format: `<scope>: <short action>`
- allowed scopes: `docs`, `shared`, `server`, `client`, `auth`, `db`, `test`, `build`, `fix`
- avoid mixing unrelated modules in one commit
- packet or contract changes must update `DOCS/API.md` in the same commit or same review batch

Examples:

- `docs: freeze phase 0 rules`
- `shared: add packet envelope model`
- `auth: add machine-bound login validation`
- `fix: handle invalid packet response`

## 7.2 Initial Risk List

Current Phase 0 risks:

- packet contract drift between server and client
- unclear login ownership between networking and auth
- GUI members depending on unfinished real services
- wrong or inconsistent `machineId` mapping during demo
- real LAN setup not matching local multi-instance assumptions
- late feature expansion beyond MVP, especially chat and reporting
- UI thread blocking during network receive events
- database schema changes after GUI and network code already depend on auth output

Risk control decisions:

- `DOCS/API.md` must be reviewed before packet implementation goes deep.
- Member 2 owns transport and shared packet implementation.
- Member 5 owns auth, session, SQLite schema, and account-to-`machineId` validation.
- Member 3 and Member 4 consume service/interface outputs instead of inventing packet parsing in UI.
- Chat remains 1-1 text only.
- Both real LAN and local multi-instance must stay documented and testable.

## 8. Shared Delivery Order

This is the only safe implementation order for the project:

1. Scope freeze
2. Contract freeze
3. Solution and project scaffold
4. Shared packet baseline
5. Networking foundation
6. Authentication and persistence
7. GUI skeletons
8. End-to-end integration
9. Feature completion
10. Stabilization
11. Release candidate
12. Final demo and archive

## 9. Phase-Based Execution Flow

### Phase 0 - Project Kickoff and Scope Freeze

Purpose:

- align the team before coding starts
- freeze the MVP boundary
- remove unclear assumptions

Member responsibilities:

- Member 1: finalize MVP, ownership, branch rules, commit rules, and decision rules
- Member 2: confirm networking feasibility and packet constraints
- Member 3: confirm server GUI scope
- Member 4: confirm client GUI scope
- Member 5: confirm auth/database scope
- Member 6: prepare bug format, test matrix format, and demo-note structure

Required outputs:

- frozen MVP scope
- out-of-scope list
- folder ownership map
- branch naming rule
- commit naming rule
- initial risk list
- first project structure agreement
- chosen technical stack
- client identity rule
- chat scope rule
- supported demo modes

Completion target:

- every member knows exactly what they own
- every planned feature is marked as MVP or out of scope
- the team can start Week 1 without role confusion

Conflict control:

- only Member 1 can approve scope changes
- no runtime code should be started before write ownership is confirmed

### Phase 1 - Architecture and Contract Freeze

Purpose:

- define the shared system shape before deep implementation starts

Member responsibilities:

- Member 1: approve architecture direction and contract freeze
- Member 2: define packet envelope, packet types, serializer/parser rules, and DTO contract
- Member 5: define auth result shape, session model, and database assumptions
- Member 3: review server GUI needs against the contract
- Member 4: review client GUI needs against the contract
- Member 6: verify docs are consistent and test cases can be derived from the contract

Required outputs:

- stable `API.md`
- packet priority order
- shared DTO list
- state enum list
- command enum list
- JSON framing rule
- invalid packet behavior rule
- auth and session response contract
- account-to-`machineId` mapping rule
- local multi-instance test assumption

Completion target:

- server, client, and auth teams can implement without guessing message format
- no critical contract question remains unresolved

Conflict control:

- only Member 2 edits packet contract implementation
- only Member 5 edits database/auth contract details
- GUI members can consume the contract but must not redefine it

### Phase 2 - Networking Foundation

Purpose:

- make transport stable before feature expansion

Member responsibilities:

- Member 2: implement TCP listener, connector, send/receive loop, dispatcher, connection state, and basic logs
- Member 1: review alignment with `API.md`
- Member 5: provide auth service interface needed for login call path
- Member 3: consume network interfaces only through agreed service shape
- Member 4: consume network interfaces only through agreed service shape
- Member 6: prepare connection, disconnect, invalid packet, and multi-client tests

Required outputs:

- TCP server
- TCP client
- shared serializer/parser
- dispatcher skeleton
- connect and disconnect handling
- session object per client
- 2 to 3 client connection test note
- local multi-instance test baseline

Completion target:

- clients can connect and exchange packets without freezing UI
- disconnect does not break the whole process
- both real-LAN assumptions and local multi-instance assumptions are valid

Conflict control:

- Member 2 is sole owner of socket-layer runtime logic
- Members 3 and 4 must not duplicate packet parsing in forms

### Phase 3 - Authentication and Persistence

Purpose:

- make login and stored session state reliable

Member responsibilities:

- Member 5: implement SQLite schema, repositories, auth logic, session logic, seed data, and persistence behavior
- Member 2: connect login packet flow to auth service
- Member 3: integrate admin login UI to real auth result
- Member 4: integrate client login UI to real login result
- Member 1: review lifecycle rules and unresolved dependencies
- Member 6: test valid login, invalid login, inactive user, and DB failure behavior

Required outputs:

- `Users` schema
- `Sessions` schema
- auth service
- session service
- login response mapping
- configuration persistence
- seed users for demo
- account-to-machine mapping validation

Completion target:

- admin and client logins work through the real auth path
- failed login is deterministic and visible
- wrong `machineId` login is rejected clearly

Conflict control:

- Member 5 owns schema and repository changes
- Member 2 may call auth services but must not rewrite DB logic

### Phase 4 - GUI Skeletons and Local Stub Flow

Purpose:

- let both UI members progress safely before all realtime behavior is finished

Member responsibilities:

- Member 3: build server login form, dashboard shell, machine list, control panel, and status view
- Member 4: build client connect form, login view, main screen, lock screen, timer/notification/chat areas
- Member 2: provide network-facing interfaces or stubs
- Member 5: provide auth-facing interfaces or stubs
- Member 1: keep GUI scope inside MVP
- Member 6: verify the screens cover the final demo flow

Required outputs:

- server dashboard shell
- client app shell
- stubbed machine list
- stubbed login screens
- lock screen shell
- notification/timer/chat placeholders
- UI update strategy for background events
- local multi-instance launch assumptions for testing

Completion target:

- both applications can run and be demo-clicked before full integration
- no GUI screen depends on hard-coded packet parsing

Conflict control:

- Member 3 owns `ServerApp` UI files
- Member 4 owns `ClientApp` UI files
- Members 2 and 5 expose interfaces rather than editing UI directly

### Phase 5 - End-to-End Integration

Purpose:

- connect real networking, auth, and UI behavior in safe order

Member responsibilities:

- Member 1: define integration order and assign lead owner for each integration bug
- Member 2: connect packet routing to service and UI-facing events
- Member 3: bind server UI to real machine and command state
- Member 4: bind client UI to real server command and status state
- Member 5: support auth/session flows during integration
- Member 6: record real bugs and regression steps

Required outputs:

- login-to-dashboard flow
- login-to-client-main-screen flow
- status sync on dashboard
- lock/unlock command flow
- ACK visibility
- integration bug list
- machine-bound login flow

Completion target:

- the core demo flow works from login to machine control for at least one client

Conflict control:

- integration is merged in small steps
- each cross-module bug has one lead owner
- contract changes are frozen unless Member 1 approves an exception

### Phase 6 - Realtime Feature Completion

Purpose:

- complete all MVP behavior after the main flow is alive

Member responsibilities:

- Member 2: finish routing for notification, timer, chat, and ACK handling
- Member 3: finish server controls and displays for all live features
- Member 4: finish client displays and reactions for all live features
- Member 5: finish persistence rules for session or timer data if needed
- Member 6: verify each feature with reproducible steps
- Member 1: accept or reject feature readiness

Required outputs:

- notification delivery
- timer updates
- chat flow
- ACK processing
- status sync completion
- direct and broadcast handling
- final 1-1 text chat behavior only

Completion target:

- every MVP packet type works in the real application
- 2 to 3 clients can participate in the demo flow
- chat remains within the agreed minimal scope

Conflict control:

- Member 2 owns transport logic
- Members 3 and 4 own presentation behavior
- timer ownership must be agreed before shared changes are made

### Phase 7 - Stabilization and Hardening

Purpose:

- remove demo-breaking instability

Member responsibilities:

- Member 1: prioritize fixes and decide accepted risks
- Member 2: harden disconnect, reconnect, timeout, malformed packet handling
- Member 3: harden server-side UI error handling
- Member 4: harden client-side UI error handling
- Member 5: harden database/auth failure behavior and data consistency
- Member 6: run regression tests and maintain `BUGS.md`

Required outputs:

- disconnect handling
- reconnect handling
- timeout handling
- malformed packet handling
- duplicate login handling
- better logs
- user-visible error states
- local multi-instance fallback test
- real LAN smoke test

Completion target:

- no high-severity issue blocks the core demo flow
- server and client survive realistic failure scenarios

Conflict control:

- no new features during this phase
- shared fixes require review from affected owners

### Phase 8 - Testing and Release Candidate

Purpose:

- freeze the build and prove it is demo-ready

Member responsibilities:

- Member 1: approve release candidate and enforce code freeze
- Member 6: produce final test report, run guide check, demo checklist, and known limitations
- Member 2: fix release-blocking network issues only
- Member 3: fix release-blocking server UI issues only
- Member 4: fix release-blocking client UI issues only
- Member 5: fix release-blocking database/auth issues only

Required outputs:

- release candidate build
- final `README.md`
- final run guide
- demo checklist
- known limitations list
- final test report
- local multi-instance test guide
- real LAN test guide

Completion target:

- the team can set up and run the demo on a clean Windows machine
- docs match the real build
- both demo modes can be explained and run if needed

Conflict control:

- code freeze is active
- all changes require Member 1 approval

### Phase 9 - Final Demo and Post-Release Review

Purpose:

- deliver the project clearly and preserve lessons learned

Member responsibilities:

- Member 1: lead the demo and handle fallback decisions
- Member 2: support network setup and troubleshooting
- Member 3: demonstrate server dashboard usage
- Member 4: demonstrate client behavior
- Member 5: explain auth, DB, and session behavior
- Member 6: control demo checklist, final docs, and post-release notes

Required outputs:

- final demo build
- final documentation package
- bug and limitation summary
- post-release notes
- primary demo mode and fallback demo mode notes

Completion target:

- the demo can be presented without improvising technical rescue
- the repository is clear enough for review and archive

Conflict control:

- no code changes during final demo unless Member 1 opens an emergency fix window

## 10. Week-by-Week Production Plan

### Week 1 - Kickoff, Contract, Scaffold, and First Round Trip

Main weekly goal:

- freeze scope
- freeze contract baseline
- freeze technical direction
- create the real solution/project scaffold
- prove the first TCP JSON-line packet round trip

Member 1:

- finalize MVP and out-of-scope list
- finalize ownership map
- finalize branch/commit rules
- publish first `API.md` baseline with Member 2
- publish first weekly task map
- freeze stack choice: `.NET 8`, C#, WinForms, TCP, SQLite, `System.Text.Json`
- freeze client identity rule and supported demo modes
- approve `RUN_GUIDE.md`, `TEST_MATRIX.md`, and `DEMO_CHECKLIST.md` as Week 1 operating docs
- verify that Week 1 ends with a runnable skeleton, not only planning docs

Member 2:

- define packet envelope and packet types
- create or support `Shared` packet model baseline
- create networking skeleton
- create serializer/parser draft
- create first connect/send/receive proof
- document local multi-instance connection assumptions

Member 3:

- create server login form skeleton
- create dashboard shell
- create machine list stub view

Member 4:

- create client connect screen
- create client login stub
- create main client shell
- create lock screen shell

Member 5:

- define SQLite schema draft
- define auth result model
- define session model
- create auth service skeleton
- define account-to-`machineId` validation rule

Member 6:

- create bug report template
- create test matrix template
- create first checklist for contract and connection tests
- create separate checklist for `Mode A` and `Mode B`
- create run guide and demo checklist baseline

Week 1 completion target:

- no one is waiting on basic role clarity
- contract and ownership are stable enough to continue
- stack and identity assumptions are frozen
- `Code/NetManager.sln` exists
- `ServerApp`, `ClientApp`, and `Shared` project files exist
- `ServerApp` and `ClientApp` reference `Shared`
- solution builds
- one TCP JSON-line packet can round-trip between server and client
- UI shells can open without blocking on network work
- `RUN_GUIDE.md`, `TEST_MATRIX.md`, and `DEMO_CHECKLIST.md` exist

### Week 2 - Network Core, Auth Core, and App Skeleton Completion

Main weekly goal:

- get real connection and login path moving

Member 1:

- resolve blockers between Member 2 and Member 5
- confirm no contract drift
- assign integration order for the coming week

Member 2:

- finish TCP listener and client connector
- finish send/receive loop
- add dispatcher skeleton
- expose login/status handlers

Member 3:

- connect dashboard to service interfaces
- prepare status rendering against stubbed or real models

Member 4:

- connect client screens to service interfaces
- prepare receive-state rendering

Member 5:

- finish SQLite schema
- finish repository layer
- finish auth validation
- connect session tracking basics

Member 6:

- run connection test notes
- run login success/failure tests
- record first real bugs
- validate local multi-instance testing path

Week 2 completion target:

- login works through real network and auth path at a basic level

### Week 3 - Login, Status, and Core Flow Integration

Main weekly goal:

- make the core demo path work end-to-end

Member 1:

- control integration order
- assign one lead owner per blocker
- stop unnecessary new work

Member 2:

- connect status routing
- connect lock/unlock routing
- expose ACK results

Member 3:

- bind dashboard to real machine state
- show command results on admin side

Member 4:

- bind client state to real commands
- show lock/unlock state correctly

Member 5:

- support auth/session fixes during integration
- stabilize login/session state behavior

Member 6:

- test login to status flow
- test lock/unlock flow
- update `BUGS.md` with exact repro steps

Week 3 completion target:

- one admin and at least one client complete the full core flow

### Week 4 - Lock, Unlock, ACK, and Control Flow Completion

Main weekly goal:

- make machine control stable before secondary features

Member 1:

- keep the team on control flow priority
- reject chat or polish detours before control flow is stable

Member 2:

- finish lock/unlock routing
- finish ACK routing and error behavior

Member 3:

- finish admin control behavior and command-result display

Member 4:

- finish lock screen and unlock reaction behavior

Member 5:

- support machine-bound login and active session validation

Member 6:

- test lock/unlock and ACK behavior in both demo modes

Week 4 completion target:

- login, status, lock/unlock, and ACK work stably

### Week 5 - Timer, Notification, and Minimal Chat Completion

Main weekly goal:

- finish timer, notification, chat, and remaining realtime behavior

Member 1:

- keep priorities narrow
- reject feature growth outside MVP

Member 2:

- finish notification, timer, chat, and broadcast/direct packet routing

Member 3:

- finish admin controls and displays for those features

Member 4:

- finish client displays and reactions for those features

Member 5:

- finish persistence behavior needed for timer/session state

Member 6:

- test each feature separately
- test 2 to 3 clients if possible
- confirm chat remains 1-1 text only

Week 5 completion target:

- all MVP features exist in real flow, even if not fully polished

### Week 6 - Multi-Client, Stability, and Environment Validation

Main weekly goal:

- remove demo blockers and increase reliability
- prove the app works in both required deployment modes

Member 1:

- freeze feature work
- rank bugs by severity
- approve only stabilization work

Member 2:

- fix disconnect, reconnect, timeout, malformed packet issues
- verify real-LAN assumptions and local multi-instance assumptions

Member 3:

- fix server UI stability and thread-safety issues

Member 4:

- fix client UI stability and command-state issues

Member 5:

- fix DB/auth edge cases and consistency issues
- verify account-to-machine mapping behavior

Member 6:

- run regression checklist
- verify fixed bugs
- maintain known limitation list
- run both demo-mode smoke tests

Week 6 completion target:

- no high-severity demo blocker remains open without explicit acceptance
- both demo modes are usable for development or presentation

### Week 7 - Source Cleanup, Regression, and Release Candidate

Main weekly goal:

- lock the final build and prepare the team to present it
- clean up source boundaries without risky redesign

Member 1:

- approve release candidate
- run final leadership review
- lock all non-essential changes
- confirm architecture explanation is simple and consistent

Member 2:

- support final setup and network troubleshooting
- clean network code boundaries if needed without changing behavior

Member 3:

- rehearse server dashboard demo path
- clean UI/service boundaries if needed without changing flow

Member 4:

- rehearse client demo path
- clean UI/service boundaries if needed without changing flow

Member 5:

- verify demo accounts, auth, and session behavior
- clean auth/data boundaries if needed without changing behavior

Member 6:

- finish demo checklist
- finish run guide verification
- finish post-release summary
- finish final regression report draft

Week 7 completion target:

- source is clean enough to present
- release candidate is stable enough for final rehearsal

### Week 8 - Final Rehearsal, Release Lock, and Demo

Main weekly goal:

- freeze the final result and present it safely

Member 1:

- lock release scope completely
- approve final demo order and fallback mode

Member 2:

- support final environment and network checks

Member 3:

- rehearse admin dashboard demo path one last time

Member 4:

- rehearse client demo path one last time

Member 5:

- verify final account, machine, and session data

Member 6:

- run final smoke test
- run final checklist
- hold demo notes and fallback steps

Week 8 completion target:

- the build, docs, source structure, and demo flow are all presentation-ready

## 11. Detailed Week 1 Working Plan

### Day 1

- Member 1: freeze MVP, ownership, and team rules
- Member 2: draft packet contract and network skeleton
- Member 5: draft DB schema and auth/session shape
- Member 3: start server shell with stubbed navigation
- Member 4: start client shell with stubbed screens
- Member 6: create test and bug tracking templates
- All: confirm `.NET 8 + WinForms + TCP + SQLite + System.Text.Json`

### Day 2

- Member 1: review open assumptions and close blockers
- Member 2: implement listener, connector, and basic packet send
- Member 5: implement schema and repository skeleton
- Member 3: finish dashboard shell and machine list stub
- Member 4: finish login shell and lock screen stub
- Member 6: review docs consistency and test readiness
- All: confirm account-to-`machineId` mapping rule

### Day 3

- Member 1: confirm first contract freeze
- Member 2: implement receive loop and parser draft
- Member 5: implement auth service draft
- Member 3: bind UI to service interfaces or stubs
- Member 4: bind UI to service interfaces or stubs
- Member 6: record first issues and missing definitions
- All: confirm chat minimal scope rule

### Day 4

- Member 1: assign Week 2 integration dependency order
- Member 2: add dispatcher skeleton and connection state handling
- Member 5: connect auth result to service boundary
- Member 3: prepare status view
- Member 4: prepare client receive-state view
- Member 6: run first connection test checklist
- All: confirm local multi-instance test assumptions

### Day 5

- Member 1: verify all members can start Week 2 without ambiguity
- Member 2: complete connect/send/receive baseline
- Member 5: complete schema and auth baseline
- Member 3: complete admin shell baseline
- Member 4: complete client shell baseline
- Member 6: publish first bug and risk summary
- All: confirm `Mode A` and `Mode B` are both documented

## 12. Handoff Rules

When a module is ready to hand off:

- interface shape is stable enough to consume
- input and output are documented
- known limitations are written
- at least one sample flow exists

The receiving member must get:

- owned file paths
- current assumptions
- sample packet or data shape
- open blockers
- expected next action

The previous owner must not:

- keep changing the interface silently
- edit the consumer's area casually
- merge changes that invalidate the handoff without notice

## 13. Integration Gates

### Gate A - Scope and Contract Gate

Pass conditions:

- MVP is frozen
- ownership is frozen
- packet schema is agreed
- state model is agreed
- tech stack is frozen
- account-to-`machineId` rule is frozen

### Gate B - Connection Gate

Pass conditions:

- solution builds successfully
- server and client connect successfully
- at least one valid JSON-line packet is exchanged
- invalid packet input fails gracefully
- two local client instances can connect, or a documented blocker exists
- UI is not blocked
- local multi-instance test works
- no GUI form owns packet parsing that belongs in networking or shared services

### Gate C - Auth Gate

Pass conditions:

- login works
- auth result is deterministic
- session state is connected
- wrong `machineId` is rejected correctly

### Gate D - Core Control Gate

Pass conditions:

- status sync works
- lock/unlock works
- ACK is visible

### Gate E - Full Feature Gate

Pass conditions:

- notification works
- timer works
- chat works
- multi-client test basically works
- chat stays within minimal scope

### Gate F - Release Gate

Pass conditions:

- disconnect test passes
- clean-machine setup test passes
- docs match the build
- no known high-severity demo blocker remains
- real LAN smoke test passes
- local multi-instance fallback passes
- `RUN_GUIDE.md` can be followed on a clean Windows machine
- `DEMO_CHECKLIST.md` core path passes
- `TEST_MATRIX.md` shows no unaccepted high-severity failure

## 14. Leader Operating Routine

Every day, Member 1 must:

1. Check what each member finished yesterday.
2. Check who is blocked and by what dependency.
3. Confirm whether packet or state rules changed.
4. Confirm whether any dependency or machine/account assumption changed.
5. Update tasks or decisions in docs.
6. Reassign priority if one module is blocking others.

Every week, Member 1 must:

1. Check whether the current gate is passed.
2. Check whether the next week can start without hidden blockers.
3. Compare docs against real implementation status.
4. Review bug severity and risk level.
5. Check whether both demo modes are still supported.
6. Confirm the weekly runnable build exists.
7. Announce the next week's priorities clearly.

Before release, Member 1 must:

1. Freeze feature work.
2. Review the demo checklist with Member 6.
3. Confirm the team can explain their module clearly.
4. Confirm the build runs from the documented steps.
5. Confirm the fallback demo mode still works.
6. Approve only release-blocking fixes.

## 15. Definition of Done

The project is done only when:

- server and client connect reliably
- login works through the chosen auth flow
- dashboard shows realtime machine state
- client receives lock/unlock, notification, timer, and chat correctly
- multi-client demo works on Windows
- each client account is correctly bound to its own `machineId`
- disconnect and reconnect do not crash the app
- invalid packets do not crash the app
- bugs are documented honestly
- docs and run guide match the actual build
- both real LAN and local multi-instance demo modes are usable
- the team can present the architecture and flow clearly
