# TASKS - RECOVERY TRACKER

Active tracker reset date: `2026-05-25`
Core delivery deadline: `2026-07-05`
Baseline report: `DOCS/RECOVERY_REPORT_2026-05-25.md`

Roadmap 8 tuan cu da bi supersede boi recovery roadmap vi build va runtime gate chua dat. Lich su checkbox cu va evidence cua tung member duoc luu trong recovery report; khong duoc dung checkbox cu de claim runtime progress.

## Tracking Rules

- Trong cac bang task `R1-R6`, `[x]` nghia la owner da nop phan viec/evidence duoc yeu cau; `[ ]` nghia la chua nop, dang bi block hoac chua mo.
- Checkbox la checklist nop viec cua member, khong phai ket qua runtime verification hay gate approval.
- Runtime/release pass duoc xac nhan trong `DOCS/TEST_MATRIX.md` va `DOCS/DEMO_CHECKLIST.md`; M6 verify evidence va M1 approve gate khi can.
- Moi feature/fix cua member duoc tao tu `develop` va PR/merge vao `develop` de integration va test; khong merge truc tiep vao `main`.
- Chi candidate tren `develop` co `Pass` do M6 ghi nhan va duoc M1 approve moi duoc promote/merge vao `main`.

Cac status chu sau van dung cho baseline, evidence submission va retained extension tracking:

| Status | Meaning |
| --- | --- |
| `Verified Pass` | Evidence runtime/test da duoc M6 xac nhan va M1 chap nhan neu la gate |
| `Evidence Submitted` | Owner da nop evidence, cho verify |
| `In Progress` | Dang lam, chua du evidence |
| `Blocked` | Bi chan boi dependency; evidence ghi ro blocker/owner |
| `Not Started` | Chua den thu tu thuc hien |
| `Conditional` | Extension duoc giu lai, chi mo khi core gate pass |
| `Promoted to Required Demo Scope` | Feature tung la extension nhung da duoc M1 dua vao core demo tu `2026-06-09` tro di |
| `Retained - Continue After Core Release` | Van thuoc roadmap nhung tiep tuc sau `2026-07-05` |
| `Historical Artifact` | Artifact cu ton tai, khong phai delivery proof |

## Baseline Audit Status

| Item | Baseline audit status | Evidence / blocker |
| --- | --- | --- |
| Full solution build | `Fail` | Build audit fail tai `ServerApp/Forms/LoginForm.Designer.cs(7,29)` do thieu `LoginForm.cs` trong working tree |
| Contract approval | `Pass` | API `v0.2` contract va canonical auth/database baseline da duoc freeze va verified |
| Runtime tests | `Blocked` | `0/33` legacy test cases da pass tai baseline audit |
| Demo path | `Blocked` | `14/14` legacy demo steps van pending tai baseline audit |
| M4 client forms | `Historical Artifact` | Forms local/uncommitted ton tai, chua consume runtime service |

## Current Evidence Submissions

