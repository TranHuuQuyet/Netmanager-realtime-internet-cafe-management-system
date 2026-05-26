# TASKS - RECOVERY TRACKER

Active tracker reset date: `2026-05-25`
Core delivery deadline: `2026-07-05`
Baseline report: `DOCS/RECOVERY_REPORT_2026-05-25.md`

Roadmap 8 tuan cu da bi supersede boi recovery roadmap vi build va runtime gate chua dat. Lich su checkbox cu va evidence cua tung member duoc luu trong recovery report; khong duoc dung checkbox cu de claim runtime progress.

## Tracking Rules

- Trong cac bang task `R1-R6`, `[x]` nghia la owner da nop phan viec/evidence duoc yeu cau; `[ ]` nghia la chua nop, dang bi block hoac chua mo.
- Checkbox la checklist nop viec cua member, khong phai ket qua runtime verification hay gate approval.
- Runtime/release pass duoc xac nhan trong `DOCS/TEST_MATRIX.md` va `DOCS/DEMO_CHECKLIST.md`; M6 verify evidence va M1 approve gate khi can.

Cac status chu sau van dung cho baseline, evidence submission va retained extension tracking:

| Status | Meaning |
| --- | --- |
| `Verified Pass` | Evidence runtime/test da duoc M6 xac nhan va M1 chap nhan neu la gate |
| `Evidence Submitted` | Owner da nop evidence, cho verify |
| `In Progress` | Dang lam, chua du evidence |
| `Blocked` | Bi chan boi dependency; evidence ghi ro blocker/owner |
| `Not Started` | Chua den thu tu thuc hien |
| `Conditional` | Extension duoc giu lai, chi mo khi core gate pass |
| `Retained - Continue After Core Release` | Van thuoc roadmap nhung tiep tuc sau `2026-07-05` |
| `Historical Artifact` | Artifact cu ton tai, khong phai delivery proof |

## Current Baseline Status

| Item | Current status | Evidence / blocker |
| --- | --- | --- |
| Full solution build | `Fail` | Build audit fail tai `ServerApp/Forms/LoginForm.Designer.cs(7,29)` do thieu `LoginForm.cs` trong working tree |
| Contract approval | `Blocked` | API/shared/auth/database dang drift; can M1/M2/M5 freeze baseline `v0.2` |
| Runtime tests | `Blocked` | `0/33` legacy test cases da pass tai baseline audit |
| Demo path | `Blocked` | `14/14` legacy demo steps van pending tai baseline audit |
| M4 client forms | `Historical Artifact` | Forms local/uncommitted ton tai, chua consume runtime service |

## Current Evidence Submissions

| Item | Submitted evidence | Current disposition |
| --- | --- | --- |
| Full solution build candidate | `dotnet build Code/NetManager.sln --artifacts-path .audit-artifacts --no-restore -v:minimal` passes with `0` warnings and `0` errors after restoring the server login path and adding an explicit client shell entry form | `Evidence Submitted`; pending M6 verification and M1 gate approval |
| Server login path | `Program` initializes typed auth off the UI thread and passes `IAuthService` into `LoginForm`; the resource manifest lookup is corrected and a startup smoke opens the `Dang nhap` dialog responsively | `Evidence Submitted`; local admin authentication interaction still requires verification |
| Client startup path | `ConnectForm` is present as a buildable shell and explicitly states that network login binding remains pending `G1/G2` | Build blocker removed only; no runtime integration claim |
| `R1-U01` client UI shell - `2026-05-26`, commit `6583b48` | On branch `quyet-clientapp-member4`, `dotnet build Code/NetManager.sln --artifacts-path .audit-artifacts --no-restore -v:minimal` passes with `0` warnings and `0` errors; UI smoke opens responsive `ConnectForm`, `ClientMainForm` preview and `LockScreenForm` preview; lock preview displays that real `LOCK/UNLOCK` waits for routing; boundary search finds no JSON/network service references in client forms | `Evidence Submitted`; UI shell buildable; network login, `LOCK`/`UNLOCK` and `ACK` runtime are not integrated; M6 verification pending |
| `R1-U01` customer-flow shell correction - `2026-05-26`, working tree | `dotnet build Code/NetManager.sln --artifacts-path .audit-artifacts -v:minimal` passes with `0` warnings and `0` errors; a temporary .NET 8 smoke verifies username/password-only login, read-only configured machine identity, hidden endpoint, no local lock action, `--machine-id PC-02` launch configuration, rejected invalid launch configuration, honest pending-login status and passive lock surface with `UnlockFromServer()` release hook | `Evidence Submitted`; corrects client shell ownership/UX only; TCP login and server-routed `LOCK`/`UNLOCK`/`ACK` remain unintegrated; M6 verification pending |
| `R1-U01` plain WinForms client refinement - `2026-05-26`, working tree | `dotnet build Code/NetManager.sln --artifacts-path .audit-artifacts -v:minimal` passes with `0` warnings and `0` errors; .NET 8 smoke verifies compact `424 x 318` login dialog matching server-style controls, read-only `PC-01`/`PC-02` machine identity, default buttons only, themed UI removal across client forms, and passive lock release through `UnlockFromServer()` | `Evidence Submitted`; presentation refinement only; login/status/control routing and ACK remain pending their runtime gates; M6 verification pending |
| `R1-A01` auth handoff + canonical DB path - `2026-05-26`, working tree | `AuthBootstrapper` resolves `internet_cafe.db` from repository root, `AuthStatusExtensions` maps auth statuses to API codes, and `PacketFactory`/`JsonHelper` now carry top-level login success/failure envelope so M2 can distinguish request vs response | `Evidence Submitted`; `R1-A01` is complete as a handoff artifact, but `G0/G1` still need runtime verification before the gate can pass |

