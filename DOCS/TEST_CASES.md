# TEST CASES - NETMANAGER

Baseline date: `2026-06-08`

This document defines executable test cases for the current NetManager recovery scope. `DOCS/TEST_MATRIX.md` tracks gate status; this file defines the detailed case format that testers should execute and record.

## Priority Legend

| Priority | Meaning |
| --- | --- |
| `P1` | Blocker. Must pass before release/demo approval. |
| `P2` | High. Important behavior with limited workaround. |
| `P3` | Medium. Useful polish, extension, or non-blocking behavior. |

## Status Legend

| Status | Meaning |
| --- | --- |
| `Pass` | Executed and matched expected result. |
| `Fail` | Executed and did not match expected result. Must link a bug in `DOCS/BUGS.md`. |
| `Blocked` | Cannot execute because required implementation or environment is missing. |
| `Not Run` | Runnable but not yet executed in this verification round. |
| `Conditional` | Extension case, opened only after its prerequisite core gate passes. |

## Build And Contract

| ID | Priority | Description | Precondition | Input / Steps | Expected Result | Command / Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `TC-B01` | `P1` | Full solution builds. | .NET 8 SDK installed; run from repository root. | Build `Code/NetManager.sln`. | Build completes with `0` errors. Warnings must be reviewed before release. | `dotnet build Code/NetManager.sln` | `Not Run` |
| `TC-C01` | `P1` | Packet `type` serializes as API string. | Shared project builds. | Serialize a `LOGIN` packet. | JSON contains `"type":"LOGIN"`, not numeric enum value. | `dotnet run --project Code/ContractSmoke/ContractSmoke.csproj` | `Not Run` |
| `TC-C02` | `P1` | Numeric packet type is rejected. | Shared JSON options loaded. | Deserialize JSON with `"type":0`. | Deserialization fails; numeric packet type is not accepted. | `dotnet run --project Code/ContractSmoke/ContractSmoke.csproj` | `Not Run` |
| `TC-C03` | `P1` | `LOGIN` request parses as request payload. | Contract smoke runnable. | Deserialize `LOGIN` request with no `success` field. | Result is `Packet<LoginPayload>`. | `dotnet run --project Code/ContractSmoke/ContractSmoke.csproj` | `Not Run` |
| `TC-C04` | `P1` | `LOGIN` success parses as result payload. | Contract smoke runnable. | Deserialize `LOGIN` response with `success:true`. | Result is `Packet<LoginResultPayload>` with session/user/machine payload. | `dotnet run --project Code/ContractSmoke/ContractSmoke.csproj` | `Not Run` |
| `TC-C05` | `P1` | `LOGIN` failure uses top-level error envelope. | Contract smoke runnable. | Deserialize failed `LOGIN` response. | Result is `Packet<EmptyPayload>`, `success:false`, and `error.code` is populated. | `dotnet run --project Code/ContractSmoke/ContractSmoke.csproj` | `Not Run` |
| `TC-C06` | `P1` | Outgoing network message rejects embedded line breaks. | Shared `NetworkProtocol` available. | Call `ValidateOutgoingMessage` with text containing `\n` or `\r`. | Throws `ArgumentException`; one JSON object per line is preserved. | Unit/manual check | `Not Run` |

## Authentication And Database

