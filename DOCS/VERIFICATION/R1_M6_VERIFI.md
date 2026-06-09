# R1 Verification Report

Prepared by: M6 (Tester)

This report reformats the R1 verification evidence using the same structure as the R2 verification report. It records G0/G1 gate checks, bug verification notes, and final R1 status.

Sprint: R1 (2026-05-25 to 2026-05-31)

## Candidate Under Test

Date: 2026-05-29 and 2026-06-02 follow-up

Tester: M6

Branch:testing_branch

Commit / build identity:0138731

Runtime mode: Local

## Build Verification

Build command:

```powershell
dotnet build Code/NetManager.sln
```

Build result:

```text
Build succeeded.
143 Warning(s)
0 Error(s)
```

Status: Pass

Conclusion: Full solution build passed for R1 baseline verification.

## G0 Results

### G0-01 Full Solution Builds

Test: Full solution builds from approved setup command

Owner: M3 + M5 + M6

Status: Pass

Verified date: 2026-05-29

#### Evidence

Command:

```powershell
dotnet build Code/NetManager.sln
```

Result:

```text
Build succeeded.
143 Warning(s)
0 Error(s)
```

#### Conclusion

Pass - approved solution build completed successfully.

### G0-02 Packet Type Serializes As API String

Test: Packet `type` serializes/deserializes as API string value

Owner: M2 + M6

Status: Pass

Verified date: 2026-05-29

#### Evidence

Command:

```powershell
dotnet run --project Code/ContractSmoke/ContractSmoke.csproj
```

Result:

```text
PASS G0-02 packet type serializes as API string
PASS G0-02 numeric packet type is rejected
```

#### Conclusion

Pass - packet type contract matches the API string-value requirement.

### G0-03 LOGIN Request And Response Parse Correctly

Test: `LOGIN` request and response parse into distinct expected paths

Owner: M2 + M6

Status: Pass

Verified date: 2026-05-29

#### Evidence

Command:

```powershell
dotnet run --project Code/ContractSmoke/ContractSmoke.csproj
```

Result:

```text
PASS G0-03 LOGIN request deserializes as request payload
PASS G0-03 LOGIN request keeps response envelope fields unset
PASS G0-03 LOGIN success deserializes as result payload
```

#### Conclusion

Pass - LOGIN request and response deserialize into the expected contract paths.

### G0-04 Failure Response Uses Top-Level Error Envelope

Test: Failure response emits top-level `success: false` and `error.code`

Owner: M2 + M6

Status: Pass

Verified date: 2026-05-29

#### Evidence

Command:

```powershell
dotnet run --project Code/ContractSmoke/ContractSmoke.csproj
```

Result:

```text
PASS G0-04 LOGIN failure uses top-level error envelope
```

#### Conclusion

Pass - failure responses expose the required top-level success/error fields.

### G0-05 Canonical Auth Seed Matches Docs

Test: Canonical auth seed/database/admin rule match docs

Owner: M5 + M6

Status: Pass

Verified date: 2026-06-02

#### Evidence

Command:

```powershell
dotnet run --project Code/Auth_Test/Auth_Test.csproj
```

Result:

```text
PASS G0-05: canonical auth seed/database/admin rule match docs
```

#### Conclusion

Pass - canonical auth seed/database/admin rule baseline matches docs.

## G1 Results

### G1-01 Server Starts And Listens

Test: Server starts and listens on recovery local endpoint

Owner: M2 + M3 + M6

Status: Pass

Verified date: 2026-05-29

#### Evidence

Command:

```powershell
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj
```

Result:

```text
ServerApp listener active on 127.0.0.1:50833
```

#### Conclusion

Pass - ServerApp listener started on the local recovery endpoint.

### G1-02 Client Starts Without UI Freeze

Test: One client connects or starts without UI freeze

Owner: M4 + M6

Status: Pass

Verified date: 2026-06-02

#### Evidence

Command:

```powershell
dotnet run --project Code/ClientApp/ClientApp.csproj
```

Result:

```text
ClientApp window opened successfully; startup UI remained responsive with no visible freeze or startup exception.
```

#### Conclusion

Pass - ClientApp starts and remains responsive during startup.

### G1-03 Client And Server Exchange Valid JSON-Line Packet

Test: Client and server exchange one valid JSON-line packet

Owner: M2 + M6

Status: Pass

Verified date: 2026-05-29

#### Evidence

Command:

```powershell
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj
```

Result:

```text
PASS: Client -> ServerApp listener -> typed dispatcher -> ACK JSON-line -> Client
requestId matches, status: "Success"
```

#### Conclusion

Pass - LOGIN round-trip completed through the JSON-line network path.

### G1-04 Invalid JSON Fails Gracefully

Test: Invalid JSON fails gracefully without receiver crash

Owner: M2 + M6

Status: Pass

Verified date: 2026-06-02

#### Evidence

Command:

```powershell
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj
```

Result:

```text
CLIENT OUT: "{" invalid json
TRACE IN tcp-0004: "{" invalid json
TRACE DISPATCH_ERROR tcp-0004: 'i' is an invalid start of a property name.
After malformed JSON, the server still accepted and processed the next LOGIN request.
```

#### Conclusion

Pass - malformed JSON produced a controlled dispatch error and did not crash the receiver/listener.

### G1-05 Unsupported Packet Type Is Controlled