`G0` is not passed by this submission: API `v0.2` contract checks, auth runtime verification and M6/M1 acceptance remain outstanding.

## R1 - Foundation Repair (`2026-05-25` to `2026-05-31`)

`R1-L01`
Owner: `M1`
Task: Approve recovery scope, deadline, core/extension lanes va merge gates
Dependency: Recovery report
Required evidence: Decision entry + team notice
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
Member done: [ ]

`R1-A01`
Owner: `M5 + M1`
Task: Declare SQLite auth path, seed/admin rule va runtime schema canonical
Dependency: `R1-L01`
Required evidence: Decision + auth handoff note
Member done: [x]

`R1-N01`
Owner: `M2`
Task: Implement server listener, typed dispatcher baseline va local JSON-line round-trip
Dependency: `R1-C01`, `R1-C02`
Required evidence: Trace valid request/response
Member done: [ ]

`R1-N02`
Owner: `M2 + M6`
Task: Validate invalid/unsupported packet does not crash receiver
Dependency: `R1-N01`
Required evidence: `G1` test result
Member done: [ ]

`R1-U01`
Owner: `M4`
Task: Integrate client form artifacts into buildable branch without claiming runtime integration
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
Task: Route `LOGIN` from TCP dispatcher to canonical auth service
Dependency: `G0`, `G1` pass
Required evidence: Request/response trace
Member done: [ ]

`R2-A01`
Owner: `M5 + M6`
Task: Verify admin/client valid login, bad password va wrong `machineId`
Dependency: `R2-N01`
Required evidence: `G2` auth cases pass
Member done: [ ]

`R2-U01`
Owner: `M4`
Task: Bind client login screen to real network/auth result
Dependency: `R2-N01`
Required evidence: Visible success/error result
Member done: [ ]

`R2-N02`
Owner: `M2 + M4`
Task: Emit `STATUS` after authenticated client login va disconnect
Dependency: `R2-A01`
Required evidence: Status packet trace
Member done: [ ]

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

`R3-N01`
Owner: `M2 + M3`
Task: Route real `LOCK`/`UNLOCK` commands from selected client action
Dependency: `G2` pass
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
Dependency: `G2` pass
Required evidence: Auth/session test result
Member done: [ ]

`R3-Q01`
Owner: `M6 + M1`
Task: Run repeat one-client core demo and approve `G3`
Dependency: All R3 tasks
Required evidence: `G3` pass/bug list
Member done: [ ]

## R4 - Multi-Client And Notification (`2026-06-15` to `2026-06-21`)

`R4-N01`
Owner: `M2 + M5`
Task: Route two authenticated clients distinctly and decide duplicate-login behavior
Dependency: `G3` pass
Required evidence: Routing/session test
Member done: [ ]

`R4-U01`
Owner: `M3 + M4`
Task: Render/maintain distinct local client instances
Dependency: `R4-N01`
Required evidence: Two-client UI evidence
Member done: [ ]

