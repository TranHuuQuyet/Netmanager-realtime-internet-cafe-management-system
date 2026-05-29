# BUGS AND RISKS - RECOVERY BASELINE

Baseline date: `2026-05-25`
Delivery deadline: `2026-07-05`
Primary mode: `Local Multi-Instance Core Demo`

## Severity Rule

- `Critical`: prevents build, connection, login or required core demo.
- `High`: threatens a core gate or makes demo result unreliable.
- `Medium`: extension/demo usability issue with a workable core path.
- `Low`: cosmetic or non-blocking.

## Active Blockers

| ID | Severity | Title | Evidence | Owner | Due / gate | Status |
| --- | --- | --- | --- | --- | --- | --- |
| `B-001` | Critical | Full solution build/server UI startup was broken by missing UI implementations and resource drift | Baseline build failed because `LoginForm.cs` was missing; follow-up also found missing `ConnectForm`. After restoring typed-auth login/client shell, server startup exposed an incorrect resource base name (`ServerApp.Resources.UiStrings` vs manifest `ServerApp.UiStrings`). Candidate fix now builds with `0` warnings/errors and opens a responsive `Dang nhap` dialog in a local startup smoke. | M3 + M5 + M4, M1 approves; M6 verifies | `R1/G0` | Verified PASS (M6 - 2026-05-29) |
| `B-002` | Critical | No verified TCP listener/dispatcher round-trip | `R1-N01` candidate adds `TcpJsonLineServer` + `PacketDispatcher`; `dotnet run --project Code/NetworkSmokeTest/NetworkSmoke.csproj --no-restore` starts a ServerApp listener, accepts one local client, traces `LOGIN`, and returns typed `ACK` with matching `requestId` and `status: "Success"`. Invalid/unsupported packet handling is still pending under `R1-N02`. | M2 | `R1/G1` | PARITIALLY VERIFIED (M6 - 2026-05-29) |
| `B-003` | Critical | No integrated login/status/control demo | No runtime proof for UI -> TCP -> auth or command/ACK flow | M2 + M3 + M4 + M5 | `R2-R3` | Open |
| `B-004` | High | Shared wire implementation does not yet prove API `v0.2` | Existing serializer/parser needs string enum and LOGIN response/error verification | M2 + M1 | `R1/G0` | Verified PASS (M6 - 2026-05-29) |
| `B-005` | High | Competing auth/database directions | `AuthUsers/AuthSessions` runtime path coexists with broader `Users/Machines/Sessions` draft | M5 + M1 | `R1/G0` |  PARTIALLY VERIFIED (M6 - 2026-05-29) |
| `B-006` | High | Seed/admin machine docs previously drifted from selected auth runtime | Runtime path uses `admin`/`PC00`/`123`; docs formerly listed another seed | M5 + M6 | `R1/G0` |  PARTIALLY VERIFIED (M6 - 2026-05-29) |
| `B-007` | High | Progress can be overstated by shells/skeletons | Legacy ticked tasks lack runtime evidence; client forms are local/uncommitted artifacts | M1 + M6 | Immediate | Tracking reset applied |

## Core Risk Register

| Risk | Owner | Control |
| --- | --- | --- |
| Packet drift between docs, client and server | M2, M1 approve | API `v0.2`, `G0` serialization/error tests before integration |
| Auth/network boundary mismatch | M2 + M5 | Canonical auth handoff and runtime LOGIN trace in `G2` |
| UI directly handles packet/DB logic | M3 + M4 | Typed service boundary; reject integration completion without evidence |
| Wrong `machineId` during demo | M5 + M6 | Deterministic error test in `G2` |
| Multi-client state leak or duplicate login ambiguity | M2 + M5 | Decide behavior and test in `G4` |
| UI freeze or crash on socket/disconnect | M2 + GUI owner | Network and disconnect verification in `G1/G4` |

## Retained Extension Risks

| Extension | Current status | Rule |
| --- | --- | --- |
| Notification | `Conditional` | Open only after `G3` |
| Timer display/persistence | `Conditional` | Display after stable notification; persistence later |
| Chat | `Conditional` | Open only if `G4` passes on time |
| Real LAN | `Conditional` | Smoke-test only after local rehearsal |
| Reconnect polish | `Conditional` | Does not replace core disconnect stability |

An unopened or incomplete extension is recorded as `Retained - Continue After Core Release`, not removed from the product.

## Bug Reporting Format

For each newly observed failure, record:

- bug ID, date and reporter;
- affected gate and mode;
- reproduction steps;
- expected and actual behavior;
- severity and owner;
- evidence link or command result;
- workaround and resolution status.


