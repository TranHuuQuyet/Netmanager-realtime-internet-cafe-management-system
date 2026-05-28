# API - RECOVERY CONTRACT

## Contract Baseline

Version: `v0.2`
Decision date: `2026-05-25`
Verification status: `Blocked until G0 tests pass`

This contract is the required runtime target for the recovery roadmap. Existing code artifacts must be corrected and tested against it before integration is considered complete.

### Current freeze evidence (`R1-C02`, submitted `2026-05-27`)

- `dotnet run --project Code/ContractSmoke/ContractSmoke.csproj --no-restore` passes:
  - `G0-02` packet `type` serializes as string and rejects numeric enum input.
  - `G0-03` `LOGIN` request/response paths deserialize into distinct payload models.
  - `G0-03` `LOGIN` request keeps response-only envelope fields (`success`, `message`, `error`) unset.
  - `G0-04` `LOGIN` failure uses top-level `success: false` and `error.code`.
- `R1-C02` remains pending M6 verification and M1 approval before gate acceptance.

## Scope And Scheduling

### Core packet types required for delivery

- `LOGIN`
- `STATUS`
- `LOCK`
- `UNLOCK`
- `ACK`

### Retained extension packet types

- `NOTIFICATION` - open after `G3`
- `TIMER` - open after notification and stable core
- `CHAT` - open only after timely `G4`

Extension packet types remain part of the product contract; scheduling gates prevent them from blocking core delivery.

## Transport Rules

- Transport is TCP.
- One packet is one UTF-8 JSON object followed by a line terminator.
- `type` is a JSON string enum, for example `"LOGIN"`.
- Field names are case-sensitive.
- Unknown fields are ignored where safe for forward compatibility.
- Invalid JSON and unsupported packet types must produce a controlled error or disconnect result, never crash a receiver.

## Packet Envelope

All request and response packets use this envelope:

```json
{
  "type": "LOGIN",
  "source": "client01",
  "target": "server",
  "requestId": "req-0001",
  "timestamp": "2026-05-25T10:00:00Z",
  "success": null,
  "message": null,
  "error": null,
  "payload": {}
}
```

Rules:

- `type`, `source`, `target`, `timestamp` and `payload` are required.
- `requestId` is required for `LOGIN`, command and `ACK` matching in the recovery implementation.
- Requests set `success`, `message` and `error` to `null` or omit nullable values during serialization.
- Responses set `success` to `true` or `false`.
- Failure responses put the machine-readable error in top-level `error.code`; UI must not depend on a competing payload-specific error shape.
- Server uses `source: "server"`; a logged-in client uses its account or machine identifier consistently as agreed in the dispatcher.

Successful response example:

```json
{
  "type": "LOGIN",
  "source": "server",
  "target": "client01",
  "requestId": "req-0001",
  "timestamp": "2026-05-25T10:00:01Z",
  "success": true,
  "message": "Login accepted",
  "payload": {
    "sessionId": "session-id",
    "username": "client01",
    "role": "Client",
    "machineId": "PC-01"
  }
}
```

Failed response example:

```json
{
  "type": "LOGIN",
  "source": "server",
  "target": "client01",
  "requestId": "req-0001",
  "timestamp": "2026-05-25T10:00:01Z",
  "success": false,
  "message": "Login rejected",
  "error": {
    "code": "ACCOUNT_MACHINE_MISMATCH",
    "details": "Account is not assigned to the requested machine"
  },
  "payload": {}
}
```

## Error Codes

| Code | Meaning |
| --- | --- |
| `INVALID_CREDENTIALS` | Username or password invalid |
| `INVALID_MACHINE_ID` | Required machine ID missing or invalid |
| `ACCOUNT_MACHINE_MISMATCH` | Account is bound to another machine |
| `ACCOUNT_DISABLED` | User is disabled |
| `MACHINE_ALREADY_ACTIVE` | Duplicate active login is rejected, if selected at `G4` |
| `INVALID_PACKET` | JSON/envelope/payload cannot be accepted |
| `UNSUPPORTED_PACKET` | Packet type is unknown or not open for routing |
| `UNAUTHORIZED_COMMAND` | Session/machine cannot receive the command |
| `SERVER_ERROR` | Controlled unexpected server failure |

