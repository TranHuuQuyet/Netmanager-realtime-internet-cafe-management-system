# R2 Verification Report

Prepared by: M6 (Tester & Documentation)
Audit date: 2026-06-08 (Asia/Saigon)
Sprint: R2 - Authenticated Status

## Candidate Under Test

Branch: `develop`
Commit: `01f06f9ab644ae772b67a804b1c42f69dc30804b`
Worktree: dirty before audit; `Code/ServerApp/Forms/MainForm.cs` had an existing uncommitted runtime-dashboard change.
Runtime mode: Local

Important audit note: G2 UI/dashboard evidence below is for the current working tree, not a clean committed `develop` checkout. The uncommitted `MainForm.cs` change must be committed/merged or the official gate evidence remains weaker than the runtime result.

## 1. Tong Quan R2

Actual runtime result: 5/6 G2 cases passed, 1/6 failed under strict R2 wording.

Docs result before this audit:

- `DOCS/TASKS.md`: 4/6 R2 tasks were checked as member-submitted (`R2-N01`, `R2-A01`, `R2-U01`, `R2-N02`), while `R2-U02` and `R2-L01` were unchecked.
- `DOCS/TEST_MATRIX.md`: 0/6 G2 cases were still recorded as `Blocked`.
- `DOCS/DEMO_CHECKLIST.md`: R2-relevant demo items were still `Blocked`.

Main discrepancy: code/runtime is ahead of the gate docs for login/auth and observable status, but `R2-N02` is overstated in `TASKS.md`. The server emits Online/Offline status after login/disconnect, but `ClientApp` does not send a `STATUS` packet and the dispatcher still rejects inbound `STATUS`.

Build and smoke evidence:

| Command | Result |
| --- | --- |
| `dotnet build Code\NetManager.sln --artifacts-path .audit-artifacts -v:minimal` | PASS, 0 warnings, 0 errors |
| `dotnet run --project Code\ContractSmoke\ContractSmoke.csproj --no-restore` | PASS contract smoke |
| `dotnet run --project Code\Auth_Test\Auth_Test.csproj --no-restore` | PASS `G2-01` to `G2-04` service/auth cases |
| `dotnet run --project Code\NetworkSmokeTest\NetworkSmoke.csproj --no-restore` | PASS TCP LOGIN success/failure and server-generated Online/Offline traces; confirms inbound `STATUS` unsupported |
| `dotnet run --project .audit-artifacts\r2\CurrentAdminLoginSmoke\CurrentAdminLoginSmoke.csproj -v:minimal` | PASS `G2-01` through `LoginForm` |
| `dotnet run --project .audit-artifacts\r2\CurrentClientLoginSmoke\CurrentClientLoginSmoke.csproj --no-restore -v:minimal` | PASS `G2-02`, `G2-03`, `G2-04` through `ConnectForm` real TCP/auth path |
| `dotnet run --project .audit-artifacts\r2\CurrentDashboardSmoke\CurrentDashboardSmoke.csproj -v:minimal` | PASS UI bridge for Online and Offline rows in current `MainForm` working tree |

## 2. Member 2 - Network Engineer

### Da Hoan Thanh

- `LOGIN` is routed by `PacketDispatcher.DispatchAsync` to `IAuthService.AuthenticateAsync`.
- LOGIN responses use API v0.2 packet envelopes with string packet type, top-level `success`, and top-level `error.code`.
- TCP runtime traces show valid login, invalid password, wrong machine, duplicate active login, invalid JSON, unknown packet, and server continuity after rejected packets.
- `TcpJsonLineServer` emits server-generated `STATUS` traces for Online after authenticated login and Offline after disconnect.

### Chua Hoan Thanh

- The server does not receive or route a client-sent `STATUS` packet.
- `PacketDispatcher` treats non-LOGIN packets as unsupported, and `NetworkSmokeTest` confirms inbound `STATUS` is rejected.

### Evidence

- `Code/ServerApp/Networking/PacketDispatcher.cs`: LOGIN dispatch and auth call.
- `Code/ServerApp/Networking/TcpJsonLineServer.cs`: `EmitStatus` and `StatusEmitted`.
- `Code/NetworkSmokeTest/Program.cs`: Online/Offline status trace checks and unsupported inbound `STATUS` check.

### Ket Luan

PARTIAL. Login routing is real and tested. Strict R2-N02 is not complete because client-sent `STATUS` receive/routing is missing.

## 3. Member 3 - Server GUI Developer

### Da Hoan Thanh

- `Program` wires `TcpJsonLineServer.StatusEmitted` into `MainForm.ApplyMachineStatusUpdate`.
- Current working tree `MainForm` can switch to runtime machine data and update a single `PC-01` row to `ONLINE` and `OFFLINE`.
- `CurrentDashboardSmoke` passed Online and Offline assertions against the current `ServerApp` project.

### Chua Hoan Thanh

- The runtime-dashboard cleanup is not cleanly committed in the current checkout; `MainForm.cs` is dirty.
- Full end-to-end visible ServerApp + ClientApp dashboard rehearsal was not run as a single interactive demo; the audit used targeted WinForms smoke plus network smoke.

### Evidence

- `Code/ServerApp/Program.cs`: status event binding to MainForm.
- `Code/ServerApp/Forms/MainForm.cs`: `ApplyMachineStatusUpdate`.
- `.audit-artifacts/r2/CurrentDashboardSmoke`: temporary M6 smoke project.

### Ket Luan

PARTIAL. The current working tree can render real Online/Offline status, but it is not yet clean committed gate evidence.

## 4. Member 4 - Client App Developer

### Da Hoan Thanh

