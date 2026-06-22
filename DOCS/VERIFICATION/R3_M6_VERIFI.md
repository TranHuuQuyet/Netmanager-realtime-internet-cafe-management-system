# R3 Verification Report

Prepared by: M6 (Tester)

This report records Sprint R3 verification before the integrated testing_branch candidate is marked as passing G3 Core Control.

Sprint: R3 (2026-06-08 to 2026-06-14)

## Candidate Under Test

Date: 2026-06-16

Tester: M6

Branch: testing_branch

Commit / build identity: f5e63ae

Runtime mode: Local

## Build Verification

Build command:

```gitbash
dotnet build Code/NetManager.sln
```

Build result:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Status: Pass

Conclusion: Build passes before G3 verification continues.

## Smoke / Evidence Command

Network control smoke:

```gitbash
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-build --no-restore
```

Optional clean run if binaries are stale:

```gitbash
dotnet build Code/NetworkSmokeTest/NetworkSmoke.csproj -v:minimal
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-restore
```

## G3 Results

### G3-00 Status Policy Precondition

Test: `G2-05/B-008` status policy is fixed or explicitly accepted before control readiness is claimed.

Owner: M1 + M2 + M4 + M6

Status: Pass

Verified date: 2026-06-16

Verification type: NetworkSmokeTest authenticated status check

#### Evidence

Command:

```gitbash
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-build --no-restore
```

Result:

```text
DOCS/VERIFICATION/R3_B008_STATUS_VERIFICATION.md
PASS: authenticated STATUS route returns Accepted ACK
TRACE STATUS ... "machineId":"PC-01" ... "status":"Online" ...
```

#### Conclusion

Pass - status route precondition is ready for G3.

### G3-01 Admin Locks Authenticated Target Client

Test: Admin locks authenticated target client.

Owner: M2 + M3 + M4

Status: Pass

Verified date: 2026-06-16

Verification type: NetworkSmokeTest command packet trace

#### Evidence

Command:

```gitbash
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-build --no-restore
```

Result:

```text
TRACE STATUS tcp-0002: {"payload":{"machineId":"PC-01","machineName":"PC-01","status":"Online","ipAddress":"127.0.0.1","lastSeen":"2026-06-16T07:44:49.2110543Z"},"type":"STATUS","source":"server","target":"PC-01","timestamp":"2026-06-16T07:44:49.2110546Z"}
TRACE OUT_COMMAND tcp-0002: {"payload":{"machineId":"PC-01","issuedBy":"ServerApp.MainForm","reason":"Admin UI lock request"},"type":"LOCK","source":"server","target":"PC-01","requestId":"58d8bd02edd3469780451516d7f4e87c","timestamp":"2026-06-16T07:44:49.2935573Z"}
COMMAND JSON LOCK  : {"payload":{"machineId":"PC-01","issuedBy":"ServerApp.MainForm","reason":"Admin UI lock request"},"type":"LOCK","source":"server","target":"PC-01","requestId":"58d8bd02edd3469780451516d7f4e87c","timestamp":"2026-06-16T07:44:49.2935573Z"}
CLIENT IN : {"payload":{"machineId":"PC-01","issuedBy":"ServerApp.MainForm","reason":"Admin UI lock request"},"type":"LOCK","source":"server","target":"PC-01","requestId":"58d8bd02edd3469780451516d7f4e87c","timestamp":"2026-06-16T07:44:49.2935573Z"}
```

#### Conclusion

Pass - authenticated target receives real Admin `LOCK`.

### G3-02 Client Returns Visible ACK After Lock

Test: Client returns visible ACK after lock.

Owner: M2 + M3 + M4

Status: Pass

Verified date: 2026-06-16

Verification type: Typed LOCK ACK trace plus command result evidence

#### Evidence

Command:

```gitbash
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-build --no-restore
```

Result:

