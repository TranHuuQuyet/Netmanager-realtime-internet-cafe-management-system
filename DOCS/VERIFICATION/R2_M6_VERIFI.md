# R2 Verification Report
Prepared by: M6 (Tester)
This report summarizes the verification activities prepared for Sprint R2, including authentication, machine validation, status update verification, evidence review, bug verification, and the current status of `G2 Auth & Status`.
Sprint: R2 (2026-06-01 to 2026-06-07)

## Date:

## Candidate Information

Branch:

Commit / build identity:

Runtime mode: Local

Tester: M6

## Build Result

`R2-BUILD` Full solution builds from approved setup command

Status: Not Run

Command: dotnet build Code/NetManager.sln

Result:

Conclusion:

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

Command: dotnet build Code/NetManager.sln

Purpose: Verify the integrated R2 candidate still builds.

Command: dotnet run --project Code/Auth_Test/Auth_Test.csproj

Purpose: Verify canonical auth seed/database/admin baseline.

Command: dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj

Purpose: Verify backend packet/auth path and network stability where applicable.

Command: dotnet run --project Code/ServerApp/ServerApp.csproj

Purpose: Verify ServerApp login/dashboard behavior through UI.

Command: dotnet run --project Code/ClientApp/ClientApp.csproj

Purpose: Verify ClientApp real login/status behavior through UI.

Notes:

- Build must pass before any R2 runtime result can be accepted.
- A screen opening without real network/auth interaction is not enough for `G2` pass.
- UI cases must be verified through `ServerApp` and `ClientApp`, not only through backend smoke tests.

## R2 Evidence Review

`R2-N01` Route real `LOGIN` from TCP dispatcher to canonical auth service

Owner: M2 + M5

Required evidence: Request/response trace

Status: Pending

Evidence:

Conclusion:

`R2-A01` Verify admin/client valid login, bad password and wrong `machineId`

Owner: M5 + M6

Required evidence: `G2` auth cases pass

Status: Pending

Evidence:

Conclusion:

`R2-U01` Bind client login screen to real M2/M5 network/auth result

Owner: M4

Required evidence: Visible success/error result

Status: Pending

Evidence:

Conclusion:

`R2-N02` Emit `STATUS` after authenticated client login and disconnect

Owner: M2 + M4

Required evidence: Status packet trace

Status: Pending

Evidence:

Conclusion:

`R2-U02` Render real one-client online/offline state

Owner: M3

Required evidence: Dashboard evidence + M6 pass

Status: Pending

Evidence:

Conclusion:

`R2-L01` Review `G2` and block control work if login/status fail

Owner: M1 + M6

Required evidence: Gate review note

Status: Pending

Evidence:

Conclusion:

## G2 Results

`G2-01` Admin login succeeds with `admin` / `123` / `PC00`

Status: Not Run

Command: dotnet run --project Code/ServerApp/ServerApp.csproj

Steps:
1. Start ServerApp.
2. Login as `admin` with password `123` and machine id `PC00`.
3. Confirm the authenticated server/admin UI opens and remains responsive.

Expected:
Admin login succeeds, the authenticated UI opens, and no startup/login exception occurs.

Result:

Conclusion:

`G2-02` Client login succeeds with `client01` / `123` / `PC-01`

Status: Not Run

Command:
dotnet run --project Code/ServerApp/ServerApp.csproj
dotnet run --project Code/ClientApp/ClientApp.csproj

Steps:
1. Start ServerApp.
2. Start ClientApp.
3. Login as `client01` with password `123` and machine id `PC-01`.
4. Confirm ClientApp displays the authenticated state.

Expected:
Client login succeeds through the real network/auth path and the client UI shows success.

Result:

Conclusion:

`G2-03` Wrong password is rejected visibly

Status: Not Run

Command:
dotnet run --project Code/ServerApp/ServerApp.csproj
dotnet run --project Code/ClientApp/ClientApp.csproj

Steps:
1. Start ServerApp.
2. Start ClientApp.
3. Attempt login with `client01`, an invalid password and machine id `PC-01`.
4. Confirm ClientApp shows a controlled rejection.

Expected:
Login is rejected visibly, ClientApp remains responsive, and the server does not crash.

Result:

Conclusion:

`G2-04` Correct client credentials with wrong `machineId` are rejected

Status: Not Run

Command:
dotnet run --project Code/ServerApp/ServerApp.csproj
dotnet run --project Code/ClientApp/ClientApp.csproj

Steps:
1. Start ServerApp.
2. Start ClientApp configured as `PC-02`, or otherwise submit machine id `PC-02`.
3. Attempt login with `client01` / `123` / `PC-02`.
4. Confirm ClientApp shows a controlled rejection.

Expected:
Login is rejected because `client01` belongs to `PC-01`; the rejection is visible and no app crashes.

Result:

Conclusion:

`G2-05` Authenticated client sends status and dashboard shows online

Status: Not Run

Command:
dotnet run --project Code/ServerApp/ServerApp.csproj
dotnet run --project Code/ClientApp/ClientApp.csproj

Steps:
1. Start ServerApp.
2. Login through ClientApp as `client01` / `123` / `PC-01`.
3. Confirm a real status event is emitted after authenticated login.
4. Confirm ServerApp dashboard shows `PC-01` as online or active.

Expected:
Status is emitted after authenticated login and ServerApp displays real online state instead of sample data.

Result:

Conclusion:

`G2-06` Disconnect/status update shows client offline or clearly stale

Status: Not Run

Command:
dotnet run --project Code/ServerApp/ServerApp.csproj
dotnet run --project Code/ClientApp/ClientApp.csproj

Steps:
1. Start ServerApp.
2. Login through ClientApp as `client01` / `123` / `PC-01`.
3. Confirm `PC-01` appears online or active.
4. Close or disconnect ClientApp.
5. Confirm ServerApp dashboard changes to offline or clearly stale.

Expected:
Disconnect does not crash ServerApp; the listener remains available and dashboard state becomes offline or clearly stale.

Result:

Conclusion:

## Bug Verification

## B-003

Issue: No integrated login/status/control demo

R2 scope: Verify login and status portion only; control remains R3.

Command:

Result:

Conclusion:

## R2 Bug And Blocker Log

Every `Fail` must create or reference a bug in `DOCS/BUGS.md`. Every `Blocked` result must name the missing dependency and owner.

| ID | Related test | Severity | Owner | Status | Notes |
| --- | --- | --- | --- | --- | --- |
| | | | | | |

## Final G2 Summary

Passed:

Failed:

Blocked:

Open bugs:

Overall status: Not Run

Promotion recommendation:

M6 conclusion:
