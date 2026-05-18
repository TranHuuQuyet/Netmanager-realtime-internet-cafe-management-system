# BUGS

## Risk Register

Current Phase 0 risks and controls:

| Risk | Owner | Severity | Control |
| --- | --- | --- | --- |
| Packet contract drift between server and client | Member 2, Member 1 approves | High | `DOCS/API.md` is the contract source of truth; packet changes must be documented same day. |
| Login ownership unclear between networking and auth | Member 2 + Member 5 | High | Member 2 owns transport; Member 5 owns auth/session validation. |
| GUI code invents its own packet parsing | Member 3 + Member 4 | Medium | GUI consumes agreed services/interfaces only. |
| Wrong `machineId` mapping during demo | Member 5 | High | Account-to-machine rule is frozen and must be tested before integration. |
| Real LAN behavior differs from local multi-instance | Member 2 + Member 6 | Medium | Both demo modes need separate smoke checks. |
| Chat scope expands beyond MVP | Member 1 | Medium | Chat remains 1-1 text only, no emoji/history/file/group. |
| UI freezes on network receive | Member 2 + GUI owner | High | Network events must not block WinForms UI thread. |
| Database schema changes after consumers depend on it | Member 5, Member 1 approves | Medium | Auth/session contract must be reviewed in Phase 1 before deep implementation. |

## Known Bugs

- No application runtime implementation has been committed yet, so runtime bugs cannot be observed in code.
- Existing roadmap and architecture docs describe intended behavior, but implementation status is still pending.

## Unresolved Issues

- Source folder skeleton is present, but `.sln` and `.csproj` files are not present yet.
- Exact packet schema implementation has not been committed.
- SQLite schema and auth storage details still need implementation.
- Reconnect behavior is only documented, not validated in code.
- Real LAN mode and local multi-instance mode are planned but not yet validated in code.
- Account-to-`machineId` validation is defined in docs but not yet validated in code.
- Default port, database path, and seed account baseline are documented in `RUN_GUIDE.md` but not implemented yet.

## Temporary Fixes

- None.

## Debugging Notes

- Treat the current repository as documentation-first until implementation files appear.
- Week 1 should produce a runnable skeleton so connection and packet tests can start early.
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