## Identity And Authentication

Recovery runtime seed baseline:

| Role | Username | Password | MachineId |
| --- | --- | --- | --- |
| Admin | `admin` | `123` | `PC00` |
| Client | `client01` | `123` | `PC-01` |
| Client | `client02` | `123` | `PC-02` |

Rules:

- Recovery admin login requires `machineId: "PC00"` to match the selected canonical auth implementation.
- Client login must validate `username`, `password` and bound `machineId`.
- Correct client credentials with wrong `machineId` return failure with `ACCOUNT_MACHINE_MISMATCH` or `INVALID_MACHINE_ID`.
- Duplicate active login behavior must be fixed and documented during `G4`; until then it is not assumed.

## Core Payloads

### `LOGIN` request

```json
{
  "username": "client01",
  "password": "123",
  "role": "Client",
  "machineId": "PC-01"
}
```

### `LOGIN` success response payload

```json
{
  "sessionId": "session-id",
  "username": "client01",
  "role": "Client",
  "machineId": "PC-01"
}
```

### `STATUS`

Sent after authenticated login and on visible state/disconnect-related updates.

```json
{
  "machineId": "PC-01",
  "machineName": "Computer 01",
  "status": "Online",
  "ipAddress": "127.0.0.1",
  "lastSeen": "2026-05-25T10:00:02Z"
}
```

### `LOCK` / `UNLOCK`

```json
{
  "machineId": "PC-01",
  "issuedBy": "admin",
  "reason": "manual_control"
}
```

### `ACK`

```json
{
  "machineId": "PC-01",
  "ackFor": "LOCK",
  "status": "Success",
  "message": "Lock applied"
}
```

Accepted `ACK.status`: `Success`, `Failed`, `Ignored`.

## Retained Extension Payloads

### `NOTIFICATION`

```json
{
  "message": "Server restart after 10 minutes",
  "severity": "Info",
  "scope": "Direct"
}
```

Direct message is `E1`; broadcast scope is `E5`.

### `TIMER`

```json
{
  "machineId": "PC-01",
  "remainingSeconds": 1800,
  "startedAt": "2026-05-25T09:30:00Z",
  "expiresAt": "2026-05-25T10:00:00Z"
}
```

Display is `E2`; persistence is `E6`.

### `CHAT`

```json
{
  "sender": "PC-01",
  "receiver": "admin",
  "message": "May em bi lag"
}
```

Chat remains direct 1-1 text only: no history, group, file/image, delivery queue or emoji-specific behavior.

## Data And Session Baseline

- Canonical recovery SQLite tables are `AuthUsers` and `AuthSessions`, as owned by M5's selected auth runtime.
- Canonical recovery runtime is `AuthBootstrapper` backed by `internet_cafe.db` in the repository root; this is the only approved SQLite path for the recovery baseline.
- Broader `Users/Machines/Sessions` consolidation is retained future work and must not be integrated in parallel before core release.
- Online/offline and command target state are maintained by an in-memory authenticated connection registry for core delivery.
- Timer persistence is an extension, not a core database requirement.

## Runtime Boundary

- M2 owns packet serialization, TCP framing, dispatcher and routing.
- M5 owns authentication/session/database behavior returned to the dispatcher.
- M3/M4 consume typed services/events and never parse wire JSON in forms.
- M6 verifies JSON samples, auth error outcomes and feature gates.

## Change And Verification Rule

- Any field/packet/error change updates this file and its tests in the same review batch.
- `G0` cannot pass until serialization produces string packet types, LOGIN request/response are distinguishable, and top-level error handling matches this contract.
- Retained extension models remain documented even while their routing is gated closed.