- `ConnectForm` uses `TcpClientConnection`, `PacketFactory.CreateLogin`, and `JsonHelper` for real TCP LOGIN, not a fake dialog.
- Client UI binding smoke passed:
  - `client01` / `123` / `PC-01` succeeds.
  - `client01` / wrong password / `PC-01` returns `INVALID_CREDENTIALS`.
  - `client01` / `123` / `PC-02` returns `ACCOUNT_MACHINE_MISMATCH`.
- Error code mapping is visible in `CreateLoginFailureMessage`.

### Chua Hoan Thanh

- `ClientApp` has no `StatusPayload` send path.
- The live connection is retained by hidden `ConnectForm` during `ClientMainForm.ShowDialog`, but there is no client status service and no disconnect status packet sent by the client.

### Evidence

- `Code/ClientApp/Forms/ConnectForm.cs`: real LOGIN send/receive path.
- `Code/ClientApp/Networking/TcpClientConnection.cs`: TCP JSON-line client.
- `.audit-artifacts/r2/CurrentClientLoginSmoke`: temporary M6 smoke project.
- Source search found `CreateStatus`/`StatusPayload` only under `Shared` and `ServerApp\Networking`, not `ClientApp`.

### Ket Luan

PARTIAL. `R2-U01` passes; M4 support for `R2-N02` status send is missing.

## 5. Member 5 - Database & Authentication

### Da Hoan Thanh

- Canonical seed data matches docs: `admin` / `123` / `PC00`, `client01` / `123` / `PC-01`, `client02` / `123` / `PC-02`.
- `AuthService` validates username, password, role, machine binding, machine existence/activity, duplicate active machine state, and opens sessions.
- Error codes match API for tested G2 cases:
  - wrong password -> `INVALID_CREDENTIALS`
  - wrong machine -> `ACCOUNT_MACHINE_MISMATCH`

### Chua Hoan Thanh

- No R2 auth blocker found in this audit.

### Evidence

- `Code/ServerApp/Auth/Services/AuthService.cs`
- `Code/ServerApp/Auth/Services/SessionService.cs`
- `Code/ServerApp/Database/DatabaseBootstrapper.cs`
- `Code/Auth_Test/Program.cs`

### Ket Luan

PASS.

## 6. Ket Qua G2

| Test | Status | Evidence |
| --- | --- | --- |
| `G2-01` Admin login | PASS | `Auth_Test` and `CurrentAdminLoginSmoke` |
| `G2-02` Client login | PASS | `Auth_Test`, `NetworkSmokeTest`, `CurrentClientLoginSmoke` |
| `G2-03` Wrong password | PASS | `Auth_Test`, `NetworkSmokeTest`, `CurrentClientLoginSmoke` |
| `G2-04` Wrong machine | PASS | `Auth_Test`, `NetworkSmokeTest`, `CurrentClientLoginSmoke` |
| `G2-05` Status online | FAIL | Observable server-generated Online status exists, but client-sent `STATUS` is missing and inbound `STATUS` is unsupported (`B-008`) |
| `G2-06` Status offline/disconnect | PASS WITH RISK | Server-generated Offline trace and dashboard bridge pass; same client-sent `STATUS` gap remains |

## 7. Demo Checklist R2 Items

| Item | Result | Notes |
| --- | --- | --- |
| Valid Login | PARTIAL PASS | Single-client `client01/PC-01` passes through ClientApp binding. Full demo checklist still expects two clients and remains tied to `G4`. |
| Wrong Machine Check | PASS | `ACCOUNT_MACHINE_MISMATCH` verified through service, TCP, and ClientApp binding. |
| Status View | PARTIAL | Online/Offline is observable through server-generated status and current working-tree dashboard bridge. Strict client-sent `STATUS` is missing. |

## 8. Bug Check

Existing `B-003`: still open. R2 login/status is no longer completely absent, but control/ACK and strict client-sent status are not complete.

New bug:

| Bug ID | Severity | Owner | Expected | Actual | Impact |
| --- | --- | --- | --- | --- | --- |
| `B-008` | High | M2 + M4 | Client sends authenticated `STATUS`; server receives/routes it according to API/R2 prompt | `ClientApp` has no status sender; inbound `STATUS` is rejected as unsupported | Blocks strict `R2-N02`/`G2-05` pass and creates doc/code contract drift |

## 9. Dieu Kien Ban Giao Sang R3

### Nhung viec bat buoc con thieu

- Decide and document whether R2 accepts server-generated presence as the official status model. If not accepted, implement client-sent `STATUS` and dispatcher receive/routing.
- Add tests for authenticated inbound `STATUS` from ClientApp/service boundary.
- Commit/merge the `MainForm.cs` runtime dashboard cleanup before claiming clean `develop` gate pass.
- Update `TEST_MATRIX.md` and `DEMO_CHECKLIST.md` only after clean integrated evidence is accepted.
- Keep `B-003` open for R3 control/ACK until lock/unlock and ACK are implemented.

### Nguoi chiu trach nhiem

- M2: dispatcher/status route and trace.
- M4: ClientApp status service/disconnect send.
- M3: committed dashboard runtime view.
- M5: no current R2 auth fix required.
- M6/M1: retest and approve/deny the status-model exception.

### Muc Do Rui Ro

High.

## 10. Ket Luan Cuoi

NOT READY FOR R3.

Reason: login/auth is now real and tested, and observable server-generated Online/Offline status works in the current working tree. However, strict R2-N02/G2-05 is not complete because ClientApp does not send `STATUS` and the server rejects inbound `STATUS`. The dashboard pass also depends on an uncommitted `MainForm.cs` change. R3 should not be formally opened until `B-008` is fixed or M1 records an explicit decision accepting server-generated presence as the R2 status contract.