Test: Unsupported packet type yields controlled behavior

Owner: M2 + M6

Status: Pass

Verified date: 2026-06-02

#### Evidence

Command:

```powershell
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj
```

Result:

```text
TRACE DISPATCH_ERROR tcp-0009: The JSON value could not be converted to Shared.Enums.PacketType.
PASS: unknown packet type disconnects only the offending client and server remains available
TRACE DISPATCH_ERROR tcp-0011: Unsupported packet type: STATUS
PASS: packet type without an open route disconnects only the offending client and server remains available
```

#### Conclusion

Pass - unsupported packet types are handled in a controlled way without stopping the server listener.

## Bug Verification Log

| ID | Related test | Severity | Owner | Expected | Actual | Status |
| --- | --- | --- | --- | --- | --- | --- |
| B-001 | Build / Server UI startup | Critical | M3 + M5 + M4 | Solution builds and ServerApp login dialog opens | Build passed; ServerApp started; login dialog displayed; no startup crash observed | Verified Pass |
| B-002 | Network round-trip | High | M2 | Client reaches typed dispatcher and receives JSON-line response | Client -> ServerApp listener -> typed dispatcher -> ACK JSON-line -> Client | Partially Verified |
| B-003 | R1 pending control/ACK scope | High | M2 + M3 | Required information available for verification | No information recorded in R1 evidence | Blocked |
| B-004 | Contract smoke | High | M2 + M6 | API v0.2 packet/LOGIN/failure contract passes | ContractSmoke passed G0-02, G0-03, and G0-04 | Verified Pass |
| B-005 | Architecture decision | Medium | M1 + M5 | Canonical auth runtime path documented | SQLite auth centered on `AuthBootstrapper` and `AuthUsers/AuthSessions` selected; runtime verification still pending at first review | Partially Verified |
| B-006 | Seed/admin docs alignment | High | M5 + M6 | Runtime seed and docs align | Documentation aligned to `admin` / `123` / `PC00`; runtime confirmation was pending at first review, later covered by G0-05 | Partially Verified |

## Bug Test Details

### B-001 Build And Server UI Startup

Status: Verified Pass

#### Evidence

Command:

```powershell
dotnet build Code/NetManager.sln
```

Result:

```text
Build succeeded.
143 Warning(s)
0 Error(s)
```

Command:

```powershell
dotnet run --project Code/ServerApp/ServerApp.csproj
```

Result:

```text
ServerApp started successfully.
Dang nhap dialog displayed.
No startup crash observed.
```

#### Conclusion

B-001 verified pass.

### B-002 Network Round-Trip

Status: Partially Verified

#### Evidence

Command:

```powershell
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj
```

Result:

```text
Client -> ServerApp listener -> typed dispatcher -> ACK JSON-line -> Client
```

Limitation:

```text
It does not prove invalid/unsupported packet handling.
```

#### Conclusion

B-002 partially verified.

### B-003 Pending Control/ACK Scope

Status: Blocked

#### Evidence

Command:

```text
No information
```

Result:

```text
No information
```

#### Conclusion

B-003 blocked.

### B-004 API Contract Smoke

Status: Verified Pass

#### Evidence

Command:

```powershell
dotnet run --project Code/ContractSmoke/ContractSmoke.csproj
```

Result:

```text
PASS G0-02 packet type serializes as API string
PASS G0-02 numeric packet type is rejected
PASS G0-03 LOGIN request deserializes as request payload
PASS G0-03 LOGIN request keeps response envelope fields unset
PASS G0-03 LOGIN success deserializes as result payload
PASS G0-04 LOGIN failure uses top-level error envelope
Contract smoke checks passed.
```

Verified items:

```text
G0-02 Packet type serializes/deserializes as API string value.
G0-03 LOGIN request and response parse into expected paths.
G0-04 Failure response emits top-level success and error.code.
ContractSmoke completed without failures.
```

#### Conclusion

B-004 verified pass.

### B-005 Architecture Decision

Status: Partially Verified

#### Evidence Review

```text
Decision: the SQLite auth implementation centered on AuthBootstrapper and AuthUsers/AuthSessions is the canonical runtime path for the recovery delivery; machine status remains in-memory for core scope.
```

Verification result:

```text
Architecture direction has been formally selected and documented.
But code has not yet been fully verified.
```

#### Conclusion

B-005 partially verified.

### B-006 Seed/Admin Documentation Alignment

Status: Partially Verified

#### Evidence

Information:

```text
Documentation reviewed and aligned with the selected authentication runtime path.
Current documented seed:
Username: admin
MachineId: PC00
Password: 123
```

Verification:

```text
Documentation alignment confirmed but runtime login has not yet been verified.
```

#### Conclusion

B-006 partially verified. Documentation issue resolved. Runtime confirmation was pending in the original R1 bug note and later covered by G0-05.

## Final G0/G1 Summary

Overall R1 status: Pass with noted bug-log exceptions

Passed: G0-01, G0-02, G0-03, G0-04, G0-05, G1-01, G1-02, G1-03, G1-04, G1-05

Failed: 0

Blocked: B-003 bug verification remained blocked in R1 evidence

Open bugs: B-003 remained open/pending based on R1 report content

Promotion recommendation: R1 G0/G1 verification is acceptable, with B-003 carried forward as pending scope.