```text
CLIENT OUT: {"payload":{"machineId":"PC-01","ackFor":"LOCK","status":"Success","message":"Lock applied."},"type":"ACK","source":"PC-01","target":"server","requestId":"58d8bd02edd3469780451516d7f4e87c","timestamp":"2026-06-16T07:44:49.4221877Z"}
TRACE COMMAND_ACK tcp-0002: {"payload":{"machineId":"PC-01","ackFor":"LOCK","status":"Success","message":"Lock applied."},"type":"ACK","source":"PC-01","target":"server","requestId":"58d8bd02edd3469780451516d7f4e87c","timestamp":"2026-06-16T07:44:49.4221877Z"}
COMMAND RESULT LOCK PC-01: Success  58d8bd02edd3469780451516d7f4e87c
COMMAND ACK LOCK: {"payload":{"machineId":"PC-01","ackFor":"LOCK","status":"Success","message":"Lock applied."},"type":"ACK","source":"PC-01","target":"server","requestId":"58d8bd02edd3469780451516d7f4e87c","timestamp":"2026-06-16T07:44:49.4221877Z"}
PASS: admin UI Lock action emits real LOCK JSON command packet and receives typed ACK
```

#### Conclusion

Pass - `LOCK` returns typed ACK/result.

### G3-03 Admin Unlocks Target Client

Test: Admin unlocks target client and client exits lock state.

Owner: M2 + M3 + M4

Status: Pass

Verified date: 2026-06-16

Verification type: NetworkSmokeTest unlock ACK trace plus typed command result evidence

#### Evidence

Command:

```gitbash
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-build --no-restore
```

Result:

```text
TRACE OUT_COMMAND tcp-0002: {"payload":{"machineId":"PC-01","issuedBy":"ServerApp.MainForm","reason":"Admin UI unlock request"},"type":"UNLOCK","source":"server","target":"PC-01","requestId":"eed72b976df846a0943fa6f4357a890e","timestamp":"2026-06-16T07:44:49.4252209Z"}
COMMAND JSON UNLOCK: {"payload":{"machineId":"PC-01","issuedBy":"ServerApp.MainForm","reason":"Admin UI unlock request"},"type":"UNLOCK","source":"server","target":"PC-01","requestId":"eed72b976df846a0943fa6f4357a890e","timestamp":"2026-06-16T07:44:49.4252209Z"}
CLIENT OUT: {"payload":{"machineId":"PC-01","ackFor":"UNLOCK","status":"Success","message":"Unlock applied."},"type":"ACK","source":"PC-01","target":"server","requestId":"eed72b976df846a0943fa6f4357a890e","timestamp":"2026-06-16T07:44:49.4529686Z"}
TRACE COMMAND_ACK tcp-0002: {"payload":{"machineId":"PC-01","ackFor":"UNLOCK","status":"Success","message":"Unlock applied."},"type":"ACK","source":"PC-01","target":"server","requestId":"eed72b976df846a0943fa6f4357a890e","timestamp":"2026-06-16T07:44:49.4529686Z"}
COMMAND RESULT UNLOCK PC-01: Success  eed72b976df846a0943fa6f4357a890e
PASS: admin UI Unlock action emits real UNLOCK JSON command packet and receives typed ACK
```

#### Conclusion

Pass - `UNLOCK` returns typed ACK/result.

### G3-04 Invalid Or Unauthorized Command Error

Test: Invalid/unauthorized command displays controlled error.

Owner: M2 + M5

Status: Pass

Verified date: 2026-06-16

Verification type: NetworkSmokeTest deterministic command error trace

#### Evidence

Command:

```gitbash
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-build --no-restore
```

Result:

