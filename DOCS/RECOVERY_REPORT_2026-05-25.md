# NETMANAGER RECOVERY REPORT - 2026-05-25

## 1. Purpose And Delivery Decision

Bao cao nay thay the cach danh gia tien do dua tren scaffold hoac checkbox cu bang runtime evidence.

- Baseline audit: working tree ngay `2026-05-25`.
- Deadline delivery bat buoc: `2026-07-05`.
- Thoi gian recovery: `41` ngay, gom 6 tuan tu `R1` den `R6`.
- Delivery bat buoc: `Core Local Demo`.
- Cac feature quan trong khong bi xoa. Chung duoc giu trong `Retained Extension Track` va chi mo khi core gate cho phep.

Ket luan audit: project dang o trang thai **Documentation-heavy but implementation-light** va **No integration proof yet**. Roadmap 8 tuan cu khong con realistic neu tiep tuc dung feature cadence cu.

## 2. Baseline Evidence

| Check | Evidence ngay 2026-05-25 | Ket luan |
| --- | --- | --- |
| Checklist cu | `14/144` task tick (`9.7%`); Week 1 tick `14/18` | Phan lon la docs, ownership, shell hoac skeleton |
| Runtime tests | `0/33` case trong `DOCS/TEST_MATRIX.md` co status `Pass` | Chua co runtime feature duoc verify |
| Demo checklist | `14/14` buoc van `Pending` | Chua co demo path duoc chung minh |
| Build | `dotnet build Code/NetManager.sln` fail tai `ServerApp/Forms/LoginForm.Designer.cs(7,29)` | Working tree khong build duoc |
| Server UI | Dashboard dung sample rows va placeholder action | UI shell only |
| Client UI | Forms dang la local/uncommitted artifacts; chi mo dialog/lock shell | UI shell only, chua integrated |
| Networking | Co DTO/helper va socket wrappers; khong thay listener/dispatcher/routing wiring | Chua co TCP runtime proof |
| Auth/DB | Co SQLite auth implementation va `AuthUsers/AuthSessions`, nhung khong co login qua network/UI | Isolated implementation only |

## 3. Critical Findings

| Severity | Finding | Impact |
| --- | --- | --- |
| Critical | Full solution build dang fail vi server login code-behind bi xoa trong working tree | Khong the chay gate hoac demo |
| Critical | Chua co first TCP JSON-line round-trip; chua co server listener/dispatcher | Moi realtime flow bi block |
| High | `API.md` mo ta `type` string va top-level error, trong khi shared serializer/parser chua chung minh dung wire shape | Packet contract drift khi integration |
| High | Co hai huong database: `AuthUsers/AuthSessions` va `Users/Machines/Sessions` | Auth/network khong co persistence source duy nhat |
| High | Seed/admin rule trong runtime auth lech docs cu | Demo account va machine validation de fail |
| High | Checkbox shell/skeleton co the bi hieu la feature usable | Fake progress va gate approval sai |

## 4. Disposition Cua Checkbox Da Tick

Lich su cong viec khong bi xoa. No duoc phan loai lai de khong tinh nham vao runtime delivery.

| Task cu | Evidence tim thay | Disposition |
| --- | --- | --- |
| `W1.P1` M1-M6 scope/ownership/rules | Docs ownership va flow ton tai | `Historical Artifact Confirmed` |
| `W1.P2` M2 packet baseline | `API.md` va shared packet artifacts ton tai, co drift risk | `Partial - Reverification Required`; tao lai `R1-C02` |
| `W1.P2` M4 client handoff | Handoff note trong member playbook local update | `Historical Handoff Confirmed`; verify lai sau API freeze |
| `W1.P2` M5 auth/schema draft | Auth/schema artifacts ton tai, khong canonical | `Partial - Canonicalization Required`; tao lai `R1-A01` |
| `W1.P3` M1 solution/references | Solution va project references ton tai | `Artifact Confirmed`; build gate van fail |
| `W1.P3` M3 server shell | Forms ton tai, current build broken | `Artifact Exists - Not Usable Yet` |
| `W1.P3` M4 client shell | Forms local/uncommitted, khong dung service that | `Local Artifact Only` |
| `W1.P3` M5 auth/DB skeleton | Co implementation co lap, chua integrated | `Skeleton Exists - Runtime Unverified` |
| `W1.P3` M6 docs templates | Bug/test/demo templates ton tai | `Template Confirmed`; khong phai test pass |

## 5. Scope For 2026-07-05

### Core Delivery Gate - Bat Buoc

- Solution build thanh cong tren setup chinh thuc.
- TCP JSON-line local round-trip va invalid packet handling.
- Client login qua TCP/auth; dung `machineId` thanh cong va sai `machineId` bi reject ro.
- Server hien online/offline status tu client.
- `LOCK`, `UNLOCK`, `ACK` va error visible chay end-to-end.
- Hai client local duoc phan biet theo account/machine.
- Disconnect khong lam server crash.
- Local multi-instance demo duoc rehearsal tren release build.