| ID | Priority | Description | Precondition | Input / Steps | Expected Result | Command / Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `TC-A01` | `P1` | SQLite schema initializes. | Writable database path available. | Bootstrap database runtime. | `AuthUsers`, `AuthSessions`, and `Machines` exist. | `dotnet run --project Code/Auth_Test/Auth_Test.csproj` | `Not Run` |
| `TC-A02` | `P1` | Canonical seed exists. | Database initialized. | Read seed users and machines. | `admin/PC00`, `client01/PC-01`, `client02/PC-02` exist and are active. | `dotnet run --project Code/Auth_Test/Auth_Test.csproj` | `Not Run` |
| `TC-A03` | `P1` | Admin login succeeds. | DB seeded; `PC00` is not already online. | Username `admin`, password `123`, machineId `PC00`, role `Admin`. | Auth succeeds, returns active session, no error code. | `dotnet run --project Code/Auth_Test/Auth_Test.csproj` | `Not Run` |
| `TC-A04` | `P1` | Client login succeeds. | DB seeded; `PC-01` is not already online. | Username `client01`, password `123`, machineId `PC-01`, role `Client`. | Auth succeeds, returns active session for `PC-01`. | `dotnet run --project Code/Auth_Test/Auth_Test.csproj` | `Not Run` |
| `TC-A05` | `P1` | Wrong password is rejected. | DB seeded. | Username `client01`, password `wrong-password`, machineId `PC-01`, role `Client`. | Auth fails with `INVALID_CREDENTIALS`; no session is opened. | `dotnet run --project Code/Auth_Test/Auth_Test.csproj` | `Not Run` |
| `TC-A06` | `P1` | Correct credentials with wrong machine are rejected. | DB seeded. | Username `client01`, password `123`, machineId `PC-02`, role `Client`. | Auth fails with `ACCOUNT_MACHINE_MISMATCH`; no session is opened. | `dotnet run --project Code/Auth_Test/Auth_Test.csproj` | `Not Run` |
| `TC-A07a` | `P1` | Missing username is rejected. | Auth service initialized. | Empty username, valid password and machineId. | Auth fails with `INVALID_PACKET` mapped from invalid input. | Unit/manual auth check | `Not Run` |
| `TC-A07b` | `P1` | Missing password is rejected. | Auth service initialized. | Valid username and machineId, empty password. | Auth fails with `INVALID_PACKET` mapped from invalid input. | Unit/manual auth check | `Not Run` |
| `TC-A07c` | `P1` | Missing machineId is rejected. | Auth service initialized. | Valid username and password, empty machineId. | Auth fails with `INVALID_MACHINE_ID` or controlled invalid-packet result at dispatcher boundary. | Unit/manual auth check | `Not Run` |
| `TC-A08` | `P1` | Role mismatch is rejected. | DB seeded. | Login `client01` with required role `Admin`. | Auth fails; API error maps to `INVALID_CREDENTIALS`. | Unit/manual auth check | `Not Run` |
| `TC-A09` | `P1` | Unknown machine is rejected. | DB seeded. | Login valid user with machineId not present in `Machines`. | Auth fails with `INVALID_MACHINE_ID`. | Unit/manual auth check | `Not Run` |
| `TC-A10` | `P1` | Inactive account or machine is rejected. | DB contains an inactive account or inactive machine fixture. | Attempt login for inactive entity. | Auth fails with `ACCOUNT_DISABLED`; no session is opened. | Unit/manual fixture check | `Not Run` |
| `TC-A11` | `P1` | Successful login opens active session. | DB seeded and machine offline. | Perform successful login. | `AuthSessions` has active session for the user and machine status becomes `Online`. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |
| `TC-A12` | `P1` | Disconnect closes session. | Client is logged in and has active session. | Close TCP connection or dispose client. | Session state becomes `Closed`, machine status becomes `Offline`. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |
| `TC-A13` | `P1` | Duplicate active machine login is rejected. | `client01/PC-01` is already connected. | Attempt second login for `client01/PC-01`. | Second login fails with `MACHINE_ALREADY_ACTIVE`. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |
| `TC-A14` | `P1` | Login succeeds after previous session closes. | Duplicate-login case has closed first connection. | Login again as `client01/PC-01`. | Login succeeds and opens a new active session. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |

## Networking

| ID | Priority | Description | Precondition | Input / Steps | Expected Result | Command / Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `TC-N01` | `P1` | Server starts and listens locally. | Auth runtime bootstraps; local port is free. | Start `TcpJsonLineServer` on loopback. | Listener accepts connections and reports a local endpoint. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |
| `TC-N02` | `P1` | Client connects over TCP. | Server is running. | Open TCP connection to server endpoint. | Connection succeeds without UI or server freeze. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |
| `TC-N03` | `P1` | Valid JSON-line `LOGIN` round trip works. | Server is running; DB seeded. | Send valid `LOGIN` packet. | Response has matching `requestId`, `success:true`, and `LoginResultPayload`. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |
| `TC-N04` | `P1` | Invalid JSON fails gracefully. | Server is running. | Send malformed JSON line. | Offending client disconnects; server remains available for next valid login. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |
| `TC-N05` | `P1` | Unknown packet type fails gracefully. | Server is running. | Send packet with unknown `type`. | Offending client disconnects or receives controlled failure; server remains available. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |
| `TC-N06` | `P1` | Unsupported but known packet type is controlled. | Server is running. | Send inbound `STATUS` before route is implemented. | Current expected result: controlled dispatch error and client disconnect; server remains available. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |
| `TC-N07` | `P1` | Login emits `Online` status. | Server is running; client login succeeds. | Login `client01/PC-01`. | Server emits `STATUS` with `machineId=PC-01`, `status=Online`. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |
| `TC-N08` | `P1` | Disconnect emits `Offline` status. | Client is logged in. | Close client TCP connection. | Server emits `STATUS` with `machineId=PC-01`, `status=Offline`. | `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj` | `Not Run` |
| `TC-N09` | `P1` | Multiple clients remain distinct. | Server is running; `PC-01` and `PC-02` are offline. | Login `client01/PC-01` and `client02/PC-02`. | Each client has a different session and connection id; status of `PC-01` does not overwrite or affect `PC-02`. | Manual/integration multi-client smoke | `Blocked` |
| `TC-N10` | `P2` | Sequential sends are not interleaved. | Client is connected. | Send several JSON-line messages quickly. | Each message is received as one complete line; no merged or split JSON packet appears. | Unit/integration stress check | `Not Run` |
| `TC-N11` | `P1` | Abrupt client loss does not crash server. | Server is running; client is connected. | Kill/close client unexpectedly. | Server logs disconnect, closes session, remains able to accept a new login. | Manual/integration smoke | `Not Run` |
| `TC-N12` | `P2` | Client reconnect behavior is controlled. | Server and client connection are available; auto reconnect is enabled. | Drop connection, then restore server. | Client attempts reconnect according to `ReconnectDelay`; failures surface via `ReconnectFailed`. | Manual/integration smoke | `Not Run` |