```text
TRACE COMMAND_ACK_ERROR tcp-0002: UNAUTHORIZED_COMMAND: ACK machine does not match authenticated connection.
COMMAND RESULT LOCK PC-02: Error UNAUTHORIZED_COMMAND 58d8bd02edd3469780451516d7f4e87c
TRACE COMMAND_ACK_ERROR tcp-0002: ACK_UNKNOWN_REQUEST: ACK requestId does not match a pending command.
COMMAND RESULT LOCK PC-01: Error ACK_UNKNOWN_REQUEST ack-missing-2d4a6aff0f2640a8a1cc68f94308c290
TRACE COMMAND_ACK_ERROR tcp-0002: ACK_TYPE_MISMATCH: ACK type does not match the pending command.
COMMAND RESULT LOCK PC-01: Error ACK_TYPE_MISMATCH 58d8bd02edd3469780451516d7f4e87c
TRACE COMMAND_ACK_ERROR tcp-0002: INVALID_PACKET: ACK status must be Success, Failed or Ignored.
COMMAND RESULT LOCK PC-01: Error INVALID_PACKET 58d8bd02edd3469780451516d7f4e87c
TRACE COMMAND_ERROR : INVALID_MACHINE_ID: Machine ID is required.
PASS: invalid machine command returns deterministic INVALID_MACHINE_ID error
TRACE COMMAND_ERROR PC-99: MACHINE_OFFLINE: Machine is offline or not connected.
PASS: offline command returns deterministic MACHINE_OFFLINE error
TRACE COMMAND_ACK_ERROR tcp-0002: COMMAND_CLIENT_DISCONNECTED: Client disconnected before ACK.
COMMAND RESULT LOCK PC-01: Error COMMAND_CLIENT_DISCONNECTED b5a4b5a658144b9c9ec0125335748dfe
PASS: pending command emits typed COMMAND_CLIENT_DISCONNECTED on client disconnect
```

#### Conclusion

Pass - invalid command paths return controlled errors.

### G3-05 Repeat One-Client Core Flow

Test: One-client login/status/lock/ACK/unlock flow passes repeatedly.

Owner: M6 + all core owners

Status: Pass

Verified date: 2026-06-16

Verification type: Local smoke core flow

#### Evidence

Command:

```gitbash
dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-build --no-restore
```

Result:

```text
PASS: authenticated LOGIN emits STATUS Online
PASS: authenticated STATUS route returns Accepted ACK
PASS: admin UI Lock action emits real LOCK JSON command packet and receives typed ACK
PASS: admin UI Unlock action emits real UNLOCK JSON command packet and receives typed ACK
PASS: authenticated disconnect emits STATUS Offline
PASS: Client -> ServerApp listener -> auth dispatcher -> controlled invalid/unsupported handling -> Client
```

#### Conclusion

Pass - one-client login/status/lock/ACK/unlock flow passes.

## Task-Level R3 Disposition

Use this section to map gate evidence back to `DOCS/TASKS.md`.

| Task | Owner | Required evidence | Verification result | Notes |
| --- | --- | --- | --- | --- |
| `R3-B01` | M2 + M4 + M6 | Updated trace/test result and bug disposition | Pass | B-008/status route accepted ACK |
| `R3-N01` | M2 + M3 | Command packet trace | Pass | LOCK/UNLOCK JSON route from admin action |
| `R3-U01` | M4 | Visible client reaction | Pass | Smoke client received LOCK/UNLOCK and returned applied ACK |
| `R3-N02` | M2 + M4 | ACK/error trace | Pass | Typed ACK and deterministic command errors verified |
| `R3-U02` | M3 | Dashboard result evidence | Pass | COMMAND RESULT boundary maps ACK/error for dashboard |
| `R3-A01` | M5 | Auth/session test result | Pass | Active session/machine guard and mismatch checks verified |
| `R3-Q01` | M6 + M1 | G3 pass/bug list | Pass | Local G3 evidence ready for M1 review |

## Bug And Blocker Log

Every Fail must create or reference a bug in `DOCS/BUGS.md`. Every Blocked result must name the missing dependency and owner.

| ID | Related test | Severity | Owner | Expected | Actual | Status |
| --- | --- | --- | --- | --- | --- | --- |
| None | All G3 tests | None | M6 | G3 command/control checks pass | No blocker found in smoke evidence | Closed |

## Final G3 Summary

Overall R3/G3 status: Pass on local NetworkSmokeTest evidence

Passed: G3-00, G3-01, G3-02, G3-03, G3-04, G3-05

Failed: 0

Blocked: 0

Open bugs: 0

Residual risks:

```text
No open smoke blocker. Separate manual screenshot evidence may still be added if M1 requires UI-only proof.
```

Promotion recommendation:

```text
Evidence submitted for G3 pass; promote after M1 approval.
```
