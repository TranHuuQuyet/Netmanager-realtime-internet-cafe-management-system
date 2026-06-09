# TEST MATRIX - RECOVERY GATES

Baseline date: `2026-05-25`
Core delivery deadline: `2026-07-05`

## Status Legend

| Status        | Meaning                                                                           |
| ------------- | --------------------------------------------------------------------------------- |
| `Pass`        | Executed and verified against the current build                                   |
| `Fail`        | Executed and failed; requires bug entry                                           |
| `Blocked`     | Cannot execute because a required implementation/dependency is missing or failing |
| `Evidence Submitted` | Implementation evidence passes locally and awaits M6 verification          |
| `Conditional` | Retained extension test, not opened until its gate allows work                    |
| `Promoted to Required Demo Scope` | Former extension test moved into required `G5` scope by the `2026-06-09` decision |
| `Not Run`     | Runnable gate has not yet been executed                                           |

Prior legacy matrix baseline: `0/33` tests were marked `Pass` at audit. The tables below are the active recovery matrix.

## Current Evidence Submissions

| Test                                                                         | Candidate result                      | Evidence                                                                                                                                                                                                                                                                                       | Acceptance state                                                                                                                              |
| ---------------------------------------------------------------------------- | ------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| `G0-01` Full solution builds                                                 | `Pass` on implementation working tree | `dotnet build Code/NetManager.sln --artifacts-path .audit-artifacts --no-restore -v:minimal` completed with `0` warnings and `0` errors after server login/client shell restoration and Windows-only WinForms annotation                                                                         | Pass(M6 - 2026-05-29)                                                            |
| Server UI startup smoke                                                      | `Pass` on implementation working tree | Launching `ServerApp.exe` after correcting the `UiStrings` resource base name produced a responsive `Dang nhap` window                                                                                                                                                                         | Supporting evidence only; it does not prove listener, authentication result or `G1/G2`                                                        |
| M3 server dashboard R1 shell honesty                                         | `Pass` on implementation working tree | `MainForm` now labels the dashboard as sample data, keeps lock/unlock visibly backend-pending, and provides a UI-only status update bridge for future typed events                                                                                                                              | Supporting evidence only; it does not prove authenticated status, dashboard online/offline runtime, command routing or ACK                    |
| Client UI startup smoke (`R1-U01`, `2026-05-26`, `6583b48`)                  | `Pass` on implementation working tree | On branch `quyet-clientapp-member4`, automated UI smoke opens responsive `ConnectForm`, `ClientMainForm` preview and `LockScreenForm` preview; lock preview displays that real `LOCK/UNLOCK` waits for routing; source boundary check finds no JSON/network service references in client forms | Supporting evidence for M4 shell submission only; it does not prove connection, auth result, status, control routing or ACK                   |
| Client customer-flow shell smoke (`R1-U01`, `2026-05-26`, working tree)      | `Pass` on implementation working tree | Full solution build passes with `0` warnings/errors; temporary .NET 8 smoke inspects the updated login/lock surfaces, launch option validation and `PC-02` multi-instance configuration without connecting to a server                                                                         | Supporting evidence for corrected client UX only; it does not prove TCP login, status, command routing or ACK                                 |
| Client plain WinForms smoke (`R1-U01`, `2026-05-26`, working tree)           | `Pass` on implementation working tree | Full solution build passes with `0` warnings/errors; temporary .NET 8 smoke verifies the compact server-style login dialog, default-control main/lock forms, launch identities and removal of custom theme dependencies                                                                        | Supporting evidence for client presentation only; real login/status/command routing and ACK remain blocked                                    |
| `G1-02` ClientApp startup UI smoke (`M4`, `2026-06-02`, working tree)        | `Pass` on implementation working tree | `dotnet build Code\NetManager.sln --artifacts-path .audit-artifacts --no-restore -v:minimal` completed with `0` warnings and `0` errors; UIAutomation launched `.audit-artifacts\bin\ClientApp\debug\ClientApp.exe` as default `PC-01`, launched `--machine-id=PC-02`, read `txtMachineId`, confirmed the window was responsive, and closed via both main-window close and `btnExit` | Pass(M6 - 2026-06-02) for UI startup only; runtime TCP connect/login/status remains tracked in `R2-N01`, `R2-U01`, `R2-N02`, `G2-02`, `G2-05`, `G2-06` |
| `G0-02/G0-03/G0-04` contract smoke (`R1-C02`, `2026-05-27`, working tree)    | `Pass` on implementation working tree | `dotnet run --project Code/ContractSmoke/ContractSmoke.csproj --no-restore` passes string packet-type serialization, numeric type rejection, `LOGIN` request/response split, request envelope nullables unset, and top-level login error envelope assertions                                   | Pass(M6 2026-06-02) |
| `G1-01/G1-03/G1-04/G1-05` ServerApp network smoke (`R1-N01/R1-N02`, `2026-06-02`, working tree) | `Pass` on implementation working tree | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-restore` starts `TcpJsonLineServer`, verifies authenticated `LOGIN` success/failure, traces controlled dispatch errors for malformed JSON, an unknown type and unopened `STATUS`, disconnects only the offending socket, then verifies a fresh valid login after each rejection | Pass(M6 2026-06-02) |
| `G2` R2 audit (`2026-06-08`, dirty working tree) | `Partial` | Build passes with `0` warnings/errors; `Auth_Test`, `NetworkSmokeTest`, `CurrentAdminLoginSmoke`, `CurrentClientLoginSmoke` and `CurrentDashboardSmoke` verify real auth/login and server-generated Online/Offline UI bridge. Strict client-sent `STATUS` is missing and inbound `STATUS` remains unsupported. | `G2-01`-`G2-04` Pass; `G2-06` Pass with risk; `G2-05` Fail (`B-008`). Not ready for R3 without fix or M1 exception. |

## G0 - Build And Contract (`R1`)

| ID      | Test                                                               | Owner        | Initial status | Evidence / blocker                                                  |
| ------- | ------------------------------------------------------------------ | ------------ | -------------- | ------------------------------------------------------------------- |
| `G0-01` | Full solution builds from approved setup command                   | M3 + M5 + M6 | `Pass(M6 - 2026-05-29)`         | Audit build failed at `ServerApp/Forms/LoginForm.Designer.cs(7,29)` |
| `G0-02` | Packet `type` serializes/deserializes as API string value          | M2 + M6      | `Pass(M6 - 2026-05-29)`      | Existing shared implementation not yet verified against API `v0.2`  |
| `G0-03` | `LOGIN` request and response parse into distinct expected paths    | M2 + M6      | `Pass(M6 - 2026-05-29)`      | Requires contract implementation correction                         |
| `G0-04` | Failure response emits top-level `success: false` and `error.code` | M2 + M5 + M6 | `Pass(M6 - 2026-05-29)`      | Requires contract/auth mapping                                      |
| `G0-05` | Canonical auth seed/database/admin rule match docs                 | M5 + M6      | `Pass(M6 - 2026-06-02)`         | Runtime seed in `internet_cafe.db` matches `admin`/`client01`/`client02` and admin `PC00` rule |

## G1 - Network Foundation (`R1`)

| ID      | Test                                                  | Mode  | Owner   | Initial status |
| ------- | ----------------------------------------------------- | ----- | ------- | -------------- |
| `G1-01` | Server starts and listens on recovery local endpoint  | Local | M2      | `Pass(M6 - 2026-05-29)`         |
| `G1-02` | ClientApp starts without UI freeze                    | Local | M4 + M6 | `Pass(M6 - 2026-06-02)` |
| `G1-03` | Client and server exchange one valid JSON-line packet | Local | M2      | `Pass(M6 - 2026-05-29)`         |
| `G1-04` | Invalid JSON fails gracefully without receiver crash  | Local | M2      | `Pass(M6 - 2026-06-02)` |
| `G1-05` | Unsupported packet type yields controlled behavior    | Local | M2      | `Pass(M6 - 2026-06-02)` |

## G2 - Authentication And Status (`R2`)

| ID      | Test                                                           | Mode  | Owner        | Initial status |
| ------- | -------------------------------------------------------------- | ----- | ------------ | -------------- |
| `G2-01` | Admin login succeeds with `admin` / `123` / `PC00`             | Local | M5 + M3      | `Pass(M6 - 2026-06-08; working tree)`      |
| `G2-02` | Client login succeeds with `client01` / `123` / `PC-01`        | Local | M5 + M4      | `Pass(M6 - 2026-06-08; working tree)`      |
| `G2-03` | Wrong password is rejected visibly                             | Local | M5 + M4      | `Pass(M6 - 2026-06-08; working tree)`      |
| `G2-04` | Correct client credentials with wrong `machineId` are rejected | Local | M5 + M4      | `Pass(M6 - 2026-06-08; working tree)`      |
| `G2-05` | Authenticated client sends status and dashboard shows online   | Local | M2 + M3 + M4 | `Fail(M6 - 2026-06-08; B-008)`      |
| `G2-06` | Disconnect/status update shows client offline or clearly stale | Local | M2 + M3      | `Pass(M6 - 2026-06-08; risk B-008)`      |

## G3 - Core Control (`R3`)

| ID      | Test                                                           | Mode  | Owner                | Initial status |
| ------- | -------------------------------------------------------------- | ----- | -------------------- | -------------- |
| `G3-00` | `G2-05/B-008` status policy is fixed or explicitly accepted before control readiness is claimed | Local | M1 + M2 + M4 + M6 | `Blocked` |
| `G3-01` | Admin locks authenticated target client                        | Local | M2 + M3 + M4         | `Blocked`      |
| `G3-02` | Client returns visible ACK after lock                          | Local | M2 + M3 + M4         | `Blocked`      |
| `G3-03` | Admin unlocks target client and client exits lock state        | Local | M2 + M3 + M4         | `Blocked`      |
| `G3-04` | Invalid/unauthorized command displays controlled error         | Local | M2 + M5              | `Blocked`      |
| `G3-05` | One-client login/status/lock/ACK/unlock flow passes repeatedly | Local | M6 + all core owners | `Blocked`      |

## G4 - Multi-Client Stability (`R4`)

| ID      | Test                                                  | Mode                 | Owner             | Initial status |
| ------- | ----------------------------------------------------- | -------------------- | ----------------- | -------------- |
| `G4-01` | `client01` and `client02` connect and remain distinct | Local multi-instance | M2 + M3 + M4 + M5 | `Blocked`      |
| `G4-02` | Command routes only to selected machine               | Local multi-instance | M2 + M3 + M4      | `Blocked`      |
| `G4-03` | Duplicate active login behavior is deterministic      | Local multi-instance | M2 + M5           | `Blocked`      |
| `G4-04` | Client disconnect does not crash server               | Local multi-instance | M2 + M6           | `Blocked`      |

## G5 - Release Readiness (`R5-R6`)

| ID      | Test                                                           | Mode  | Owner                | Initial status |
| ------- | -------------------------------------------------------------- | ----- | -------------------- | -------------- |
| `G5-01` | Clean setup follows current run guide                          | Local | M6                   | `Blocked`      |
| `G5-02` | Full core regression passes                                    | Local | M6 + all core owners | `Blocked`      |
| `G5-03` | Local multi-instance rehearsal passes twice on RC              | Local | M1 + M6              | `Blocked`      |
| `G5-04` | Two physical LAN clients connect/login and remain distinct while local fallback still passes | Real LAN + Local | M2 + M3 + M4 + M5 + M6 | `Blocked` |
| `G5-05` | Admin and selected client exchange 1-1 chat; non-selected client does not receive it | Local + LAN | M2 + M3 + M4 + M6 | `Blocked` |
| `G5-06` | Admin starts timed rental package `5-10` minutes; Admin/Client show countdown, warn at 5 minutes, and expiry sends `LOCK` without logout | Local + LAN | M2 + M3 + M4 + M5 + M6 | `Blocked` |
| `G5-07` | Open-ended rental amount uses rounded-minute formula; `61` seconds charges `2` minutes at `10000` VND/hour | Local | M3 + M5 + M6 | `Blocked` |
| `G5-08` | ServerApp restart restores active `BillingSessions` from SQLite and Admin Panel continues calculating time/amount | Local | M3 + M5 + M6 | `Blocked` |
| `G5-09` | Minimal reconnect/resync lets a running client sync billing/timer and receive extend/LOCK after ServerApp restart | Local + LAN | M2 + M4 + M5 + M6 | `Blocked` |
| `G5-10` | No unaccepted High/Critical demo blocker remains               | Both  | M1 + M6              | `Blocked`      |
| `G5-11` | Release docs match approved build, required promoted scope and retained extension state | Both | M6 | `Blocked` |

## Retained Extension Tests

| ID      | Feature case                                                   | Open condition             | Owner             | Status        |
| ------- | -------------------------------------------------------------- | -------------------------- | ----------------- | ------------- |
| `E1-01` | Admin sends direct notification and correct client displays it | `G3` pass                  | M2 + M3 + M4 + M6 | `Conditional` |
| `E5-01` | Notification broadcasts only to intended active clients        | `E1` stable                | M2 + GUI owners   | `Conditional` |
| `E7-01` | Client reconnect UX polish behaves according to approved policy beyond required minimal resync | `G5-09` pass | M2 + M4 + M6 | `Conditional` |

## Evidence Rule

- Every `Fail` must create or reference a bug in `DOCS/BUGS.md`.
- Every `Pass` must record test date, tested `develop` commit/build identity, mode q tester.
- M6 `Pass` evidence on the integrated `develop` candidate is required before M1 may approve a merge/promotion into `main`.
- A `Fail` or `Blocked` candidate remains outside `main` and is corrected through the feature/fix to `develop` flow.
- Extension tests remain visible even if recorded as `Retained - Continue After Core Release` in final reporting; tests promoted by the `2026-06-09` decision are counted under `G5`, not under retained extensions.
