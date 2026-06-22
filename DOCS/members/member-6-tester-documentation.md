# Member 6 - Tester And Documentation

## Recovery Role

Ban own evidence, test status, bug tracking, run/demo docs va final continuation report. Audit baseline co `0/33` runtime test pass va build fail, nen M6 tham gia tu `R1`, khong cho den release moi test; tu `2026-06-09`, LAN/chat/billing/restart restore la required scope can `G5` evidence.

## Write Scope

- `DOCS/BUGS.md`
- `DOCS/RUN_GUIDE.md`
- `DOCS/TEST_MATRIX.md`
- `DOCS/DEMO_CHECKLIST.md`
- `README.md`
- Tracker updates khi M1 yeu cau

## Non-Owned Scope

- Runtime implementation cua M2-M5.
- API/auth/scope decision approval cua M1.
- Retained extension implementation khi feature chua duoc mo.

## Dependencies

- Can owner nop build/test evidence kem build identity va affected gate.
- Can M1 ghi gate, exception va release decisions truoc khi reporting.
- Can current integration build runnable de nang status tu `Blocked` sang `Pass` hoac `Fail`.
- Can candidate da duoc integrate vao `testing_branch` va co commit/build identity ro truoc khi xac nhan de promote vao `main`.

## Boundary Rules

- Danh dau `[x]` trong tracker khi member da nop task/evidence; ghi ket qua verify cua current build trong test/demo docs.
- Moi `Fail` phai co bug ID, steps, actual/expected, severity va owner.
- `Blocked` phai ghi dependency dang thieu; khong de pending mo ho.
- Extension khong xong van duoc bao cao `Retained - Continue After Core Release`.
- Promoted LAN/chat/billing/restart restore khong duoc bao cao nhu retained extension; neu fail thi ghi core/demo blocker hoac M1 exception ro.
- Test tren candidate cua `testing_branch`; chi ghi `Pass` kem tested commit/build identity moi duoc dung lam dieu kien M1 merge vao `main`.
- Neu test `Fail` hoac `Blocked`, ghi evidence/blocker va khong chap nhan promotion vao `main`.

## Core Assignments

| Due | Task | Dependency | Required evidence |
| --- | --- | --- | --- |
| `2026-05-27` | Publish initial fail/blocked baseline va docs mismatch list | Audit evidence | Bugs/test/docs update |
| `2026-05-31` | Verify build/contract/network foundation | Owner submissions | `G0/G1` statuses |
| `2026-06-07` | Verify login/machine/status | Runtime flow | `G2` report |
| `2026-06-14` | Verify command/ACK repeated demo | Runtime flow | `G3` report |
| `2026-06-21` | Verify two-client/disconnect and promoted-scope readiness for chat/billing/LAN | Runtime flow | `G4` report |
| `2026-06-28` | Verify regression/setup/local rehearsal, physical LAN, chat, billing timed/free, restart restore and minimal resync | RC candidate | `G5` report |
| `2026-07-05` | Publish final required demo and retained extension continuation status | Release build | Final report |

## Retained Extension Ownership

- Verify retained direct notification, broadcast or reconnect polish only when M1 opens the corresponding gate.
- Verify promoted timer/billing, chat, Real LAN and billing persistence as required `G5` scope.
- Record every retained feature in final status even if it is not complete by core delivery.

## Definition Of Done

- Tests reflect executable truth rather than claimed progress.
- Moi promotion vao `main` co `Pass` evidence cua candidate tuong ung tren `testing_branch`.
- Demo can be followed from current run guide.
- Final report separates passed required capability, failed/excepted required scope, and retained extension continuation.
