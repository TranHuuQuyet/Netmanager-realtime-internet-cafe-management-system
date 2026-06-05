# R2 Verification Report
git stash pop   git stash push -u
Prepared by: M6 (Tester)

This report is the prepared verification template for Sprint R2. It records the checks required before the integrated `develop` candidate can be marked as passing `G2 Auth & Status`.

Sprint: R2 (2026-06-01 to 2026-06-07)

## Candidate Under Test

Date:

Tester: M6

Branch:

Commit / build identity: git log --oneline -1

Runtime mode: Local

Build command:

```powershell
dotnet build Code/NetManager.sln
```

Build result:

Status:

Conclusion:

## Required R2 Evidence From Members

Before M6 runs final `G2` verification, each owner should provide the evidence below.

| Task | Owner | Required evidence | Received | Notes |
| --- | --- | --- | --- | --- |
| `R2-N01` Route real `LOGIN` through TCP dispatcher and canonical auth service | M2 + M5 | Request/response trace from current integration build | Submitted(2026 - 06 - 05) | |
| `R2-A01` Verify valid login, bad password and wrong `machineId` auth behavior | M5 + M6 | Auth result evidence for `admin`, `client01`, bad password and wrong machine | No | |
| `R2-U01` Bind ClientApp login screen to real auth result | M4 | Visible success/error result in ClientApp | No | |
| `R2-N02` Emit `STATUS` after authenticated login and disconnect | M2 + M4 | Status packet trace or equivalent runtime trace | No | |
| `R2-U02` Render real online/offline state in ServerApp dashboard | M3 | Dashboard evidence showing real client status | No | |
| `R2-L01` Review `G2` and block control work if login/status fail | M1 + M6 | Gate review note after M6 result | No | |

## Approved Test Data

| Role | Username | Password | MachineId |
| --- | --- | --- | --- |
| Admin | `admin` | `123` | `PC00` |
| Client | `client01` | `123` | `PC-01` |
| Client | `client02` | `123` | `PC-02` |

Negative cases:

| Case | Input | Expected result |
| --- | --- | --- |
| Wrong password | `client01` / wrong password / `PC-01` | Login is rejected visibly |
| Wrong machine | `client01` / `123` / `PC-02` | Login is rejected visibly |
| Duplicate active machine, if applicable | Same active client logs in twice | Deterministic behavior is documented |

## Recommended Verification Commands

Run from repository root.

```powershell
dotnet build Code/NetManager.sln
dotnet run --project Code/Auth_Test/Auth_Test.csproj
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj
dotnet run --project Code/ServerApp/ServerApp.csproj
dotnet run --project Code/ClientApp/ClientApp.csproj
```

Notes:

- Build must pass before any R2 runtime result can be accepted.
- `Auth_Test` verifies the canonical seed/admin baseline.
- `NetworkSmokeTest` verifies the backend packet/auth path where applicable.
- UI cases must be verified through `ServerApp` and `ClientApp`, not only by shell startup.

## G2 Results

### `G2-01` Admin Login Succeeds

Test: Admin login succeeds with `admin` / `123` / `PC00`

Owner: M5 + M3

Status: Not Run

Command / steps:

1. Start `ServerApp`.
2. Login as `admin` with password `123` and machine id `PC00`.
3. Confirm the admin/server UI opens successfully and remains responsive.

Expected:

- Admin login succeeds.
- Server/admin UI enters the authenticated path.
- No startup or login exception occurs.

Actual:

Evidence:

Conclusion:

Bug / blocker:

### `G2-02` Client Login Succeeds

Test: Client login succeeds with `client01` / `123` / `PC-01`

Owner: M5 + M4

Status: Not Run

Command / steps:

1. Start `ServerApp`.
2. Start `ClientApp`.
3. Login as `client01` with password `123` and machine id `PC-01`.
4. Confirm the client UI shows a successful authenticated state.

Expected:

- Client login succeeds.
- Client UI displays the authenticated state.
- The login result comes from the real network/auth path.

Actual:

Evidence:

Conclusion:

Bug / blocker:

### `G2-03` Wrong Password Is Rejected

Test: Wrong password is rejected visibly

Owner: M5 + M4

Status: Not Run

Command / steps:

1. Start `ServerApp`.
2. Start `ClientApp`.
3. Attempt login with `client01`, an invalid password and machine id `PC-01`.
4. Confirm the client shows a controlled rejection.

Expected:

- Login is rejected.
- The rejection is visible to the user.
- The app remains responsive.
- No server or receiver crash occurs.

Actual:

Evidence:

Conclusion:

Bug / blocker:

### `G2-04` Wrong Machine Is Rejected

Test: Correct client credentials with wrong `machineId` are rejected

Owner: M5 + M4

Status: Not Run

Command / steps:

1. Start `ServerApp`.
2. Start `ClientApp` configured as `PC-02`, or otherwise submit machine id `PC-02`.
3. Attempt login with `client01` / `123` / `PC-02`.
4. Confirm the client shows a controlled rejection.

Expected:

- Login is rejected because `client01` is assigned to `PC-01`.
- The rejection is visible to the user.
- The app remains responsive.
- No server or receiver crash occurs.

Actual:

Evidence:

Conclusion:

Bug / blocker:

### `G2-05` Authenticated Client Sends Status

Test: Authenticated client sends status and dashboard shows online

Owner: M2 + M3 + M4

Status: Not Run

Command / steps:

1. Start `ServerApp`.
2. Login through `ClientApp` as `client01` / `123` / `PC-01`.
3. Confirm a real status event is emitted after authenticated login.
4. Confirm the ServerApp dashboard shows `PC-01` as online or active.

Expected:

- Status emission happens after authenticated login.
- Server dashboard displays the real online state.
- Dashboard state is not sample data.

Actual:

Evidence:

Conclusion:

Bug / blocker:

### `G2-06` Disconnect Status Is Reflected

Test: Disconnect/status update shows client offline or clearly stale

Owner: M2 + M3

Status: Not Run

Command / steps:

1. Start `ServerApp`.
2. Login through `ClientApp` as `client01` / `123` / `PC-01`.
3. Confirm `PC-01` appears online or active.
4. Close or disconnect `ClientApp`.
5. Confirm the ServerApp dashboard changes to offline or clearly marks the status as stale.

Expected:

- Disconnect does not crash the server.
- Server listener remains available.
- Dashboard updates to offline or clearly stale.
- The behavior is deterministic and documented.

Actual:

Evidence:

Conclusion:

Bug / blocker:

## Bug And Blocker Log

Every `Fail` must create or reference a bug in `DOCS/BUGS.md`. Every `Blocked` result must name the missing dependency and owner.

| ID | Related test | Severity | Owner | Expected | Actual | Status |
| --- | --- | --- | --- | --- | --- | --- |
| | | | | | | |

## Final G2 Summary

Overall R2/G2 status: Not Run

Passed:

Failed:

Blocked:

Open bugs:

Promotion recommendation:


