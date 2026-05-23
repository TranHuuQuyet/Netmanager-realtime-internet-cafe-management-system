# NetManager

NetManager is a .NET 8 Windows Forms solution for a LAN cafe management demo. The repository currently focuses on a contract-first server/client structure, a polished server-side UI shell, and a clear path toward TCP-based realtime features. The implemented code is still a scaffold, but it now builds cleanly and reflects the actual solution layout instead of a documentation-only starter.

## Project Overview

The solution is split into three projects:

- `ServerApp`: the admin/server WinForms app
- `ClientApp`: the client WinForms app
- `Shared`: the shared class library reserved for DTOs, enums, packet helpers, and other contract-level code

The architectural goal is to keep the UI, transport contract, and shared models separated so the server and client do not drift into incompatible packet shapes or duplicated logic.

## Goals

- Provide a stable educational base for LAN management workflows
- Keep the networking contract explicit before implementation expands
- Support both real LAN demos and local multi-instance development
- Keep the server UI maintainable and easy to extend
- Preserve a clean boundary between UI, transport, and shared contract code

## Current Status

What is implemented today:

- The solution builds successfully on .NET 8
- `ServerApp` opens with a login dialog before the main dashboard
- Login validation checks that `username`, `password`, and `machineId` are not empty
- The server dashboard loads sample machines and sample customer rows
- Machine selection is synchronized between the cards, table, and chat panel
- Server UI text is centralized in `ServerApp/Resources/UiStrings.resx`
- Machine icons are rendered with custom GDI+ drawing instead of image assets
- Action buttons and chat input provide placeholder feedback instead of backend actions

What is not implemented yet:

- TCP transport
- Login/auth backend
- Database-backed persistence
- Real machine control commands
- Real chat delivery
- Client-side runtime behavior beyond the default WinForms shell

## Features

### Implemented

- `ServerApp` login form with field validation and modal flow
- `MainForm` dashboard with two tabs: machines and customers
- Sample machine cards with status coloring
- `DataGridView`-based machine and customer views
- Local selection state handling for the selected machine
- Resource-backed UI strings for server-facing labels and messages

### Planned

- TCP client/server communication
- Shared packet and DTO models
- Account-to-`machineId` validation
- `LOCK` / `UNLOCK` command handling with `ACK`
- `STATUS` heartbeat or state sync
- Minimal 1-1 chat delivery
- SQLite-backed auth/session/customer storage

## Architecture

The repo uses a simple separation of concerns:

- `ServerApp` owns the server-facing Windows Forms UI
- `ClientApp` owns the client-facing Windows Forms UI
- `Shared` is reserved for the contract layer so both apps can compile against the same packet and model definitions later
- `DOCS/API.md` is the protocol source of truth while the runtime is still being assembled

Why this shape exists:

- Separate executables make the deployment story clearer for a LAN demo
- A shared library prevents packet and DTO drift once transport is introduced
- UI resources keep form code focused on layout and behavior instead of hard-coded text
- The docs-first workflow lets the team freeze contract decisions before deeper implementation

## Folder Structure

