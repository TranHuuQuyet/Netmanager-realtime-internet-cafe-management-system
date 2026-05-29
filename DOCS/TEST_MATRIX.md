# TEST MATRIX - RECOVERY GATES

Baseline date: `2026-05-25`
Core delivery deadline: `2026-07-05`

## Status Legend

| Status        | Meaning                                                                           |
| ------------- | --------------------------------------------------------------------------------- |
| `Pass`        | Executed and verified against the current build                                   |
| `Fail`        | Executed and failed; requires bug entry                                           |
| `Blocked`     | Cannot execute because a required implementation/dependency is missing or failing |
| `Conditional` | Retained extension test, not opened until its gate allows work                    |
| `Not Run`     | Runnable gate has not yet been executed                                           |

Prior legacy matrix baseline: `0/33` tests were marked `Pass` at audit. The tables below are the active recovery matrix.

## Current Evidence Submissions

| Test                                                                         | Candidate result                      | Evidence                                                                                                                                                                                                                                                                                       | Acceptance state                                                                                                                              |
| ---------------------------------------------------------------------------- | ------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| `G0-01` Full solution builds                                                 | `Pass` on implementation working tree | `dotnet build Code/NetManager.sln --artifacts-path .audit-artifacts --no-restore -v:minimal` completed with `0` warnings and `0` errors after server login/client shell restoration                                                                                                            | Submitted for M6 verification; gate remains blocked until required `G0` cases pass                                                            |
| Server UI startup smoke                                                      | `Pass` on implementation working tree | Launching `ServerApp.exe` after correcting the `UiStrings` resource base name produced a responsive `Dang nhap` window                                                                                                                                                                         | Supporting evidence only; it does not prove listener, authentication result or `G1/G2`                                                        |
| Client UI startup smoke (`R1-U01`, `2026-05-26`, `6583b48`)                  | `Pass` on implementation working tree | On branch `quyet-clientapp-member4`, automated UI smoke opens responsive `ConnectForm`, `ClientMainForm` preview and `LockScreenForm` preview; lock preview displays that real `LOCK/UNLOCK` waits for routing; source boundary check finds no JSON/network service references in client forms | Supporting evidence for M4 shell submission only; it does not prove connection, auth result, status, control routing or ACK                   |
| Client customer-flow shell smoke (`R1-U01`, `2026-05-26`, working tree)      | `Pass` on implementation working tree | Full solution build passes with `0` warnings/errors; temporary .NET 8 smoke inspects the updated login/lock surfaces, launch option validation and `PC-02` multi-instance configuration without connecting to a server                                                                         | Supporting evidence for corrected client UX only; it does not prove TCP login, status, command routing or ACK                                 |
| Client plain WinForms smoke (`R1-U01`, `2026-05-26`, working tree)           | `Pass` on implementation working tree | Full solution build passes with `0` warnings/errors; temporary .NET 8 smoke verifies the compact server-style login dialog, default-control main/lock forms, launch identities and removal of custom theme dependencies                                                                        | Supporting evidence for client presentation only; real login/status/command routing and ACK remain blocked                                    |
| `G0-02/G0-03/G0-04` contract smoke (`R1-C02`, `2026-05-27`, working tree)    | `Pass` on implementation working tree | `dotnet run --project Code/ContractSmoke/ContractSmoke.csproj --no-restore` passes string packet-type serialization, numeric type rejection, `LOGIN` request/response split, request envelope nullables unset, and top-level login error envelope assertions                                   | Submitted for M6 verification; contract gate remains pending M1 approval and remaining `G0` dependencies                                      |
| `G1-01/G1-03` ServerApp network smoke (`R1-N01`, `2026-05-27`, working tree) | `Pass` on implementation working tree | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-restore` starts `TcpJsonLineServer`, accepts one local client, traces inbound `LOGIN`, returns typed `ACK` from `PacketDispatcher`, and verifies matching `requestId` + `status: "Success"`                               | Submitted for M6 verification; covers valid local listener/round-trip only; `G1-04/G1-05` invalid/unsupported packet behavior remains pending |

## G0 - Build And Contract (`R1`)

| ID      | Test                                                               | Owner        | Initial status | Evidence / blocker                                                  |
| ------- | ------------------------------------------------------------------ | ------------ | -------------- | ------------------------------------------------------------------- |
| `G0-01` | Full solution builds from approved setup command                   | M3 + M5 + M6 | `Pass(M6 - 2026-05-29)`         | Audit build failed at `ServerApp/Forms/LoginForm.Designer.cs(7,29)` |
| `G0-02` | Packet `type` serializes/deserializes as API string value          | M2 + M6      | `Pass(M6 - 2026-05-29)`      | Existing shared implementation not yet verified against API `v0.2`  |
| `G0-03` | `LOGIN` request and response parse into distinct expected paths    | M2 + M6      | `Pass(M6 - 2026-05-29)`      | Requires contract implementation correction                         |
| `G0-04` | Failure response emits top-level `success: false` and `error.code` | M2 + M5 + M6 | `Pass(M6 - 2026-05-29)`      | Requires contract/auth mapping                                      |
| `G0-05` | Canonical auth seed/database/admin rule match docs                 | M5 + M6      | `Blocked`      | Documentation decision recorded; runtime proof required             |

## G1 - Network Foundation (`R1`)

| ID      | Test                                                  | Mode  | Owner   | Initial status |
| ------- | ----------------------------------------------------- | ----- | ------- | -------------- |
| `G1-01` | Server starts and listens on recovery local endpoint  | Local | M2      | `Pass(M6 - 2026-05-29)`         |
| `G1-02` | One client connects without UI freeze                 | Local | M2 + M4 | `Pass(M6 - 2026-05-29)`         |
| `G1-03` | Client and server exchange one valid JSON-line packet | Local | M2      | `Pass(M6 - 2026-05-29)`         |
| `G1-04` | Invalid JSON fails gracefully without receiver crash  | Local | M2      | `Blocked`         |
| `G1-05` | Unsupported packet type yields controlled behavior    | Local | M2      | `Blocked`      |

## G2 - Authentication And Status (`R2`)

| ID      | Test                                                           | Mode  | Owner        | Initial status |
| ------- | -------------------------------------------------------------- | ----- | ------------ | -------------- |
| `G2-01` | Admin login succeeds with `admin` / `123` / `PC00`             | Local | M5 + M3      | `Blocked`      |
| `G2-02` | Client login succeeds with `client01` / `123` / `PC-01`        | Local | M5 + M4      | `Blocked`      |
| `G2-03` | Wrong password is rejected visibly                             | Local | M5 + M4      | `Blocked`      |
| `G2-04` | Correct client credentials with wrong `machineId` are rejected | Local | M5 + M4      | `Blocked`      |
| `G2-05` | Authenticated client sends status and dashboard shows online   | Local | M2 + M3 + M4 | `Blocked`      |
| `G2-06` | Disconnect/status update shows client offline or clearly stale | Local | M2 + M3      | `Blocked`      |

## G3 - Core Control (`R3`)

| ID      | Test                                                           | Mode  | Owner                | Initial status |
| ------- | -------------------------------------------------------------- | ----- | -------------------- | -------------- |
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
| `G5-04` | No unaccepted High/Critical demo blocker remains               | Both  | M1 + M6              | `Blocked`      |
| `G5-05` | Release docs match approved build and retained extension state | Local | M6                   | `Blocked`      |

## Retained Extension Tests

| ID      | Feature case                                                   | Open condition             | Owner             | Status        |
| ------- | -------------------------------------------------------------- | -------------------------- | ----------------- | ------------- |
| `E1-01` | Admin sends direct notification and correct client displays it | `G3` pass                  | M2 + M3 + M4 + M6 | `Conditional` |
| `E2-01` | Client displays timer update and expiry behavior is documented | `E1` pass; no core blocker | M2 + M4 + M6      | `Conditional` |
| `E3-01` | Admin and client exchange 1-1 text chat                        | `G4` pass on time          | M2 + M3 + M4 + M6 | `Conditional` |
| `E4-01` | One real LAN client connects and completes a smoke path        | Local rehearsal pass       | M2 + M6           | `Conditional` |
| `E5-01` | Notification broadcasts only to intended active clients        | `E1` stable                | M2 + GUI owners   | `Conditional` |
| `E6-01` | Timer survives required session persistence scenario           | `E2` and session stable    | M5 + M6           | `Conditional` |
| `E7-01` | Client reconnect UX behaves according to approved policy       | Disconnect core pass       | M2 + M4 + M6      | `Conditional` |

## Evidence Rule

- Every `Fail` must create or reference a bug in `DOCS/BUGS.md`.
- Every `Pass` must record test date, tested `develop` commit/build identity, mode q tester.
- M6 `Pass` evidence on the integrated `develop` candidate is required before M1 may approve a merge/promotion into `main`.
- A `Fail` or `Blocked` candidate remains outside `main` and is corrected through the feature/fix to `develop` flow.
- Extension tests remain visible even if recorded as `Retained - Continue After Core Release` in final reporting.


`G0-01` Full solution builds from approved setup command  

Status: PASS

Command: dotnet build Code/NetManager.sln

Result: Build succeeded.
        143 Warning(s)
        0 Error(s)

Conclusion: PASS

`G0-02`  Packet `type` serializes/deserializes as API string value

Status: Pass

Command: dotnet run --project Code/ContractSmoke/ContractSmoke.csproj

Result: PASS G0-02 packet type serializes as API string
        PASS G0-02 numeric packet type is rejected

Conclusion: Pass

`G0-03`  `LOGIN` request and response parse into distinct expected paths

Status: Pass

Command: dotnet run --project Code/ContractSmoke/ContractSmoke.csproj

Result: PASS G0-03 LOGIN request deserializes as request payload
        PASS G0-03 LOGIN request keeps response envelope fields unset
        PASS G0-03 LOGIN success deserializes as result payload

Conclusion: Pass

`G0-04`  Failure response emits top-level `success: false` and `error.code`

Status: Pass 

Command: dotnet run --project Code/ContractSmoke/ContractSmoke.csproj

Result: PASS G0-04 LOGIN failure uses top-level error envelope

Conclusion: Pass

`G0-05`  Canonical auth seed/database/admin rule match docs

`G1-01`  Server starts and listens on recovery local endpoint

Status: Pass

Command: dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj

Result: ServerApp listener active on 127.0.0.1:50833

Conclusion: Pass

`G1-02`  One client connects without UI freeze

Status: Not Tested

Command: dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj

Result: Smoke test không có UI, không thể verify qua lệnh này

Conclusion: Not Tested — cần UI integration test, chưa có trong R1

`G1-03`  Client and server exchange one valid JSON-line packet

Status: Pass

Command: dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj

Result: PASS: Client -> ServerApp listener ->typed
            dispatcher -> ACK JSON-line -> Client
            requestId khớp, status: "Success"

Conclusion: Pass — LOGIN gửi đi, ACK trả về đúng, round-trip hoàn chỉnh


`G1-04`  Invalid JSON fails gracefully without receiver crash

Status: Blocked

Command: —

Result: —

Conclusion: Blocked — chờ M2 hoàn thành R1-N02 (xử lý packet lỗi phía server)

`G1-05`  Unsupported packet type yields controlled behavior

Status: Blocked

Command: —

Result: —

Conclusion: Blocked — chờ M2 hoàn thành R1-N02 (xử lý packet type không hỗ trợ)