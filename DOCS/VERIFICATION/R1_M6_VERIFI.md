# R1 Verification Report

Prepared by: M6 (Tester)

This report summarizes the verification activities performed during Sprint R1, including test execution results, evidence review, bug verification, and the current status of project features.

Sprint: R1 (2026-05-25 to 2026-05-31)

## Date: 2026-05-29

## G0 Results

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

Status: Blocked

Command: No information

Result: No information 

Conclusion: Blocked

## G1 Results

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

Command: No information

Result: No information 

Conclusion: Blocked

`G1-05`  Unsupported packet type yields controlled behavior

Status: Blocked

Command: No information

Result: No information 

## B-001 

Command: dotnet build Code/NetManager.sln

Result:Build succeeded.
        143 Warning(s)
        0 Error(s)

Command:dotnet run --project Code/ServerApp/ServerApp.csproj

Result:vServerApp started successfully.
        Dang nhap dialog displayed.
        No startup crash observed.

Conclusion: B-001 VERIFIED PASS

## B-002
 
Command: dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj

Result: Client -> ServerApp listener -> typed dispatcher -> ACK JSON-line -> Client

Limitation: It does not prove invalid/unsupported packet handling

Conclution: PARTIALLY VERIFIED
## B-003

Command: No information

Result: No informaion 

Conclusion: Blocked 

## B-004 

Command: dotnet run --project Code/ContractSmoke/ContractSmoke.csproj

Result:PASS G0-02 packet type serializes as API string
        PASS G0-02 numeric packet type is rejected
        PASS G0-03 LOGIN request deserializes as request payload
        PASS G0-03 LOGIN request keeps response envelope fields unset
        PASS G0-03 LOGIN success deserializes as result payload
        PASS G0-04 LOGIN failure uses top-level error envelope
        Contract smoke checks passed.

Status: PASS

Conclusion: B-004 VERIFIED PASS
        API v0.2 contract verification completed successfully.
Verified items:
        G0-02 Packet type serializes/deserializes as API string value.
        G0-03 LOGIN request and response parse into expected paths.
        G0-04 Failure response emits top-level success and error.code.
        ContractSmoke completed without failures.

## B-005

Evidence Review:Decision: the SQLite auth implementation centered on `AuthBootstrapper` and `AuthUsers/AuthSessions` is the canonical
                runtime path for the recovery delivery; machine status remains in-memory for core scope

Verification Result:Architecture direction has been formally selected and documented
                But code has not yet been fully verified

Conclusion: PARTIALLY VERIFIED

## B-006

Infomation: Documentation reviewed and aligned with the selected authentication runtime path.
        Current documented seed:
                        Username: admin
                        MachineId: PC00
                        Password: 123

Verification: Documentation alignmet confirmed but runtime login has not yet been verified

Conclusion: PARTIALLY VERIFIED
        Documentation issue resolved.
        Runtime confirmation pending.

## Day:2026-06-02

`G0-05`  Canonical auth seed/database/admin rule match docs

Status: Pass

Command: dotnet run --project Code/Auth_Test/Auth_Test.csproj

Result: PASS G0-05: canonical auth seed/database/admin rule match docs

Conclusion: Pass - canonical auth seed/database/admin rule baseline matches docs.

`G1-02`  ClientApp starts without UI freeze

Status: Pass

Command: dotnet run --project Code/ClientApp/ClientApp.csproj

Result: ClientApp window opened successfully; startup UI remained responsive with no visible freeze or startup exception.

Conclusion: Pass - ClientApp starts and remains responsive during startup.


`G1-04`  Invalid JSON fails gracefully without receiver crash

Status: Pass

Command: dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj

Result:
CLIENT OUT: "{" invalid json
TRACE IN tcp-0004: "{" invalid json
TRACE DISPATCH_ERROR tcp-0004: 'i' is an invalid start of a property name. Expected a '"'. LineNumber: 0 | BytePositionInLine: 2.

After the malformed JSON, the server still accepted and processed the next LOGIN request:

TRACE IN tcp-0005: {"payload":{"username":"client01","password":"123","role":"Client","machineId":"PC-01"},"type":"LOGIN","source":"PC-01","target":"server","requestId":"roundtrip-b0ef3f77f2bf4c50acfe6a739f79d768","timestamp":"2026-06-02T11:05:25.7758098Z"}
TRACE OUT tcp-0005: {"payload":{},"type":"LOGIN","source":"server","target":"PC-01","requestId":"roundtrip-b0ef3f77f2bf4c50acfe6a739f79d768","timestamp":"2026-06-02T11:05:25.7978102Z","success":false,"message":"Login rejected","error":{"code":"MACHINE_ALREADY_ACTIVE","details":"Machine is already active."}}

Conclusion: Pass - malformed JSON produced a controlled dispatch error and did not crash the receiver/listener; the server remained available for the next request.

`G1-05`  Unsupported packet type yields controlled behavior

Status: Pass

Command: dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj

Result:
TRACE DISPATCH_ERROR tcp-0009: The JSON value could not be converted to Shared.Enums.PacketType.
PASS: unknown packet type disconnects only the offending client and server remains available

TRACE DISPATCH_ERROR tcp-0011: Unsupported packet type: STATUS
PASS: packet type without an open route disconnects only the offending client and server remains available

Conclusion: Pass - unsupported packet types are handled in a controlled way without stopping the server listener.




