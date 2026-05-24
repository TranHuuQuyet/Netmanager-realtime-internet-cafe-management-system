# TEST MATRIX

This file defines the baseline tests for the project. Member 6 owns this file, and each module owner supports reproduction and fixes.

## 1. Test Status Legend

| Status | Meaning |
| --- | --- |
| `Pending` | not implemented or not tested yet |
| `Pass` | tested and working |
| `Fail` | tested and broken |
| `Blocked` | cannot test because a dependency is missing |

## 2. Gate B - Connection Tests

| ID | Test | Mode | Owner | Status |
| --- | --- | --- | --- | --- |
| B-01 | Server starts and listens on default port | Local | Member 2 | Pending |
| B-02 | One client connects to server | Local | Member 2 | Pending |
| B-03 | Two clients connect to server at the same time | Local | Member 2 | Pending |
| B-04 | Client sends one valid JSON-line packet | Local | Member 2 | Pending |
| B-05 | Server returns one response packet | Local | Member 2 | Pending |
| B-06 | Invalid packet does not crash server | Local | Member 2 | Pending |
| B-07 | Network receive does not freeze WinForms UI | Local | Member 2 + GUI owner | Pending |

## 3. Gate C - Auth Tests

| ID | Test | Mode | Owner | Status |
| --- | --- | --- | --- | --- |
| C-01 | Admin login succeeds | Local | Member 5 | Pending |
| C-02 | Client login succeeds with correct `machineId` | Local | Member 5 | Pending |
| C-03 | Client login fails with wrong password | Local | Member 5 | Pending |
| C-04 | Client login fails with wrong `machineId` | Local | Member 5 | Pending |
| C-05 | Disabled account cannot login | Local | Member 5 | Pending |
| C-06 | Duplicate active machine login is handled clearly | Local | Member 5 + Member 2 | Pending |

## 4. Gate D - Core Control Tests

| ID | Test | Mode | Owner | Status |
| --- | --- | --- | --- | --- |
| D-01 | Client sends `STATUS` after login | Local | Member 2 + Member 4 | Pending |
| D-02 | Server dashboard shows online machine | Local | Member 3 | Pending |
| D-03 | Admin sends `LOCK` to one client | Local | Member 3 + Member 2 | Pending |
| D-04 | Client shows lock screen after `LOCK` | Local | Member 4 | Pending |
| D-05 | Client sends `ACK` after lock | Local | Member 4 + Member 2 | Pending |
| D-06 | Admin sends `UNLOCK` to one client | Local | Member 3 + Member 2 | Pending |
| D-07 | Client exits lock screen after `UNLOCK` | Local | Member 4 | Pending |
| D-08 | Server displays command result or ACK | Local | Member 3 | Pending |

## 5. Gate E - MVP Feature Tests

| ID | Test | Mode | Owner | Status |
| --- | --- | --- | --- | --- |
| E-01 | Admin sends notification to one client | Local | Member 3 + Member 4 | Pending |
| E-02 | Admin broadcasts notification to clients | Local | Member 2 + GUI owners | Pending |
| E-03 | Timer update appears on client | Local | Member 4 | Pending |
| E-04 | Timer state appears on server if required | Local | Member 3 + Member 5 | Pending |
| E-05 | Client sends 1-1 chat to admin | Local | Member 4 + Member 3 | Pending |
| E-06 | Admin replies with 1-1 chat | Local | Member 3 + Member 4 | Pending |

## 6. Gate F - Release Tests

| ID | Test | Mode | Owner | Status |
| --- | --- | --- | --- | --- |
| F-01 | Local multi-instance smoke test passes | Local | Member 6 | Pending |
| F-02 | Real LAN smoke test passes | Real LAN | Member 6 + Member 2 | Pending |
| F-03 | Client disconnect does not crash server | Both | Member 2 | Pending |
| F-04 | Server restart behavior is documented | Both | Member 6 | Pending |
| F-05 | Clean-machine setup follows `RUN_GUIDE.md` | Both | Member 6 | Pending |
| F-06 | No high-severity demo blocker remains open | Both | Member 1 + Member 6 | Pending |

## 7. Regression Rule

When a test changes from `Pass` to `Fail`, add a bug entry to `DOCS/BUGS.md` with:

- test ID
- reproduction steps
- expected behavior
- actual behavior
- severity
- owner
