# BUGS

## Risk Register

Current flow risks and controls:

| Risk | Owner | Severity | Control |
| --- | --- | --- | --- |
| Packet contract drift between server and client | M2, M1 approves | High | `DOCS/API.md` is the contract source of truth; packet changes must be documented same day. |
| Login ownership unclear between networking and auth | M2 + M5 | High | M2 owns transport; M5 owns auth/session validation. |
| GUI code invents its own packet parsing | M3 + M4 | Medium | GUI consumes agreed services/interfaces only. |
| Wrong `machineId` mapping during demo | M5 | High | Account-to-machine rule is frozen and must be tested by `W3.P1`. |
| Real LAN behavior differs from local multi-instance | M2 + M6 | Medium | Both demo modes need separate smoke checks by `W6.P3`. |
| Chat scope expands beyond MVP | M1 | Medium | Chat remains 1-1 text only, no emoji/history/file/group. |
| UI freezes on network receive | M2 + GUI owner | High | Network events must not block WinForms UI thread. |
| Database schema changes after consumers depend on it | M5, M1 approves | Medium | Auth/session contract must be reviewed in `W1.P2` before deep implementation. |

## Known Bugs

- No live TCP/auth/database end-to-end runtime has been completed yet, so most runtime bugs are still pending discovery.
- Current code contains scaffold and server UI shell behavior; networking, auth, persistence, real command flow, and client workflow still need implementation.

## Unresolved Issues

- TCP listener/connector and send/receive loop are not completed yet.
- First TCP JSON-line round trip is still pending.
- Auth/database folders and runtime implementation still need to be created under `Code/ServerApp/Auth/` and `Code/ServerApp/Database/`.
- Client workflow screens still need implementation beyond the default shell.
- Reconnect behavior is documented but not validated in code.
- Real LAN mode and local multi-instance mode are planned but not validated in runtime.
- Account-to-`machineId` validation is defined in docs but not validated in runtime.
- Default port, database path, and seed account baseline are documented in `RUN_GUIDE.md` but not implemented yet.

## Temporary Fixes

- None.

## Debugging Notes

- Use `TASKS.md` for progress and `TEST_MATRIX.md` for test status.
- Week 1 should end with scaffold clarity and first connection proof, or leave round trip as the top W2 blocker.
- When code is added, record only actionable bugs here.
- Keep each bug entry concise and tied to a reproduction step.
- Mark whether a bug appears in `Mode A - Real LAN Demo`, `Mode B - Local Multi-Instance Demo`, or both.

## Bug Tracking Format

- title
- affected module
- affected mode
- reproduction steps
- expected behavior
- actual behavior
- severity
- temporary workaround if any

## Severity Guidance

- High: blocks login, connection, or core demo flow
- Medium: affects usability but has a workaround
- Low: cosmetic or non-blocking issue