## ClientApp UI

| ID | Priority | Description | Precondition | Input / Steps | Expected Result | Command / Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `TC-U01` | `P1` | ClientApp starts without UI freeze. | Windows desktop environment. | Start ClientApp. | `ConnectForm` opens and remains responsive. | `dotnet run --project Code/ClientApp/ClientApp.csproj` | `Not Run` |
| `TC-U02` | `P2` | Launch machine id is applied. | ClientApp startup works. | Start with `--machine-id=PC-02`. | Machine textbox displays `PC-02`. | Manual UI smoke | `Not Run` |
| `TC-U03a` | `P1` | Empty username validation is visible. | ClientApp open. | Leave username empty, submit login. | Validation message appears and focus returns to username. | Manual UI smoke | `Not Run` |
| `TC-U03b` | `P1` | Empty password validation is visible. | ClientApp open. | Leave password empty, submit login. | Validation message appears and focus returns to password. | Manual UI smoke | `Not Run` |
| `TC-U03c` | `P1` | Empty machineId validation is visible. | ClientApp open. | Leave machineId empty, submit login. | Validation message appears and focus returns to machineId. | Manual UI smoke | `Not Run` |
| `TC-U04` | `P1` | Server unavailable error is visible. | Server is not running. | Attempt valid client login. | UI shows TCP/server unavailable message; app remains responsive. | Manual UI smoke | `Not Run` |
| `TC-U05` | `P1` | Wrong password error is visible. | Server is running. | Login `client01/wrong-password/PC-01`. | UI shows invalid credential message; main form does not open. | Manual UI smoke | `Not Run` |
| `TC-U06` | `P1` | Wrong machine error is visible. | Server is running. | Login `client01/123/PC-02`. | UI shows machine mismatch message; main form does not open. | Manual UI smoke | `Not Run` |
| `TC-U07` | `P1` | Successful client login opens main form. | Server is running; `PC-01` offline. | Login `client01/123/PC-01`. | `ClientMainForm` opens with authenticated session data. | Manual UI smoke | `Not Run` |
| `TC-U08` | `P2` | Client main form displays session details. | Client login succeeds. | Inspect main form fields. | Username, machineId, short session id, host/port, and login time are visible. | Manual UI smoke | `Not Run` |
| `TC-U09` | `P3` | Used-time counter updates. | Client main form is open. | Wait at least two seconds. | Used-time field increases without UI freeze. | Manual UI smoke | `Not Run` |
| `TC-U10` | `P1` | Client logout/disconnect is controlled. | Client main form is open and server is running. | Click logout or close client form. | Client exits cleanly; server receives disconnect and emits offline status. | Manual UI + server trace | `Not Run` |
| `TC-U11` | `P1` | Lock screen cannot be user-closed. | `LockScreenForm` is displayed. | Attempt normal user close. | Form remains open unless unlocked by server path. | Manual/unit UI smoke | `Not Run` |
| `TC-U12` | `P1` | Server unlock closes lock screen. | `LockScreenForm` is displayed. | Call `UnlockFromServer()`. | Form closes cleanly. | Manual/unit UI smoke | `Not Run` |

## ServerApp UI

