# R2 Verification Report

Prepared by: M6 (Tester)

This report is the prepared verification template for Sprint R2. It records the checks required before the integrated testing_branch candidate can be marked as passing G2 Auth & Status.

Sprint: R2 (2026-06-01 to 2026-06-07)

## Candidate Under Test

Date:2026-0906

Tester: M6

Branch:testing_branch

Commit / build identity: 0138731 (git log -oneline -1)

Runtime mode: Local

## Build Verification

Build command:

```powershell
dotnet build Code/NetManager.sln
```

Build result:Build succeeded.
            0 Warning(s)
            0 Error(s)

Status:Pass

Conclusion: Verification pass

## G2 Results

### G2-01 Admin Login Succeeds

Test: Admin login succeeds with `admin` / `123` / `PC00`

Owner: M5 + M3

Status: Pass

Verified date: 2026-06-09

#### Evidence

Command / method:

```powershell
dotnet build Code/ServerApp/ServerApp.csproj --no-restore
```

GUI verification:

```text
ServerRunning=True
ServerMainWindowTitle=MAY CHU
ServerTitles=MAY CHU
```

Credentials used:

```text
admin / 123 / PC00
```

#### Conclusion

Pass - admin login succeeds through the real ServerApp login UI and reaches the server/admin UI.

### G2-02 Client Login Succeeds

Test: Client login succeeds with `client01` / `123` / `PC-01`

Owner: M5 + M4

Status: Pass

Verified date: 2026-06-09

Verification type: Auth_Test output

#### Evidence

Command:

```powershell
dotnet run --project Code/Auth_Test/Auth_Test.csproj
```

Output:

```text
PASS G0-05: canonical auth seed/database/admin rule match docs
PASS G2-01: admin login succeeds with admin / 123 / PC00
PASS G2-02: client login succeeds with client01 / 123 / PC-01
PASS G2-03: wrong password is rejected visibly
PASS G2-04: correct client credentials with wrong machineId are rejected
```

Credentials used:

```text
client01 / 123 / PC-01
```

#### Conclusion

Pass - Auth_Test output confirms `G2-02`.

### G2-03 Wrong Password Is Rejected

Test: Wrong password is rejected visibly

Owner: M5 + M4

Status: Pass

Verified date: 2026-06-09

Verification type: Auth_Test runtime auth check

#### Evidence

Command:

```powershell
dotnet run --project Code/Auth_Test/Auth_Test.csproj
```

Result:

```text
PASS G0-05: canonical auth seed/database/admin rule match docs
PASS G2-01: admin login succeeds with admin / 123 / PC00
PASS G2-02: client login succeeds with client01 / 123 / PC-01
PASS G2-03: wrong password is rejected visibly
PASS G2-04: correct client credentials with wrong machineId are rejected
```

#### Conclusion

Pass - Auth_Test output confirms `G2-03`.

#### Bug / Blocker

None.

### G2-04 Wrong Machine Is Rejected

Test: Correct client credentials with wrong `machineId` are rejected

Owner: M5 + M4

Status: Pass

Verified date: 2026-06-09

Verification type: Auth_Test runtime auth check
#### Evidence

Command:

```powershell
dotnet run --project Code/Auth_Test/Auth_Test.csproj
```

Result:

```text
PASS G0-05: canonical auth seed/database/admin rule match docs
PASS G2-01: admin login succeeds with admin / 123 / PC00
PASS G2-02: client login succeeds with client01 / 123 / PC-01
PASS G2-03: wrong password is rejected visibly
PASS G2-04: correct client credentials with wrong machineId are rejected
```

#### Conclusion

Pass - Auth_Test output confirms `G2-04`.

### G2-05 Authenticated Client Sends Status

Test: Authenticated client sends status and dashboard shows online

Owner: M2 + M3 + M4

Status: Pass

Verified date: 2026-06-09

Verification type: NetworkSmokeTest authenticated status check

#### Evidence

Command:

```powershell
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-restore
```

Result:

```text
PASS: authenticated LOGIN emits STATUS Online
TRACE STATUS tcp-0001: ... "machineId":"PC-01" ... "status":"Online" ...
PASS: valid LOGIN returns authenticated session payload
```

#### Conclusion

Pass - authenticated login emits a real online status event for `PC-01`.

### G2-06 Disconnect Status Is Reflected

Test: Disconnect/status update shows client offline or clearly stale

Owner: M2 + M3

Status: Pass

Verified date: 2026-06-09

Verification type: NetworkSmokeTest disconnect/status check

#### Evidence

Command:

```powershell
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-restore
```

Result:

```text
PASS: authenticated LOGIN emits STATUS Online
TRACE STATUS tcp-0001: ... "machineId":"PC-01" ... "status":"Offline" ...
PASS: disconnected socket closes session 2b61108d1b264cc89c645098a4aa7722
PASS: authenticated disconnect emits STATUS Offline
PASS: Client -> ServerApp listener -> auth dispatcher -> controlled invalid/unsupported handling -> Client
```

#### Conclusion

Pass - disconnect updates authenticated client status to Offline and the ServerApp listener remains available.

## Bug And Blocker Log

Every Fail must create or reference a bug in `DOCS/BUGS.md`. Every Blocked result must name the missing dependency and owner.

| ID | Related test | Severity | Owner | Expected | Actual | Status |
| --- | --- | --- | --- | --- | --- | --- |
| None | All G2 tests | None | M6 | All G2 checks pass | No bug or blocker found | Closed |

## Final G2 Summary

Overall R2/G2 status: Pass

Passed: G2-01, G2-02, G2-03, G2-04, G2-05, G2-06

Failed: 0

Blocked: 0

Open bugs: 0

Promotion recommendation: Promote R2/G2 candidate.