### Retained Extension Track - Van Nam Trong Roadmap

| ID | Feature | Open gate | Status ban dau |
| --- | --- | --- | --- |
| `E1` | Direct notification | `G3 Core Control` pass | `Conditional` |
| `E2` | Timer display | `E1` pass va khong co High/Critical blocker | `Conditional` |
| `E3` | 1-1 text chat | `G4` pass truoc `2026-06-21` | `Conditional` |
| `E4` | Real LAN smoke test | Local rehearsal pass truoc `2026-06-28` | `Conditional` |
| `E5` | Notification broadcast | Direct notification stable | `Conditional` |
| `E6` | Timer persistence | Timer display va session stable | `Conditional` |
| `E7` | Reconnect polish | Disconnect stability pass | `Conditional` |

Feature extension khong xong truoc deadline phai ghi `Retained - Continue After Core Release`, khong bi xoa khoi project.

### Product Backlog Retained

- Customer CRUD.
- Shutdown control.
- Dashboard polish ngoai core flow.
- Reporting va analytics sau MVP.

Backlog nay chi duoc mo sau khi `G5 Release` pass.

## 6. Recovery Architecture Decisions

- `Shared` la wire-contract source duy nhat; forms khong parse JSON va khong invent payload.
- API recovery baseline la `v0.2`: envelope co `type` string va top-level `success/message/error`.
- SQLite auth path trong `AuthBootstrapper` la canonical runtime cho delivery nay.
- Canonical demo seed theo runtime auth: `admin` / `PC00` / `123`, `client01` / `PC-01` / `123`, `client02` / `PC-02` / `123`.
- `AuthUsers/AuthSessions` la runtime schema canonical cho recovery; schema `Users/Machines/Sessions` la future consolidation, khong integrate song song truoc release.
- Core machine online/offline state nam trong authenticated in-memory connection registry.
- Server target boundary: listener -> registry -> typed dispatcher -> auth/command handler -> UI event bridge.
- Client target boundary: connection service -> typed receive handler -> command executor -> ACK sender -> UI binding.

## 7. Six-Week Recovery Roadmap

| Week | Date | Core gate | Extension rule |
| --- | --- | --- | --- |
| `R1 Foundation Repair` | `25/05-31/05` | Build, contract/auth/DB freeze, packet test, TCP local round-trip | Tat ca extension `Conditional` |
| `R2 Authenticated Status` | `01/06-07/06` | Real login, wrong-machine rejection, one-client status | Chua mo extension routing |
| `R3 Core Control` | `08/06-14/06` | Lock/unlock/ACK/error va repeated one-client demo | Pass thi mo `E1` |
| `R4 Multi-Client & Notification` | `15/06-21/06` | Two-client local, disconnect stability | Lam `E1`; co the mo `E2/E3` |
| `R5 Stabilization & Extensions` | `22/06-28/06` | Regression, clean setup, local rehearsal | LAN chi mo sau local rehearsal |
| `R6 Release & Demo` | `29/06-05/07` | RC, freeze, hai rehearsal, demo | Bao cao pass/incomplete/retained |

Guardrails:

- Neu `R1` fail ngay `31/05`, extension giu lai nhung khong mo.
- Neu `R3` fail ngay `14/06`, khong trien khai extension truoc demo.
- Neu `R4` fail ngay `21/06`, demo bat buoc ha xuong one-client local; multi-client van duoc giu trong recovery backlog.
- Muc tieu freeze build la `30/06`; tu `01/07` chi fix demo blocker duoc M1 approve.

## 8. Member Delivery Summary

| Member | Core responsibility before deadline | Retained extension ownership |
| --- | --- | --- |
| M1 | Freeze scope/contract, approve gates, block scope creep, approve RC | Decide extension opening va continuation |
| M2 | Contract implementation, TCP listener/dispatcher, login/status/control routing, multi-client | Route notification/timer/chat va support LAN/reconnect |
| M3 | Restore server UI build path, bind real state/control/ACK | Admin notification/timer/chat views |
| M4 | Integrate client forms with real service, lock/unlock/ACK | Client notification/timer/chat views |
| M5 | Canonical auth/SQLite/session/machine validation | Timer persistence va extended session behavior |
| M6 | Mark actual fail/blocked/pass, verify every gate, rehearsal docs | Verify extension status va continuation report |

## 9. Reporting Rule

- `DOCS/TASKS.md` la active tracker duy nhat sau recovery reset.
- Chi `Verified Pass` duoc tinh vao delivery completion.
- Moi evidence phai co ngay, owner va link/log/test result.
- Moi blocker High/Critical phai co owner va due date.
- Bao cao cu hoac local artifacts chi la context, khong duoc dung de tick runtime completion.

