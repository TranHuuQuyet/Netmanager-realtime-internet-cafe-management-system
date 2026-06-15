# DEMO CHECKLIST - RECOVERY RELEASE

Deadline: `2026-07-05`
Primary acceptance mode: `Real LAN + Local Multi-Instance Required Demo`
Extension policy: retained features may be shown only when their gate has passed.

## Core Demo Goal

Demonstrate stable internet-cafe control behavior with two physical LAN clients while keeping local multi-instance regression available:

1. Start the approved server build.
2. Start two physical LAN clients and verify local multi-instance fallback still works.
3. Login with machine-bound client accounts.
4. Show online/offline state.
5. Lock one selected client.
6. Show ACK/result and unlock it.
7. Show the other client was not affected.
8. Exchange 1-1 chat between Admin and the selected client only.
9. Start timed and open-ended rentals from Admin Panel.
10. Demonstrate timed warning/expiry `LOCK`, open-ended amount calculation and SQLite billing restore after ServerApp restart.
11. Demonstrate disconnect/reconnect/resync without server crash.

## Mandatory Core Checklist

| Step | Expected result | Related gate | Current status |
| --- | --- | --- | --- |
| Build RC | Solution builds and approved binaries are identifiable | `G0`, `G5` | `Blocked` |
| Start server | Server listens on an approved LAN-capable endpoint and UI remains responsive | `G1`, `G5` | `Blocked` |
| Start clients | Two physical LAN clients connect distinctly; two local instances still pass regression/fallback | `G1`, `G4`, `G5` | `Blocked` |
| Valid login | `client01/PC-01` and `client02/PC-02` succeed | `G2`, `G4` | `Partial(M6 - 2026-06-08; one-client G2 pass, two-client G4 still blocked)` |
| Wrong-machine check | Wrong `machineId` fails visibly | `G2` | `Pass(M6 - 2026-06-08)` |
| Status view | Server displays real online/offline state | `G2` | `Pass(local audit - 2026-06-15; B-008 closed for client-sent STATUS; two-client G4 still blocked)` |
| Lock selected client | Selected client enters lock state only | `G3`, `G4` | `Blocked` |
| ACK result | Admin sees command result/error | `G3` | `Blocked` |
| Unlock selected client | Client exits lock state | `G3` | `Blocked` |
| Disconnect | Server remains running and state is controlled | `G4` | `Blocked` |
| 1-1 chat | Admin chats with selected client and the other client does not receive it | `G5` | `Blocked` |
| Start rentals | Admin starts timed rental for one machine and open-ended rental for the other | `G5` | `Blocked` |
| Timed rental expiry | Demo package `5-10` minutes shows countdown, 5-minute warning and expiry `LOCK` without logout | `G5` | `Blocked` |
| Open-ended billing | Amount follows rounded-minute formula; `61` seconds charges `2` minutes at `10000` VND/hour | `G5` | `Blocked` |
| Restart restore | ServerApp restart restores active `BillingSessions` from SQLite and resyncs running clients enough for timer/billing and extend/LOCK | `G5` | `Blocked` |
| Rehearsal twice | Same required path passes twice on RC build | `G5` | `Blocked` |

## Retained Extension Demonstrations

These are important project features and remain reportable. They do not replace mandatory core acceptance.

| Extension | Expected demo when opened | Gate to open | Current status |
| --- | --- | --- | --- |
| Direct notification | Selected client receives an admin message | `G3` pass | `Conditional` |
| Notification broadcast | Additional verified broadcast evidence | `E1` stable | `Conditional` |
| Reconnect polish | UX retry/backoff polish beyond required minimal resync | `G5-09` pass | `Conditional` |

At final reporting, each extension is marked `Verified Pass Before Release`, `Opened but Incomplete`, or `Retained - Continue After Core Release`.

## Fallback And Guardrails

- Physical LAN and local multi-instance regression are both required after the `2026-06-09` scope decision.
- The release candidate reaches `main` only after M6 verifies its integrated `develop` build as `Pass` and M1 approves promotion.
- If a required LAN/chat/billing path cannot pass, it is a core blocker unless M1 records an explicit demo exception.
- Local multi-instance never replaces a failing physical LAN path, and physical LAN never replaces a failing local regression path.
- No extension may enter the demo script if it introduces an open High/Critical blocker.

## Pre-Demo Environment Checklist

| Item | Status |
| --- | --- |
| Approved `.NET 8` environment available | To verify |
| RC build identity recorded | Blocked until `G5` |
| Approved local and LAN endpoints known | Blocked until network runtime exists |
| Firewall rule or port allowance for LAN test prepared | Blocked until `G5` LAN setup |
| Canonical SQLite database/seed and billing reset/restore instructions verified | Blocked until `G0/G2/G5` |
| Demo accounts `admin`, `client01`, `client02` tested | Blocked until `G2` |
| Two physical LAN clients and two local client instances rehearsed | Blocked until `G4/G5` |
| Billing demo packages `5-10` minutes prepared | Blocked until `G5` |
| Known limitations and retained extension report prepared | Not started |

## Demo Roles

| Member | Responsibility |
| --- | --- |
| M1 | Approves release, leads demo, selects documented fallback |
| M2 | Supports network/runtime routing, LAN, chat and minimal reconnect/resync |
| M3 | Demonstrates server status, control result, chat view and billing monitor |
| M4 | Demonstrates client reaction, timer/billing display, chat and LOCK behavior |
| M5 | Explains auth/session/database decisions, machine validation and `BillingSessions` restore |
| M6 | Operates checklist, captures evidence and reports required/retained feature status |