`R4-N02`
Owner: `M2 + M6`
Task: Verify disconnect does not crash server
Dependency: `R4-N01`
Required evidence: `G4` disconnect case
Member done: [ ]

`R4-E01`
Owner: `M2 + M3 + M4`
Task: Implement direct notification if `G3` passed
Dependency: `G3` pass
Required evidence: `E1` test
Member done: [ ]

`R4-Q01`
Owner: `M6 + M1`
Task: Approve `G4` and determine which retained extensions open
Dependency: Core R4 tasks
Required evidence: Gate/extension decision
Member done: [ ]

## R5 - Stabilization And Opened Extensions (`2026-06-22` to `2026-06-28`)

`R5-Q01`
Owner: `M6 + all owners`
Task: Run core regression, bug triage and clean setup verification
Dependency: `G4` pass
Required evidence: Regression report
Member done: [ ]

`R5-D01`
Owner: `M1 + M6`
Task: Rehearse local multi-instance primary/fallback demo
Dependency: `R5-Q01`
Required evidence: Rehearsal result
Member done: [ ]

`R5-E01`
Owner: `M2 + M4`
Task: Implement timer display if extension is opened
Dependency: `E1` pass; no High/Critical bug
Required evidence: `E2` test
Member done: [ ]

`R5-E02`
Owner: `M2 + M3 + M4`
Task: Implement 1-1 chat only if opened by M1
Dependency: `G4` pass by `2026-06-21`
Required evidence: `E3` test
Member done: [ ]

`R5-E03`
Owner: `M2 + M6`
Task: Execute Real LAN smoke only after local rehearsal pass
Dependency: `R5-D01` pass
Required evidence: `E4` smoke note
Member done: [ ]

## R6 - Release And Demo (`2026-06-29` to `2026-07-05`)

`R6-L01`
Owner: `M1`
Task: Approve release candidate and freeze on `2026-06-30`
Dependency: `G5` candidate
Required evidence: RC/freeze note
Member done: [ ]

`R6-Q01`
Owner: `M6 + all owners`
Task: Run two core rehearsals on RC build
Dependency: `R6-L01`
Required evidence: Two pass records
Member done: [ ]

`R6-D01`
Owner: `M1 + team`
Task: Deliver core demo by `2026-07-05`
Dependency: `G0-G5` pass
Required evidence: Final demo checklist
Member done: [ ]

`R6-D02`
Owner: `M6`
Task: Publish extension status: pass, opened/incomplete, or retained
Dependency: Extension evidence
Required evidence: Final continuation report
Member done: [ ]

## Retained Extension Track

| ID | Feature | Primary owners | Open condition | Before deadline target | Status |
| --- | --- | --- | --- | --- | --- |
| `E1` | Direct notification | M2, M3, M4, M6 | `G3` pass | Demonstrate if opened | `Conditional` |
| `E2` | Timer display | M2, M4, M6 | `E1` pass and no High/Critical blocker | Demonstrate if opened | `Conditional` |
| `E3` | 1-1 text chat | M2, M3, M4, M6 | `G4` pass by `2026-06-21` | Attempt only if opened | `Conditional` |
| `E4` | Real LAN smoke | M2, M6 | Local rehearsal pass by `2026-06-28` | Smoke evidence, not core gate | `Conditional` |
| `E5` | Notification broadcast | M2, M3, M4 | `E1` stable | Continue after core if needed | `Conditional` |
| `E6` | Timer persistence | M5, M2 | `E2` and core session stable | Continue after core if needed | `Conditional` |
| `E7` | Reconnect polish | M2, M4 | Disconnect core test pass | Continue after core if needed | `Conditional` |

## Retained Product Backlog

| Feature | Rule |
| --- | --- |
| Customer CRUD | Retained; do not open before `G5` |
| Shutdown control | Retained; do not open before `G5` |
| Dashboard polish beyond demo needs | Retained; do not block core gates |
| Reporting/analytics | Retained for post-core planning |

## Gate Counting Rule

Core delivery is complete only when `G0` through `G5` in `DOCS/TEST_MATRIX.md` are `Pass`, final local rehearsal passes twice, and no unaccepted High/Critical blocker remains. Retained extensions remain part of NetManager regardless of whether they are finished by core release.