| Item | Submitted evidence | Current disposition |
| --- | --- | --- |
| Full solution build candidate | `dotnet build Code/NetManager.sln --artifacts-path .audit-artifacts --no-restore -v:minimal` passes with `0` warnings and `0` errors after restoring the server login path, adding an explicit client shell entry form and annotating the WinForms server app as Windows-only | `Verified Pass`; `G0-01` recorded in `DOCS/TEST_MATRIX.md` |
| Server login path | `Program` initializes typed auth off the UI thread and passes `IAuthService` into `LoginForm`; the resource manifest lookup is corrected and a startup smoke opens the `Dang nhap` dialog responsively | `Verified Pass` for R1 startup/build path; UI-driven admin login remains covered by later `G2` verification |
| M3 server dashboard R1 shell honesty - `2026-06-01`, working tree | `MainForm` labels the machine dashboard as sample data, uses explicit sample loaders, disables `LOCK`/`UNLOCK` actions as backend-pending, and exposes `ApplyMachineStatusUpdate(machineId, status)` as a UI-side bridge for R2 typed status events without parsing packets, querying SQLite or fabricating ACK/error results | `Verified Pass` for R1 shell honesty/build path only; dashboard real status remains `R2-U02` and `G2-05/G2-06` stay blocked until M2/M4 status routing exists |
| Client startup path | `ConnectForm` is present as a buildable shell and explicitly states that network login binding remains pending `R2/G2` | `Verified Pass` for R1 startup shell only; no runtime integration claim |
| `R1-U01` client UI shell - `2026-05-26`, commit `6583b48` | On branch `quyet-clientapp-member4`, `dotnet build Code/NetManager.sln --artifacts-path .audit-artifacts --no-restore -v:minimal` passes with `0` warnings and `0` errors; UI smoke opens responsive `ConnectForm`, `ClientMainForm` preview and `LockScreenForm` preview; lock preview displays that real `LOCK/UNLOCK` waits for routing; boundary search finds no JSON/network service references in client forms | `Verified Pass` for R1 UI shell/startup only; network login, `LOCK`/`UNLOCK` and `ACK` runtime remain pending later gates |
| `R1-U01` customer-flow shell correction - `2026-05-26`, working tree | `dotnet build Code/NetManager.sln --artifacts-path .audit-artifacts -v:minimal` passes with `0` warnings and `0` errors; a temporary .NET 8 smoke verifies username/password-only login, read-only configured machine identity, hidden endpoint, no local lock action, `--machine-id PC-02` launch configuration, rejected invalid launch configuration, honest pending-login status and passive lock surface with `UnlockFromServer()` release hook | `Verified Pass` for corrected client shell ownership/UX only; TCP UI login and server-routed `LOCK`/`UNLOCK`/`ACK` remain pending runtime gates |
| `R1-U01` plain WinForms client refinement - `2026-05-26`, working tree | `dotnet build Code/NetManager.sln --artifacts-path .audit-artifacts -v:minimal` passes with `0` warnings and `0` errors; .NET 8 smoke verifies compact `424 x 318` login dialog matching server-style controls, read-only `PC-01`/`PC-02` machine identity, default buttons only, themed UI removal across client forms, and passive lock release through `UnlockFromServer()` | `Verified Pass` for presentation/client startup only; login/status/control routing and ACK remain pending their runtime gates |
| `R1-A01` auth handoff + canonical DB path - `2026-05-26`, working tree | `AuthBootstrapper` resolves `internet_cafe.db` from repository root, seeds canonical `admin` / `client01` / `client02` accounts, keeps `AuthUsers` and `AuthSessions` as the runtime tables, and `AuthStatusExtensions` maps auth statuses to API codes | `Verified Pass`; canonical seed/admin rule da khop runtime DB va `G0-05` da dong |
| `B-008/G2-05` client-sent STATUS closure - `2026-06-15`, `testing_branch` commit `222c68c` | `ClientMainForm.SendResumeStatusAsync()` sends authenticated `STATUS`; `PacketDispatcher.DispatchStatusAsync()` accepts it and returns typed `ACK`; `TcpJsonLineServer.StatusEmitted` remains wired to `MainForm.ApplyMachineStatusUpdate`; `dotnet build Code\NetManager.sln --no-restore -v:minimal`, `NetworkSmokeTest`, `Auth_Test` and `ContractSmoke` pass | `Verified Pass` for `B-008/G2-05` local audit; closes the status blocker and unblocks `R3-B01/G3-00` only, while full `G3` control demo remains tracked separately |

`G0` contract/auth/build baseline, `G1` network foundation and the `B-008/G2-05` client-sent status gap are verified pass in the recovery log. ClientApp control remains tracked under `R3/G3` and later gates.

## R1 - Foundation Repair (`2026-05-25` to `2026-05-31`)

`R1-L01`
Owner: `M1`
Task: Approve recovery scope, deadline, core/extension lanes va merge gates
Dependency: Recoveary report
Required evidence: Decision entry + team notice + `develop` -> tester `Pass` -> `main` promotion rule
Member done: [x]

`R1-C01`
Owner: `M3 + M5`
Task: Restore buildable server login path va full solution build
Dependency: Current broken form state
Required evidence: `dotnet build Code/NetManager.sln` pass
Member done: [x]