| ID | Priority | Description | Precondition | Input / Steps | Expected Result | Command / Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `TC-S01` | `P1` | ServerApp login form starts. | Windows desktop environment; DB can initialize. | Start ServerApp. | Login dialog opens and remains responsive. | `dotnet run --project Code/ServerApp/ServerApp.csproj` | `Not Run` |
| `TC-S02` | `P1` | Admin login opens dashboard. | DB seeded; `PC00` offline. | Login `admin/123/PC00`. | Login succeeds; `MainForm` opens. | Manual UI smoke | `Not Run` |
| `TC-S03` | `P1` | Wrong admin login is rejected. | ServerApp login form open. | Submit wrong admin credentials. | Error message appears; dashboard does not open. | Manual UI smoke | `Not Run` |
| `TC-S04` | `P1` | Port conflict is handled. | Another process occupies `127.0.0.1:5000`. | Login admin and let app start network server. | Warning appears; app does not crash. | Manual UI smoke | `Not Run` |
| `TC-S05` | `P1` | Dashboard shows client online. | ServerApp dashboard open; client login succeeds. | Login `client01/123/PC-01` from ClientApp. | Dashboard displays or updates `PC-01` with `ONLINE`. | Manual integration smoke | `Not Run` |
| `TC-S06` | `P1` | Dashboard shows client offline. | Dashboard shows an online client. | Disconnect that client. | Dashboard updates the same machine to `OFFLINE`. | Manual integration smoke | `Not Run` |
| `TC-S07` | `P1` | Status update is UI-thread safe. | Server emits status from background networking code. | Trigger Online/Offline status updates. | No `InvalidOperationException` or cross-thread UI crash occurs. | Manual integration smoke | `Not Run` |
| `TC-S08` | `P2` | Machine selection stays synchronized. | Dashboard has machine rows/cards. | Select a machine in grid and card view. | Selected row/card and selected-machine label point to the same machine. | Manual UI smoke | `Not Run` |
| `TC-S09` | `P2` | Runtime status replaces sample status. | Dashboard initially shows sample data. | Receive first real status event. | Sample list is cleared and runtime machine data is shown. | Manual integration smoke | `Not Run` |

## Core Control

These cases are required for full core demo/release, but are currently blocked because real `LOCK`, `UNLOCK`, and `ACK` routing is not implemented end to end.

| ID | Priority | Description | Precondition | Input / Steps | Expected Result | Command / Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `TC-CTRL01` | `P1` | Admin locks selected client. | Server dashboard open; target client authenticated and online. | Select `PC-01`, send lock command. | Only `PC-01` receives `LOCK`. | Future integration smoke | `Blocked` |
| `TC-CTRL02` | `P1` | Client displays lock screen after `LOCK`. | `TC-CTRL01` command reaches client. | Observe ClientApp. | Lock screen appears and normal close is blocked. | Future integration smoke | `Blocked` |
| `TC-CTRL03` | `P1` | Client returns ACK after lock. | Client receives `LOCK`. | Inspect server trace/result. | Client sends `ACK` with `ackFor=LOCK`, `status=Success`. | Future integration smoke | `Blocked` |
| `TC-CTRL04` | `P1` | Admin sees command result. | ACK routing implemented. | Send lock or unlock. | Server UI shows success/failure result for selected machine. | Future integration smoke | `Blocked` |
| `TC-CTRL05` | `P1` | Admin unlocks selected client. | Target client is locked. | Send unlock command. | Only target client receives `UNLOCK`. | Future integration smoke | `Blocked` |
| `TC-CTRL06` | `P1` | Client exits lock state after `UNLOCK`. | Client is locked. | Receive `UNLOCK`. | Lock screen closes and client returns to normal main form. | Future integration smoke | `Blocked` |
| `TC-CTRL07` | `P1` | Command does not affect wrong client. | Two clients are online. | Lock `PC-01`. | `PC-01` locks; `PC-02` remains unchanged. | Future multi-client smoke | `Blocked` |
| `TC-CTRL08` | `P1` | Offline command fails cleanly. | Target machine is offline. | Send lock/unlock command. | Server shows controlled error; no crash or stale success. | Future integration smoke | `Blocked` |
| `TC-CTRL09` | `P1` | Unauthorized command is rejected. | Non-admin client is connected. | Attempt to send command packet as client. | Server rejects with `UNAUTHORIZED_COMMAND` or controlled failure. | Future security smoke | `Blocked` |

## Multi-Client Stability

