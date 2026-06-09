# RUN GUIDE - RECOVERY TARGET

This guide documents the approved setup target for the recovery roadmap. Runtime steps remain `Blocked` until the corresponding test gate passes.

## Current Status - 2026-05-25

- A current implementation candidate restores the typed-auth server login path and a buildable client shell; an isolated build submission passes with `0` warnings and `0` errors.
- Server startup smoke now opens a responsive login dialog after correcting the UI resource manifest lookup; login result behavior is not yet gate-verified.
- The canonical recovery auth seed is now verified in `internet_cafe.db`: `admin` / `123` / `PC00`, `client01` / `123` / `PC-01`, `client02` / `123` / `PC-02`.
- `G0` build/contract/auth baseline is verified pass.
- Client startup is an explicit shell artifact and does not prove network/login integration.
- `G1` network foundation is verified pass: the TCP listener/dispatcher handles valid login round-trips, malformed JSON and unsupported packet types without crashing the receiver.
- ClientApp UI login binding, status flow and control flow remain pending under `R2/G2` and later gates.
- Use `DOCS/TEST_MATRIX.md` for real pass/fail status.

## Scope Update - 2026-06-09

- Real LAN with two physical clients, 1-1 Admin/client chat, and SQLite-backed billing/session monitor are now required demo scope.
- Local multi-instance remains required as regression/fallback, but it no longer replaces the physical LAN path.
- Billing/session restore after ServerApp restart is required; reconnect UX polish beyond minimal resync remains retained work.

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
| Local server host | `127.0.0.1` | Verified local regression endpoint |
| Required LAN server host | selected server LAN IP or approved LAN-capable bind address | Required `G5` demo endpoint after `2026-06-09` scope update |
| Default port | `5000` | Verified local endpoint; required LAN port unless M1 approves a change |
| Framing | one UTF-8 JSON object per line | Approved contract target |
| Auth schema | `AuthUsers`, `AuthSessions` | Canonical recovery path |
| Billing schema target | `BillingSessions` with active/closed restore behavior | Required future implementation target |
| Database under approved root-run workflow | `internet_cafe.db` in repository root | Resolved by `AuthBootstrapper`; keep running from repository root so the canonical SQLite path stays unambiguous |
| Machine status storage | in-memory connection registry plus required reconnect/resync evidence | Core architecture target |
| Default billing rate | `10000` VND/hour | Required demo rate |

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

## Local Regression Demo Mode - Required

Expected local regression flow after `G0-G4` pass and before final `G5`:

1. Build the approved solution.
2. Start `ServerApp` from the approved build/run workflow.
3. Confirm server listens on `127.0.0.1:5000`.
4. Start client instance one and login as `client01` / `PC-01`.
5. Start client instance two and login as `client02` / `PC-02`.
6. Confirm server displays distinct online clients.
7. Lock and unlock one selected client and inspect ACK/result.
8. Disconnect a client and confirm the server remains running.

This remains a release-critical regression path.

## Required Real LAN Demo Mode

Expected flow for `G5` after the `2026-06-09` scope update:

1. Start ServerApp on the selected server machine from the approved build/run workflow.
2. Confirm the approved TCP port is reachable from the client machines; allow the port through firewall if needed.
3. Record the server LAN IP used for the test.
4. Start physical client one with `--machine-id PC-01 --server-host <server-lan-ip> --server-port 5000`.
5. Start physical client two with `--machine-id PC-02 --server-host <server-lan-ip> --server-port 5000`.
6. Login as `client01` / `PC-01` and `client02` / `PC-02`.
7. Confirm Admin Panel displays both clients distinctly and commands/chat route only to the selected client.
8. Keep the local multi-instance regression path available and verify it still passes.

## Required Billing Demo Mode

Expected billing behavior for `G5`:

1. Admin starts a timed rental on one machine using a short demo package of `5-10` minutes.
2. Admin starts an open-ended rental on another machine.
3. Client does not choose rental mode at login; Admin owns timed/free/extend decisions.
4. Timed rental displays countdown, warns at 5 minutes remaining, sends `LOCK` at expiry and does not force logout.
5. Open-ended rental displays running amount in Admin Panel; Client may show temporary time/amount.
6. Amount is computed with rounded-up minutes: `chargedMinutes = Math.Ceiling(elapsedSeconds / 60.0)` and `amount = Math.Ceiling(chargedMinutes * 10000 / 60.0)`.
7. The `61` second case must be demonstrated or tested as a 2-minute charge.

## Required Billing Restart/Restore Mode

Expected restart behavior for `G5`:

1. Start at least one active timed or open-ended billing session.
2. Stop ServerApp without closing the client app.
3. Client continues showing local temporary time/amount where implemented.
4. Restart ServerApp from the approved workflow.
5. Admin Panel restores active `BillingSessions` from SQLite and recalculates elapsed time/amount from the original start time.
6. Minimal reconnect/resync lets the running client sync timer/billing and receive extend/LOCK after server restart.

## Retained Extension Mode

Extension features remain in the project and may be added to rehearsal only when their test gate passes:

- direct notification after `G3`;
- notification broadcast after direct notification is stable;
- reconnect UX polish after required minimal reconnect/resync passes;
- customer CRUD, shutdown control, dashboard polish and reporting after their retained backlog gates.

Unfinished extension setup must be documented as continuation work, not silently omitted.

## Reset And Evidence Rules

- M5 must document a verified SQLite reset/seed path during `G0/G2`; the canonical reset is root `internet_cafe.db` with `AuthUsers/AuthSessions` and the three recovery accounts above.
- M5 must add a verified billing reset/restore path for `BillingSessions` during `G5`; do not clear active billing evidence without recording the reset.
- Do not delete or replace the tracked database without an approved reset procedure, even when the seed has already been verified.
- M6 records build identity, runtime mode, test date and evidence for each pass.
- A screen opening without real network/auth interaction is not a completed demo step.