`R1-C02`
Owner: `M2 + M1`
Task: Freeze API `v0.2`; align string packet type, LOGIN response va error envelope
Dependency: Contract review
Required evidence: API approval + serialization tests
Member done: [X]

`R1-A01`
Owner: `M5 + M1`
Task: Declare SQLite auth path, seed/admin rule va runtime schema canonical
Dependency: `R1-L01`
Required evidence: Decision + auth handoff note
Member done: [x]

`R1-N01`
Owner: `M2`
Task: Implement server listener, typed dispatcher baseline va local JSON-line round-trip; no ClientApp UI login integration claim
Dependency: `R1-C01`, `R1-C02`
Required evidence: Trace valid request/response
Member done: [X]

`R1-N02`
Owner: `M2 + M6`
Task: Validate invalid/unsupported packet does not crash receiver
Dependency: `R1-N01`
Required evidence: `G1` test result
Member done: [x]

`R1-U01`
Owner: `M4`
Task: Integrate client form artifacts into buildable branch without claiming runtime integration; covers ClientApp shell/startup smoke only
Dependency: `R1-C01`
Required evidence: Build/UI smoke note
Member done: [x]

`R1-Q01`
Owner: `M6`
Task: Record initial fail/blocked statuses and high-severity blockers
Dependency: Audit evidence
Required evidence: Updated tests/bugs docs
Member done: [x]

## R2 - Authenticated Status (`2026-06-01` to `2026-06-07`)

`R2-N01`
Owner: `M2 + M5`
Task: Route real `LOGIN` from TCP dispatcher to canonical auth service
Dependency: `G0`, `G1` pass
Required evidence: Request/response trace
Member done: [X]



`R2-A01`
Owner: `M5 + M6`
Task: Verify admin/client valid login, bad password va wrong `machineId`
Dependency: `R2-N01`
Required evidence: `G2` auth cases pass
Member done: [X]

`R2-U01`
Owner: `M4`
Task: Bind client login screen to real M2/M5 network/auth result
Dependency: `R2-N01`
Required evidence: Visible success/error result
Member done: [X]

`R2-N02`
Owner: `M2 + M4`
Task: Emit `STATUS` after authenticated client login va disconnect through the runtime service boundary
Dependency: `R2-A01`
Required evidence: Status packet trace
Member done: [X]

`R2-U02`
Owner: `M3`
Task: Render real one-client online/offline state
Dependency: `R2-N02`
Required evidence: Dashboard evidence + M6 pass
Member done: [ ]

`R2-L01`
Owner: `M1 + M6`
Task: Review `G2` va block control work neu login/status fail
Dependency: All R2 core tasks
Required evidence: Gate review note
Member done: [ ]

## R3 - Core Control (`2026-06-08` to `2026-06-14`)

Scope update `2026-06-09`: `G2-05/B-008` phai duoc dong hoac co M1 exception ro truoc khi claim R3+ readiness. Lock/unlock/ACK van la nen bat buoc cho chat, billing expiry va LAN demo sau nay.

`R3-B01`
Owner: `M2 + M4 + M6`
Task: Close carry-forward `G2-05/B-008` client-sent `STATUS` gap or record an approved equivalent status policy before claiming control/chat/billing readiness
Dependency: `G2` partial audit
Required evidence: Updated trace/test result and bug disposition
Member done: [x]

`R3-N01`
Owner: `M2 + M3`
Task: Route real `LOCK`/`UNLOCK` commands from selected client action
Dependency: `G2` pass; `R3-B01`
Required evidence: Command packet trace
Member done: [ ]

`R3-U01`
Owner: `M4`
Task: Apply lock/unlock client state through runtime command handler
Dependency: `R3-N01`
Required evidence: Visible client reaction
Member done: [ ]

`R3-N02`
Owner: `M2 + M4`
Task: Send typed `ACK` and deterministic command error
Dependency: `R3-U01`
Required evidence: ACK/error trace
Member done: [ ]

