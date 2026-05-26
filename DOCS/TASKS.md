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
| `R1-A01` auth handoff + canonical DB path - `2026-05-26`, working tree | `AuthBootstrapper` resolves `internet_cafe.db` from repository root, seeds canonical `admin` / `client01` / `client02` accounts, keeps `AuthUsers` and `AuthSessions` as the runtime tables, and `AuthStatusExtensions` maps auth statuses to API codes | `Evidence Submitted`; handoff is canonical in code, while `G0-05` runtime verification still remains before the gate can pass |

`G0` is not passed by this submission: API `v0.2` contract checks, auth runtime verification and M6/M1 acceptance remain outstanding.

## R1 - Foundation Repair (`2026-05-25` to `2026-05-31`)

| ID | Owner | Task | Dependency | Required evidence | Member done |
| --- | --- | --- | --- | --- | --- |
| `R1-L01` | M1 | Approve recovery scope, deadline, core/extension lanes va merge gates | Recovery report | Decision entry + team notice | [x] |
| `R1-C01` | M3 + M5 | Restore buildable server login path va full solution build | Current broken form state | `dotnet build Code/NetManager.sln` pass | [x] |
| `R1-C02` | M2 + M1 | Freeze API `v0.2`; align string packet type, LOGIN response va error envelope | Contract review | API approval + serialization tests | [ ] |
| `R1-A01` | M5 + M1 | Declare SQLite auth path, seed/admin rule va runtime schema canonical | `R1-L01` | Decision + auth handoff note | [x] |
| `R1-N01` | M2 | Implement server listener, typed dispatcher baseline va local JSON-line round-trip | `R1-C01`, `R1-C02` | Trace valid request/response | [ ] |
| `R1-N02` | M2 + M6 | Validate invalid/unsupported packet does not crash receiver | `R1-N01` | `G1` test result | [ ] |
| `R1-U01` | M4 | Integrate client form artifacts into buildable branch without claiming runtime integration | `R1-C01` | Build/UI smoke note | [x] |
| `R1-Q01` | M6 | Record initial fail/blocked statuses and high-severity blockers | Audit evidence | Updated tests/bugs docs | [x] |

## R2 - Authenticated Status (`2026-06-01` to `2026-06-07`)

| ID | Owner | Task | Dependency | Required evidence | Member done |
| --- | --- | --- | --- | --- | --- |
| `R2-N01` | M2 + M5 | Route `LOGIN` from TCP dispatcher to canonical auth service | `G0`, `G1` pass | Request/response trace | [ ] |
| `R2-A01` | M5 + M6 | Verify admin/client valid login, bad password va wrong `machineId` | `R2-N01` | `G2` auth cases pass | [ ] |
| `R2-U01` | M4 | Bind client login screen to real network/auth result | `R2-N01` | Visible success/error result | [ ] |
| `R2-N02` | M2 + M4 | Emit `STATUS` after authenticated client login va disconnect | `R2-A01` | Status packet trace | [ ] |
| `R2-U02` | M3 | Render real one-client online/offline state | `R2-N02` | Dashboard evidence + M6 pass | [ ] |
| `R2-L01` | M1 + M6 | Review `G2` va block control work neu login/status fail | All R2 core tasks | Gate review note | [ ] |

## R3 - Core Control (`2026-06-08` to `2026-06-14`)

| ID | Owner | Task | Dependency | Required evidence | Member done |
| --- | --- | --- | --- | --- | --- |
| `R3-N01` | M2 + M3 | Route real `LOCK`/`UNLOCK` commands from selected client action | `G2` pass | Command packet trace | [ ] |
| `R3-U01` | M4 | Apply lock/unlock client state through runtime command handler | `R3-N01` | Visible client reaction | [ ] |
| `R3-N02` | M2 + M4 | Send typed `ACK` and deterministic command error | `R3-U01` | ACK/error trace | [ ] |
| `R3-U02` | M3 | Show ACK/error result in admin UI | `R3-N02` | Dashboard result evidence | [ ] |
| `R3-A01` | M5 | Enforce active session/machine guard for command target | `G2` pass | Auth/session test result | [ ] |
| `R3-Q01` | M6 + M1 | Run repeat one-client core demo and approve `G3` | All R3 tasks | `G3` pass/bug list | [ ] |

## R4 - Multi-Client And Notification (`2026-06-15` to `2026-06-21`)

| ID | Owner | Task | Dependency | Required evidence | Member done |
| --- | --- | --- | --- | --- | --- |
| `R4-N01` | M2 + M5 | Route two authenticated clients distinctly and decide duplicate-login behavior | `G3` pass | Routing/session test | [ ] |
| `R4-U01` | M3 + M4 | Render/maintain distinct local client instances | `R4-N01` | Two-client UI evidence | [ ] |
| `R4-N02` | M2 + M6 | Verify disconnect does not crash server | `R4-N01` | `G4` disconnect case | [ ] |
| `R4-E01` | M2 + M3 + M4 | Implement direct notification if `G3` passed | `G3` pass | `E1` test | [ ] |
| `R4-Q01` | M6 + M1 | Approve `G4` and determine which retained extensions open | Core R4 tasks | Gate/extension decision | [ ] |

## R5 - Stabilization And Opened Extensions (`2026-06-22` to `2026-06-28`)

| ID | Owner | Task | Dependency | Required evidence | Member done |
| --- | --- | --- | --- | --- | --- |
| `R5-Q01` | M6 + all owners | Run core regression, bug triage and clean setup verification | `G4` pass | Regression report | [ ] |
| `R5-D01` | M1 + M6 | Rehearse local multi-instance primary/fallback demo | `R5-Q01` | Rehearsal result | [ ] |
| `R5-E01` | M2 + M4 | Implement timer display if extension is opened | `E1` pass; no High/Critical bug | `E2` test | [ ] |
| `R5-E02` | M2 + M3 + M4 | Implement 1-1 chat only if opened by M1 | `G4` pass by `2026-06-21` | `E3` test | [ ] |
| `R5-E03` | M2 + M6 | Execute Real LAN smoke only after local rehearsal pass | `R5-D01` pass | `E4` smoke note | [ ] |

## R6 - Release And Demo (`2026-06-29` to `2026-07-05`)

| ID | Owner | Task | Dependency | Required evidence | Member done |
| --- | --- | --- | --- | --- | --- |
| `R6-L01` | M1 | Approve release candidate and freeze on `2026-06-30` | `G5` candidate | RC/freeze note | [ ] |
| `R6-Q01` | M6 + all owners | Run two core rehearsals on RC build | `R6-L01` | Two pass records | [ ] |
| `R6-D01` | M1 + team | Deliver core demo by `2026-07-05` | `G0-G5` pass | Final demo checklist | [ ] |
| `R6-D02` | M6 | Publish extension status: pass, opened/incomplete, or retained | Extension evidence | Final continuation report | [ ] |

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