```text
Code/
|-- NetManager.sln
|-- global.json
|-- ServerApp/
|   |-- Forms/
|   `-- Resources/
|-- ClientApp/
`-- Shared/
DOCS/
|-- API.md
|-- BUGS.md
|-- DECISIONS.md
|-- DEMO_CHECKLIST.md
|-- LEADER_FLOW.md
|-- README.md
|-- RUN_GUIDE.md
|-- TASKS.md
|-- TEST_MATRIX.md
`-- members/
```

## Tech Stack

- .NET 8
- C#
- Windows Forms
- TCP is the intended transport layer
- SQLite is the planned local persistence layer
- `System.Text.Json` is the documented serialization format

The SDK is pinned in [`Code/global.json`](Code/global.json) to `8.0.421`.

## Networking Flow

The target runtime flow is documented in [`DOCS/API.md`](DOCS/API.md):

1. Client and server connect over TCP.
2. Packets are encoded as UTF-8 JSON.
3. One JSON object is sent per line.
4. `LOGIN` validates `username`, `password`, and `machineId`.
5. `STATUS` carries heartbeat or state changes.
6. `LOCK` and `UNLOCK` are server-issued commands.
7. The client responds with `ACK` after command execution.
8. `CHAT` remains direct 1-1 text only in the MVP.

Important current-state note:

- The protocol is defined, but the runtime transport is not wired into the apps yet.

## Environment Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- Visual Studio 2022 or another editor that supports WinForms on .NET 8
- A Windows environment is required because both apps target `net8.0-windows`

## Setup & Installation

From the repository root:

1. Make sure the .NET 8 SDK is installed.
2. Open `Code/NetManager.sln` in Visual Studio, or use the command line.
3. Restore and build the solution:

```powershell
dotnet build Code/NetManager.sln
```

4. If you want to run from the terminal, start the apps separately:

```powershell
dotnet run --project Code/ServerApp/ServerApp.csproj
dotnet run --project Code/ClientApp/ClientApp.csproj
```

## Run Instructions

### ServerApp

- Launch `ServerApp`
- The app opens the login dialog first
- Enter any non-empty `username`, `password`, and `machineId` to reach the dashboard
- The current login flow is UI validation only; it does not contact a backend yet

### ClientApp

- Launch `ClientApp`
- The current client is still the default WinForms shell (`Form1`)
- It is present so the solution structure and build path stay complete while client behavior is developed

## Configuration

There is no runtime configuration file yet.

Current configuration sources are:

- hard-coded sample data in `ServerApp`
- UI strings in `ServerApp/Resources/UiStrings.resx`
- documented defaults in `DOCS/RUN_GUIDE.md` and `DOCS/API.md`

Documented runtime assumptions:

- local host: `127.0.0.1`
- default port: `5000`
- each client account should be bound to one `machineId`
- local multi-instance mode and real LAN mode must both remain supported

## API / Protocol

The protocol baseline is documented in [`DOCS/API.md`](DOCS/API.md). It currently defines these packet types:

- `LOGIN`
- `STATUS`
- `LOCK`
- `UNLOCK`
- `ACK`
- `NOTIFICATION`
- `TIMER`
- `CHAT`

Contract rules worth keeping stable:

- Fields are case-sensitive
- Unknown fields should be ignored for forward compatibility
- Invalid packets should fail gracefully
- Chat stays minimal and text-only for the MVP
- Server and client must use the same schema and error codes

## UI Preview

### Server login

- Modal login dialog
- Fields: `username`, `password`, `machineId`
- `Enter` submits and `Esc` closes the dialog
- Inline validation message for missing fields

### Server dashboard

- Machines tab
  - sample machine cards
  - action buttons for lock, unlock, and shutdown
  - split view with a machine table and a chat panel
- Customers tab
  - customer detail fields
  - login/account detail fields
  - customer table
  - add, edit, delete, and cancel buttons

### Client app

- Still the default WinForms shell
- No production client workflow is wired yet

## Development Workflow

- Keep all runtime code under `Code/`
- Treat `DOCS/API.md` as the contract source of truth
- Use `DOCS/LEADER_FLOW.md` as the master 8-week working flow
- Use `DOCS/TASKS.md` as the only checkbox tracker
- Update `DOCS/BUGS.md` and `DOCS/RUN_GUIDE.md` when behavior changes
- Keep server-visible UI text in `UiStrings.resx` rather than hard-coding it in forms
- Prefer service boundaries over packet parsing inside WinForms event handlers
- Verify the solution with `dotnet build Code/NetManager.sln`

## Contribution Guide

- Use the existing branch naming convention from the docs:
  - `member-<number>/<short-task>`
  - `integration/<flow-name>`
  - `hotfix/<short-fix>`
- Use commit messages in the form `<scope>: <short action>`
- Keep packet changes documented in the same session
- Do not let `ServerApp` and `ClientApp` define different interpretations of the same packet
- Keep form code focused on presentation and event handling
- If protocol or shared model changes are needed, update the docs before or alongside the code

## Known Issues

- `Shared/Class1.cs` is still a placeholder
- `ClientApp` is still a default `Form1` shell
- `ServerApp` actions for lock, unlock, shutdown, chat, and customer CRUD are placeholder handlers
- Machine and customer data are still hard-coded sample rows
- There is no live TCP transport yet
- There is no auth backend yet
- There is no database persistence yet

## Future Improvements

- Add packet and DTO types to `Shared`
- Implement TCP connect, send, receive, and reconnect handling
- Wire login to auth and session storage
- Replace sample data with SQLite-backed persistence
- Implement command handling for lock, unlock, notification, timer, and chat
- Add client-side state reaction and `ACK` responses
- Add LAN smoke testing and local multi-instance validation
- If file transfer remains in scope later, add it as a documented packet and implement it after the current MVP contract is stable

## Roadmap

The docs organize delivery around a fixed 8-week baseline:

- Week 1: `W1.P1` kickoff/ownership, `W1.P2` API contract, `W1.P3` scaffold/first round trip
- Week 2: `W2.P1` network core, `W2.P2` auth/DB core, `W2.P3` UI skeleton completion
- Week 3: `W3.P1` login integration, `W3.P2` status sync, `W3.P3` one-client core demo
- Week 4: `W4.P1` lock/unlock, `W4.P2` ACK/error handling, `W4.P3` control regression
- Week 5: `W5.P1` notification, `W5.P2` timer, `W5.P3` 1-1 chat
- Week 6: `W6.P1` multi-client, `W6.P2` stability hardening, `W6.P3` demo mode validation
- Week 7: `W7.P1` regression/bug triage, `W7.P2` source/docs cleanup, `W7.P3` release candidate
- Week 8: `W8.P1` final rehearsal, `W8.P2` release lock, `W8.P3` demo/archive

See [`DOCS/TASKS.md`](DOCS/TASKS.md) for the current execution tracker.

## License

No license file is committed yet. Until one is added, the repository should be treated as all rights reserved by default.
