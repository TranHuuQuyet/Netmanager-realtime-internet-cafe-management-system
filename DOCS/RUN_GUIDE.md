# RUN GUIDE

This guide defines how the team should build, run, and verify the project during development and demo preparation.

Current status:

- `Code/NetManager.sln` and project files exist
- `ServerApp` has a server UI shell
- `ClientApp` still needs real workflow screens
- TCP, auth/database, and end-to-end runtime implementation are not complete yet

## 1. Required Environment

- Windows 10 or Windows 11
- .NET 8 SDK
- Visual Studio 2022 or another editor that supports .NET 8
- SQLite runtime/package selected by Member 5 during implementation

## 2. Solution Shape

The runtime solution is under `Code/`.

```text
Code/
|-- NetManager.sln
|-- ServerApp/
|-- ClientApp/
`-- Shared/
```

Planned project types:

- `ServerApp`: Windows Forms app
- `ClientApp`: Windows Forms app
- `Shared`: class library

Required references:

- `ServerApp` references `Shared`
- `ClientApp` references `Shared`

## 3. Default Development Configuration

These defaults should be used unless Member 1 approves a change.

| Setting | Default |
| --- | --- |
| Server host for local mode | `127.0.0.1` |
| Server port | `5000` |
| Packet framing | one UTF-8 JSON object per line |
| Database location | `Code/ServerApp/AppData/netmanager.db` |
| Local log location | `Code/Logs/` if file logging is added |

## 4. Demo Seed Accounts

Member 5 owns the final seed implementation, but the demo account baseline should remain stable.

| Role | Username | Password | MachineId |
| --- | --- | --- | --- |
| Admin | `admin01` | `123456` | empty or `SERVER` |
| Client | `pc01` | `123456` | `PC-01` |
| Client | `pc02` | `123456` | `PC-02` |
| Client | `pc03` | `123456` | `PC-03` |

Rules:

- client login must validate `username`, `password`, and `machineId`
- login with the correct client account but the wrong `machineId` must fail
- seed passwords are only for demo and development

## 5. Local Multi-Instance Mode

Use this mode for daily development and fallback demo.

Expected flow:

1. Start `ServerApp`.
2. Confirm the server is listening on `127.0.0.1:5000`.
3. Start one `ClientApp` instance using `pc01` and `PC-01`.
4. Start another `ClientApp` instance using `pc02` and `PC-02`.
5. Confirm both clients appear separately on the server dashboard.

Local instance rule:

- each client instance must use a different `machineId`
- do not reuse the same account for multiple client instances unless testing duplicate-login behavior

## 6. Real LAN Mode

Use this mode for the final demo if the network environment is stable.

Expected flow:

1. Start `ServerApp` on the server machine.
2. Confirm firewall allows the selected TCP port.
3. Find the server machine LAN IP.
4. Start `ClientApp` on each client machine.
5. Connect each client to the server LAN IP and default port.
6. Login each client with its assigned account and `machineId`.

Real LAN risks to check:

- firewall blocking the server port
- client machines using the wrong server IP
- duplicate `machineId`
- unstable Wi-Fi or mixed network segments

## 7. Minimum Weekly Build Rule

At the end of every week, the team should be able to run a visible build.

Minimum checks:

- solution builds
- server starts
- client starts
- latest completed feature can be demonstrated
- failing checks are recorded in `DOCS/BUGS.md`

## 8. Reset Rules

Member 5 must document the final reset behavior when database implementation exists.

Until then, expected reset behavior is:

- stop server and clients
- remove generated local database only if the team agrees it is safe
- recreate seed data through the official setup path

Never remove source files, project files, or docs as part of a runtime reset.