| ID | Priority | Description | Precondition | Input / Steps | Expected Result | Command / Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `TC-M01` | `P1` | Two client instances login. | Server dashboard open; both machines offline. | Login `client01/PC-01` and `client02/PC-02`. | Both clients remain connected with distinct sessions and machine ids. | Manual multi-instance smoke | `Blocked` |
| `TC-M02` | `P1` | Dashboard shows two distinct online machines. | `TC-M01` passed. | Inspect dashboard. | `PC-01` and `PC-02` both show `ONLINE`; no row/card overwrite. | Manual multi-instance smoke | `Blocked` |
| `TC-M03` | `P1` | Disconnecting one client does not affect the other. | Two clients online. | Disconnect `PC-01`. | `PC-01` becomes `OFFLINE`; `PC-02` remains `ONLINE`. | Manual multi-instance smoke | `Blocked` |
| `TC-M04` | `P1` | Duplicate active login behavior is deterministic. | One client is logged in. | Start second client with same user/machine. | Duplicate is rejected with `MACHINE_ALREADY_ACTIVE` or documented selected behavior. | Manual/integration smoke | `Not Run` |
| `TC-M05` | `P2` | Login after disconnect works in multi-client mode. | `PC-01` disconnected; `PC-02` remains online. | Login `PC-01` again. | `PC-01` becomes online again without disturbing `PC-02`. | Manual multi-instance smoke | `Blocked` |
| `TC-M06` | `P1` | Commands route only to selected machine. | Two clients online; command routing implemented. | Lock/unlock selected machine. | Only selected machine changes state; other machine is unaffected. | Future multi-client smoke | `Blocked` |

## Release Readiness

| ID | Priority | Description | Precondition | Input / Steps | Expected Result | Command / Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `TC-R01` | `P1` | Clean setup follows run guide. | Fresh clone or clean workspace; .NET 8 installed. | Follow `DOCS/RUN_GUIDE.md` from repository root. | Build and required smoke tests run without hidden setup steps. | Manual release rehearsal | `Not Run` |
| `TC-R02` | `P1` | Full core regression passes. | All P1 cases implemented. | Run build, contract, auth, network, UI, control, and multi-client cases. | No P1 failures remain. | Release checklist evidence | `Blocked` |
| `TC-R03` | `P1` | Local multi-instance demo passes twice. | Release candidate built. | Execute core demo flow twice. | Both rehearsals pass with the same expected behavior. | Manual release rehearsal | `Blocked` |
| `TC-R04` | `P1` | No open critical/high demo blocker remains. | Bugs reviewed. | Inspect `DOCS/BUGS.md`. | No unaccepted `Critical` or `High` blocker remains for core demo. | Bug review | `Not Run` |
| `TC-R05` | `P2` | Docs match current implementation. | Candidate behavior verified. | Review `API`, `RUN_GUIDE`, `TEST_MATRIX`, `DEMO_CHECKLIST`, and this file. | Docs do not claim unimplemented runtime behavior as pass. | Documentation review | `Not Run` |

## Retained Extension Cases

These cases do not block the core release unless their feature is opened for demo.

| ID | Priority | Description | Precondition | Input / Steps | Expected Result | Command / Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `TC-E01` | `P3` | Direct notification reaches selected client. | Core control passes. | Send notification to `PC-01`. | Only `PC-01` displays notification. | Future extension smoke | `Conditional` |
| `TC-E02` | `P3` | Timer update displays on client. | Notification/control route is stable. | Send timer payload. | Client displays remaining time accurately enough for demo. | Future extension smoke | `Conditional` |
| `TC-E03` | `P3` | Admin and client exchange direct chat. | Multi-client core passes. | Send chat messages both directions. | Messages appear only in the intended conversation. | Future extension smoke | `Conditional` |
| `TC-E04` | `P3` | Real LAN smoke connects one client. | Local core rehearsal passed. | Start server on one machine and connect client by LAN IP. | One client completes limited login/status path. | Manual LAN smoke | `Conditional` |
| `TC-E05` | `P3` | Broadcast does not hit unintended clients. | Notification route implemented. | Send direct and broadcast notifications. | Direct messages stay direct; broadcast scope matches selected policy. | Future extension smoke | `Conditional` |
| `TC-E06` | `P3` | Timer/session persistence behavior is documented. | Timer feature opened. | Restart or simulate persistence scenario. | Behavior matches documented release policy. | Future extension smoke | `Conditional` |

## Execution Notes

- Every `Fail` must reference or create an entry in `DOCS/BUGS.md`.
- Every `Pass` should record date, command, build identity, and evidence in the active verification report.
- `Blocked` is acceptable for unopened future work, but it must not be reported as delivered runtime behavior.
- P1 cases define the minimum release/demo acceptance path.