`R3-U02`
Owner: `M3`
Task: Show ACK/error result in admin UI
Dependency: `R3-N02`
Required evidence: Dashboard result evidence
Member done: [ ]

`R3-A01`
Owner: `M5`
Task: Enforce active session/machine guard for command target
Dependency: `G2` pass; `R3-B01`
Required evidence: Auth/session test result
Member done: [x]

`R3-Q01`
Owner: `M6 + M1`
Task: Run repeat one-client core demo and approve `G3`
Dependency: All R3 tasks; no unaccepted `G2-05/B-008` blocker
Required evidence: `G3` pass/bug list
Member done: [ ]

## R4 - Multi-Client, LAN Readiness And Required Chat/Billing Setup (`2026-06-15` to `2026-06-21`)

`R4-N01`
Owner: `M2 + M5`
Task: Route two authenticated local clients distinctly, keep the route LAN-capable, and decide duplicate-login behavior
Dependency: `G3` pass
Required evidence: Routing/session test
Member done: [ ]

`R4-U01`
Owner: `M3 + M4`
Task: Render/maintain distinct local client instances and prepare Admin/Client UI for two physical LAN clients
Dependency: `R4-N01`
Required evidence: Two-client UI evidence
Member done: [ ]

`R4-N02`
Owner: `M2 + M6`
Task: Verify disconnect does not crash server
Dependency: `R4-N01`
Required evidence: `G4` disconnect case
Member done: [ ]

`R4-C01`
Owner: `M2 + M3 + M4`
Task: Implement required 1-1 `CHAT` routing between Admin and the selected client; wrong/offline target must show controlled error
Dependency: `G3` pass; `R4-N01`
Required evidence: Selected-client chat trace and UI evidence
Member done: [ ]

`R4-B01`
Owner: `M3 + M5 + M1`
Task: Define required billing/session interface target: Admin selects timed/free/extend per machine, Client cannot select rental mode, default rate is `10000` VND/hour
Dependency: `2026-06-09` scope decision
Required evidence: Docs/API handoff note for `BillingSessions` and Admin UI ownership
Member done: [ ]

`R4-R01`
Owner: `M2 + M4 + M5`
Task: Define minimal reconnect/resync behavior required after ServerApp restart for billing/timer sync and extend/LOCK delivery
Dependency: `R4-N01`; `R4-B01`
Required evidence: Resync contract note and owner handoff
Member done: [ ]

`R4-Q01`
Owner: `M6 + M1`
Task: Approve `G4`, confirm promoted required scope remains on the demo path, and keep only unpromoted features as retained extensions
Dependency: Core R4 tasks including required chat/billing setup
Required evidence: Gate/required-scope decision
Member done: [ ]

## R5 - Stabilization And Required Demo Additions (`2026-06-22` to `2026-06-28`)

`R5-Q01`
Owner: `M6 + all owners`
Task: Run core regression, bug triage and clean setup verification for local, chat, billing and LAN-ready paths
Dependency: `G4` pass
Required evidence: Regression report
Member done: [ ]

`R5-D01`
Owner: `M1 + M6`
Task: Rehearse local multi-instance regression/fallback demo
Dependency: `R5-Q01`
Required evidence: Rehearsal result
Member done: [ ]

`R5-B01`
Owner: `M5 + M2`
Task: Implement SQLite `BillingSessions` target for active/closed billing, machine/client session reference, rental mode, start time, optional expiry, status and rate-per-hour restore behavior
Dependency: `R4-B01`; stable auth/session path
Required evidence: SQLite schema/reset/restore evidence
Member done: [ ]

`R5-B02`
Owner: `M3 + M5`
Task: Add Admin billing monitor evidence for timed rental, open-ended rental, extend action, rounded-minute amount display and active-session restore after ServerApp restart
Dependency: `R5-B01`
Required evidence: Admin Panel billing evidence and restore note
Member done: [ ]

`R5-B03`
Owner: `M2 + M4`
Task: Implement Client timed/free display evidence: countdown or temporary amount, 5-minute warning, expiry `LOCK` reaction, and no forced logout
Dependency: `R3` control pass; `R5-B01`
Required evidence: Client timer/billing and LOCK evidence
Member done: [ ]

