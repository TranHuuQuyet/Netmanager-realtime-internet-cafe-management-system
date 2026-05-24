# API

## Contract Baseline

Version: `v0.1`

This document is the shared contract reference for `W1.P2 API Contract`.
Server, client, and shared models must follow this file until the team formally changes the contract.

## Scope

Core packet types in this baseline:

- `LOGIN`
- `STATUS`
- `LOCK`
- `UNLOCK`
- `ACK`
- `NOTIFICATION`
- `TIMER`
- `CHAT`

## Confirmed Project Rules

- The project targets `.NET 8`, C#, Windows Forms, TCP, SQLite, and `System.Text.Json`.
- Each client account is mapped to one `machineId`.
- The server must validate `username`, `password`, and `machineId` on login.
- A correct account with the wrong `machineId` must be rejected.
- Chat is minimal 1-1 text only.
- Chat has no emoji, no history, no file/image, and no group room behavior.
- The project must work in both real LAN mode and local multi-instance mode.

## Transport Rules

- Transport is TCP.
- Payloads are UTF-8 JSON.
- One packet is one JSON object per line.
- Fields are case-sensitive.
- Unknown fields should be ignored for forward compatibility.
- Invalid packets must fail gracefully instead of crashing the app.

## Packet Envelope

All packets use the same envelope shape.

```json
{
  "type": "LOGIN",
  "source": "client01",
  "target": "server",
  "requestId": "req-0001",
  "timestamp": "2026-05-13T10:00:00Z",
  "success": true,
  "message": "optional human readable note",
  "error": {
    "code": "optional_error_code",
    "details": "optional error details"
  },
  "payload": {}
}
```

### Envelope Rules

- `type` is required.
- `source` and `target` help routing and debugging.
- `requestId` is optional but recommended for request/response matching.
- `success` is used on responses.
- `error` is only present when a request fails.
- `payload` contains the packet-specific body.
- `timestamp` is recommended on all requests and responses.
- Clients should use the account name or `machineId` as `source`.
- The server should use a stable server identifier such as `server`.

## Common Response Pattern

Successful response:

```json
{
  "type": "LOGIN",
  "source": "server",
  "target": "client01",
  "requestId": "req-0001",
  "success": true,
  "message": "Login accepted",
  "payload": {}
}
```

Failed response:

```json
{
  "type": "LOGIN",
  "source": "server",
  "target": "client01",
  "requestId": "req-0001",
  "success": false,
  "message": "Login rejected",
  "error": {
    "code": "INVALID_CREDENTIALS",
    "details": "Username or password is not valid"
  },
  "payload": {}
}
```

### Minimum Error Codes

The baseline error codes should include:

- `INVALID_CREDENTIALS`
- `INVALID_MACHINE_ID`
- `ACCOUNT_MACHINE_MISMATCH`
- `ACCOUNT_DISABLED`
- `MACHINE_ALREADY_ACTIVE`
- `INVALID_PACKET`
- `UNSUPPORTED_PACKET`
- `SERVER_ERROR`

## Packet Payloads

### `LOGIN`

Used by admin and client login flows.
For client login, `machineId` is mandatory and must match the assigned account.

```json
{
  "username": "client01",
  "password": "123456",
  "role": "Client",
  "machineId": "PC-01"
}
```

Expected login behavior:

- admin login may not require a bound client machine
- client login must validate the bound `machineId`
- wrong `machineId` must return `success: false` and an error code such as `INVALID_MACHINE_ID` or `ACCOUNT_MACHINE_MISMATCH`

### `STATUS`

Used for machine state updates and heartbeat-style sync.
Clients should send it on a regular interval, and also when local state changes.

```json
{
  "machineId": "PC-01",
  "machineName": "Computer 01",
  "status": "Online",
  "ipAddress": "192.168.1.10",
  "lastSeen": "2026-05-13T10:00:00Z"
}
```

### `LOCK`

Used to request or notify a lock action.

```json
{
  "machineId": "PC-01",
  "issuedBy": "admin01",
  "reason": "time_expired"
}
```

### `UNLOCK`

Used to request or notify an unlock action.

```json
{
  "machineId": "PC-01",
  "issuedBy": "admin01",
  "reason": "manual_unlock"
}
```

### `ACK`

Used by the client to confirm that a server command was received and executed.
The client should return `ACK` after `LOCK`, `UNLOCK`, and other command-style packets when execution result matters.

```json
{
  "machineId": "PC-01",
  "ackFor": "LOCK",
  "status": "Success",
  "message": "Lock applied"
}
```

Recommended `ACK.status` values:

- `Success`
- `Failed`
- `Ignored`

### `NOTIFICATION`

Used to send a simple message to one machine or to all machines.

```json
{
  "message": "Server restart after 10 minutes",
  "severity": "Info",
  "scope": "Broadcast"
}
```

### `TIMER`

Used for session timer updates.

```json
{
  "machineId": "PC-01",
  "remainingSeconds": 1800,
  "startedAt": "2026-05-13T09:30:00Z",
  "expiresAt": "2026-05-13T10:00:00Z"
}
```

### `CHAT`

Used for basic 1-1 text chat.
This packet is intentionally minimal for the MVP.

```json
{
  "sender": "PC-01",
  "receiver": "admin01",
  "message": "May em bi lag"
}
```

Chat scope rules:

- only direct 1-1 text
- no room field is required in the MVP
- no history loading
- no emoji-specific handling
- no file or image payload
- no delivery queue after reconnect

## Shared Models

Recommended shared types for the `Shared` project:

- `PacketType`
- `AuthResult`
- `MachineStatus`
- `CommandType`
- `NotificationMessage`
- `AckInfo`
- `ChatMessage`
- `TimerInfo`
- `SessionInfo`
- `ErrorInfo`

## Database Summary

### `Users`

Suggested fields:

- `Id`
- `Username`
- `PasswordHash`
- `Role`
- `MachineId`
- `IsActive`
- `LastLogin`

Rules:

- client accounts should be bound to one `MachineId`
- the bound machine must be validated during login

### `Sessions`

Suggested fields:

- `Id`
- `UserId`
- `MachineId`
- `Status`
- `StartTime`
- `EndTime`

### `Machines`

Suggested fields:

- `Id`
- `MachineName`
- `IpAddress`
- `Status`
- `LastSeen`

## Change Rule

- If a field name, packet shape, or response rule changes, update this file in the same session.
- Do not let server and client drift into different packet schemas.
- Do not widen chat scope or machine identity rules without updating `LEADER_FLOW.md` and `TASKS.md`.
