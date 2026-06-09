# DEMO CHECKLIST - RECOVERY RELEASE

Deadline: `2026-07-05`
Primary acceptance mode: `Local Multi-Instance Core Demo`
Extension policy: retained features may be shown only when their gate has passed.

## Core Demo Goal

Demonstrate stable internet-cafe control behavior on one machine using the server app and local client instances:

1. Start the approved server build.
2. Start two client instances.
3. Login with machine-bound client accounts.
4. Show online/offline state.
5. Lock one selected client.
6. Show ACK/result and unlock it.
7. Show the other client was not affected.
8. Demonstrate disconnect without server crash.

## Mandatory Core Checklist

| Step | Expected result | Related gate | Current status |
| --- | --- | --- | --- |
| Build RC | Solution builds and approved binaries are identifiable | `G0`, `G5` | `Blocked` |
| Start server | Server listens locally and UI remains responsive | `G1` | `Blocked` |
| Start clients | Two client instances connect distinctly | `G1`, `G4` | `Blocked` |
| Valid login | `client01/PC-01` and `client02/PC-02` succeed | `G2`, `G4` | `Blocked` |
| Wrong-machine check | Wrong `machineId` fails visibly | `G2` | `Blocked` |
| Status view | Server displays real online/offline state | `G2` | `Blocked` |
| Lock selected client | Selected client enters lock state only | `G3`, `G4` | `Blocked` |
| ACK result | Admin sees command result/error | `G3` | `Blocked` |
| Unlock selected client | Client exits lock state | `G3` | `Blocked` |
| Disconnect | Server remains running and state is controlled | `G4` | `Blocked` |
| Rehearsal twice | Same path passes twice on RC build | `G5` | `Blocked` |

## Retained Extension Demonstrations

These are important project features and remain reportable. They do not replace mandatory core acceptance.

| Extension | Expected demo when opened | Gate to open | Current status |
| --- | --- | --- | --- |
| Direct notification | Selected client receives an admin message | `G3` pass | `Conditional` |
| Timer display | Client displays issued remaining time | Notification stable; no core blocker | `Conditional` |
| 1-1 text chat | Admin and one client exchange text | `G4` pass on time | `Conditional` |
| Real LAN smoke | One client connects through server LAN IP | Local rehearsal pass | `Conditional` |
| Broadcast/timer persistence/reconnect | Additional verified extension evidence | Extension prerequisite pass | `Conditional` |

At final reporting, each extension is marked `Verified Pass Before Release`, `Opened but Incomplete`, or `Retained - Continue After Core Release`.

## Fallback And Guardrails

- Local multi-instance is the approved primary/fallback platform for core release.
- The release candidate reaches `main` only after M6 verifies its integrated `develop` build as `Pass` and M1 approves promotion.
- If two-client core cannot pass by `2026-06-21`, M1 may demonstrate one-client core and retain two-client work after release, with the limitation stated explicitly.
- Real LAN never replaces a failing local core path.
- No extension may enter the demo script if it introduces an open High/Critical blocker.

## Pre-Demo Environment Checklist

| Item | Status |
| --- | --- |
| Approved `.NET 8` environment available | To verify |
| RC build identity recorded | Blocked until `G5` |
| Approved local endpoint known | Blocked until network runtime exists |
| Canonical SQLite database/seed reset instructions verified | Blocked until `G0/G2` |
| Demo accounts `admin`, `client01`, `client02` tested | Blocked until `G2` |
| Two local client instances rehearsed | Blocked until `G4/G5` |
| Known limitations and retained extension report prepared | Not started |

## Demo Roles

| Member | Responsibility |
| --- | --- |
| M1 | Approves release, leads demo, selects documented fallback |
| M2 | Supports network/runtime routing and any opened extension |
| M3 | Demonstrates server status and control result |
| M4 | Demonstrates client reaction and any opened client extension |
| M5 | Explains auth/session/database decisions and machine validation |
| M6 | Operates checklist, captures evidence and reports retained features |