`R5-C01`
Owner: `M2 + M3 + M4`
Task: Complete required 1-1 chat regression: Admin sends to selected client, client replies, other client does not receive the message
Dependency: `R4-C01`
Required evidence: `G5` chat test evidence
Member done: [ ]

`R5-L01`
Owner: `M2 + M6 + all owners`
Task: Execute required Real LAN rehearsal with two physical clients and keep local multi-instance regression passing
Dependency: `R5-D01` pass; LAN-capable listener/client setup
Required evidence: Physical LAN smoke/rehearsal note and local fallback result
Member done: [ ]

`R5-R01`
Owner: `M2 + M4 + M5 + M6`
Task: Verify minimal reconnect/resync after ServerApp restart: Admin restores active billing from SQLite and running client can sync timer/billing and receive extend/LOCK
Dependency: `R4-R01`; `R5-B01`
Required evidence: Restart/resync test result
Member done: [ ]

## R6 - Release And Demo (`2026-06-29` to `2026-07-05`)

`R6-L01`
Owner: `M1`
Task: Approve release candidate and freeze on `2026-06-30`
Dependency: `G5` candidate including LAN/chat/billing/restart restore
Required evidence: RC/freeze note
Member done: [ ]

`R6-Q01`
Owner: `M6 + all owners`
Task: Run two RC rehearsals covering local regression, physical LAN, required chat, timed/free billing, expiry LOCK and restart restore
Dependency: `R6-L01`
Required evidence: Two pass records
Member done: [ ]

`R6-D01`
Owner: `M1 + team`
Task: Deliver core demo by `2026-07-05`
Dependency: `G0-G5` pass
Required evidence: Final demo checklist including promoted required scope
Member done: [ ]

`R6-D02`
Owner: `M6`
Task: Publish required-scope pass/fail status and retained-extension continuation report
Dependency: Required demo and retained extension evidence
Required evidence: Final required-scope and continuation report
Member done: [ ]

## Retained Extension Track

| ID | Feature | Primary owners | Open condition | Before deadline target | Status |
| --- | --- | --- | --- | --- | --- |
| `E1` | Direct notification | M2, M3, M4, M6 | `G3` pass | Demonstrate if opened | `Conditional` |
| `E2` | Timer/billing display | M2, M3, M4, M5, M6 | Promoted by `2026-06-09` decision | Required `G5` demo evidence | `Promoted to Required Demo Scope` |
| `E3` | 1-1 text chat | M2, M3, M4, M6 | Promoted by `2026-06-09` decision | Required `G5` demo evidence | `Promoted to Required Demo Scope` |
| `E4` | Real LAN two-client demo | M2, M6, all UI owners | Promoted by `2026-06-09` decision | Required physical LAN evidence | `Promoted to Required Demo Scope` |
| `E5` | Notification broadcast | M2, M3, M4 | `E1` stable | Continue after core if needed | `Conditional` |
| `E6` | Billing/timer persistence | M5, M2 | Promoted by `2026-06-09` decision | Required SQLite restore evidence | `Promoted to Required Demo Scope` |
| `E7` | Reconnect polish beyond minimal resync | M2, M4 | Required minimal reconnect/resync pass | Continue after core if needed | `Conditional` |

## Retained Product Backlog

| Feature | Rule |
| --- | --- |
| Customer CRUD | Retained; do not open before `G5` |
| Shutdown control | Retained; do not open before `G5` |
| Dashboard polish beyond demo needs | Retained; do not block core gates |
| Reporting/analytics | Retained for post-core planning |

## Gate Counting Rule

Core delivery is complete only when `G0` through `G5` in `DOCS/TEST_MATRIX.md` are `Pass`, final local and physical LAN rehearsals pass, required chat/billing/restart restore pass, and no unaccepted High/Critical blocker remains. Retained extensions remain part of NetManager regardless of whether they are finished by core release.

Branch promotion does not replace gate counting: work merged into `develop` is only an integration candidate. It enters `main` only after the applicable M6 `Pass` evidence is recorded and M1 approves promotion.
