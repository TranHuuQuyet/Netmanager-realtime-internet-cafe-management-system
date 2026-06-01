# RUN GUIDE - RECOVERY TARGET

This guide documents the approved setup target for the recovery roadmap. Runtime steps remain `Blocked` until the corresponding test gate passes.

## Current Status - 2026-05-25

- A current implementation candidate restores the typed-auth server login path and a buildable client shell; an isolated build submission passes with `0` warnings and `0` errors.
- Server startup smoke now opens a responsive login dialog after correcting the UI resource manifest lookup; login result behavior is not yet gate-verified.
- The build evidence is submitted for M6 verification and does not by itself pass `G0`, because API/auth contract checks remain outstanding.
- Client startup is an explicit shell artifact and does not prove network/login integration.
- TCP listener/dispatcher valid round-trip evidence has been submitted through `R1-N01`; invalid/unsupported packet handling, login integration, status and control flow are not verified.
- Use `DOCS/TEST_MATRIX.md` for real pass/fail status.

## Required Environment

- Windows 10 or Windows 11.
- .NET 8 SDK.
- Visual Studio 2022 or terminal support for `net8.0-windows`.
- SQLite runtime via the selected `Microsoft.Data.Sqlite` ServerApp dependency.

## Approved Build Command

From repository root:

```powershell
dotnet build Code/NetManager.sln
```

This command must pass before a runtime demo or feature gate can be accepted.

## Recovery Runtime Defaults

| Setting | Recovery value | Status |
| --- | --- | --- |
| Local server host | `127.0.0.1` | Target until `G1` pass |
| Default port | `5000` | Target until `G1` pass |
| Framing | one UTF-8 JSON object per line | Approved contract target |
| Auth schema | `AuthUsers`, `AuthSessions` | Canonical recovery path |
| Database under approved root-run workflow | `internet_cafe.db` in repository root | Resolved by `AuthBootstrapper`; keep running from repository root so the canonical SQLite path stays unambiguous |
| Machine status storage | in-memory connection registry | Core architecture target |

Until a deterministic application data path is implemented and approved, run recovery commands from repository root so the relative SQLite database location is not ambiguous.

## Recovery Demo Accounts

| Role | Username | Password | MachineId |
| --- | --- | --- | --- |
| Admin | `admin` | `123` | `PC00` |
| Client | `client01` | `123` | `PC-01` |
| Client | `client02` | `123` | `PC-02` |

Rules:

- Admin uses `PC00` in the recovery baseline.
- A client account can use only its assigned `machineId`.
- A wrong machine login must fail visibly in the app after `G2` is implemented.
- These credentials are demo-only and must not be treated as production security.

## Core Local Demo Mode - Required

Expected flow after `G0-G4` pass:

1. Build the approved solution.
2. Start `ServerApp` from the approved build/run workflow.
3. Confirm server listens on `127.0.0.1:5000`.
4. Start client instance one and login as `client01` / `PC-01`.
5. Start client instance two and login as `client02` / `PC-02`.
6. Confirm server displays distinct online clients.
7. Lock and unlock one selected client and inspect ACK/result.
8. Disconnect a client and confirm the server remains running.

This is the release-critical demonstration path.

## Retained Extension Mode

Extension features remain in the project and may be added to rehearsal only when their test gate passes:

- direct notification after `G3`;
- timer display after notification is stable;
- chat only after timely `G4`;
- Real LAN smoke only after local rehearsal;
- broadcast, persistence and reconnect polish after prerequisite extensions.

Unfinished extension setup must be documented as continuation work, not silently omitted.

## Real LAN Smoke - Extension Only

When `E4` is opened:

1. Confirm local core rehearsal already passed.
2. Start server on the selected server machine.
3. Permit the approved TCP port through firewall.
4. Connect one client using the server LAN IP.
5. Run a limited smoke path and keep local multi-instance available as fallback.

Real LAN is retained as a valuable project capability, but it is not allowed to block the local core release.

## Reset And Evidence Rules

- M5 must document a verified SQLite reset/seed path during `G0/G2`.
- Do not delete or replace the tracked database without an approved reset procedure.
- M6 records build identity, runtime mode, test date and evidence for each pass.
- A screen opening without real network/auth interaction is not a completed demo step.
