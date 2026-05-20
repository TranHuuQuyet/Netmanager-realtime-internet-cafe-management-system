# API

## Socket Events

Planned packet/event names:

- `LOGIN`
- `LOGOUT`
- `STATUS`
- `LOCK`
- `UNLOCK`
- `NOTIFICATION`
- `TIMER`
- `CHAT`

## Request/Response Formats

### Generic Packet Shape

```json
{
  "type": "LOGIN",
  "source": "client01",
  "target": "server",
  "payload": {}
}
```

### Login Packet

```json
{
  "type": "LOGIN",
  "username": "client01",
  "password": "123456"
}
```

### Chat Packet

```json
{
  "type": "CHAT",
  "sender": "PC01",
  "message": "May em bi lag"
}
```

### Lock Packet

```json
{
  "type": "LOCK"
}
```

### Notification Packet

```json
{
  "type": "NOTIFICATION",
  "message": "Server restart after 10 minutes"
}
```

## API Contracts

- every packet must include a `type`
- shared field names must stay stable across server and client
- invalid packets should fail gracefully
- packet handlers should route by `type`
- transport is expected to be TCP with JSON payloads

## How Packet Items Are Read And Used

Think of a packet as a small message object. Each item inside the packet has a job:

- `type`: tells the handler which service should process the packet
- `source`: tells where the packet came from
- `target`: tells who should receive or process it
- `payload` or direct fields like `username`, `message`: contains the business data used by services

### Typical Flow

1. Network layer receives raw JSON from TCP.
2. JSON is parsed into a shared `Packet` model.
3. Dispatcher reads `packet.type`.
4. Dispatcher selects the correct service.
5. Service reads the needed items from the packet.
6. Service updates application state, database, or UI.

### Example 1: Login Packet 

Incoming packet:

```json
{
  "type": "LOGIN",
  "username": "client01",
  "password": "123456"
}
```

How services use it:

- handler reads `type = LOGIN`
- auth service reads `username`
- auth service reads `password`
- auth service validates credentials
- server returns success/failure result packet

Pseudo flow:

```text
packet.type -> LoginHandler
packet.username -> AuthService
packet.password -> AuthService
AuthService -> check user -> return AuthResult
```

### Example 2: Chat Packet

Incoming packet:

```json
{
  "type": "CHAT",
  "sender": "PC01",
  "message": "May em bi lag"
}
```

How services use it:

- handler reads `type = CHAT`
- chat service reads `sender`
- chat service reads `message`
- server can store it, display it, or forward it to admin UI

Pseudo flow:

```text
packet.type -> ChatHandler
packet.sender -> ChatService
packet.message -> ChatService
ChatService -> save/broadcast/display
```

### Example 3: Lock Packet

Incoming packet:

```json
{
  "type": "LOCK"
}
```

How services use it:

- handler reads `type = LOCK`
- client control service does not need extra fields
- client UI service shows lock screen

Pseudo flow:

```text
packet.type -> ControlHandler
ControlHandler -> ClientControlService.LockMachine()
```

### Recommended Service Mapping

- `LOGIN` -> `AuthService`
- `LOGOUT` -> `SessionService`
- `STATUS` -> `MachineStatusService`
- `LOCK` -> `ClientControlService`
- `UNLOCK` -> `ClientControlService`
- `NOTIFICATION` -> `NotificationService`
- `TIMER` -> `SessionTimerService`
- `CHAT` -> `ChatService`

### Dispatcher Idea

The dispatcher should only route packets. Business logic should stay inside services.

```text
if packet.type == LOGIN -> AuthService
if packet.type == STATUS -> MachineStatusService
if packet.type == CHAT -> ChatService
if packet.type == LOCK -> ClientControlService
```

### Important Rule

Services should only read the fields they need for that packet type. For example:

- `AuthService` should care about `username` and `password`
- `ChatService` should care about `sender` and `message`
- `ClientControlService` should care about `type` and target machine/session

This keeps packet parsing simple and service responsibilities clear.

## Packet Structures

### Suggested Shared Models

- `PacketType`
- `AuthResult`
- `MachineStatus`
- `CommandType`
- `NotificationMessage`
- `ChatMessage`
- `TimerInfo`
- `SessionInfo`

## Database Schema Summaries

### Users

Suggested fields:

- `Id`
- `Username`
- `PasswordHash`
- `Role`
- `MachineId`
- `IsActive`
- `LastLogin`

### Sessions

Suggested fields:

- `Id`
- `UserId`
- `MachineId`
- `Status`
- `StartTime`
- `EndTime`

### Machines

Suggested fields:

- `Id`
- `MachineName`
- `IpAddress`
- `Status`
- `LastSeen`

## Notes

- This API document is the contract reference for future implementation work.
- If implementation changes the schema, update this file immediately to avoid server/client drift.
