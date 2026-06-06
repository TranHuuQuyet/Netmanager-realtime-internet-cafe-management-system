# M5 DB Split Stage 2 - Nhom 2 Rehearsal Note

## Muc tieu

Nhom 2 dam bao database sau khi reset van chay on, dung schema, dung path va san sang demo.

## Baseline can dat

- File SQLite runtime phai tao tai root repo:
  - `internet_cafe.db`
- Khong duoc roi ve path cu:
  - `AppData/netmanager.db`
- Schema sau reset phai khop file:
  - `Code/ServerApp/Database/DatabaseSchema.sql`
- Cac bang chinh bat buoc co:
  - `AuthUsers`
  - `Machines`
  - `AuthSessions`
- Cac index co ban bat buoc co:
  - `IX_AuthUsers_Username`
  - `IX_AuthUsers_MachineId`
  - `IX_AuthSessions_UserId_State`
  - `IX_AuthSessions_MachineId_State`

## Checklist test

### 1. Kiem tra DB path

- [x] Chay app tu root repo.
- [x] Xac nhan file `internet_cafe.db` dang co o root repo.
- [x] Xac nhan hien khong co DB tai `AppData/netmanager.db` trong workspace.
- [x] Kiem tra cac module sau dung chung mot DB path:
  - `DatabaseBootstrapper`
  - `DbInitializer`
  - `DatabaseOptions`

Ghi chu hien tai:

- `internet_cafe.db` dang ton tai o root repo.
- Khong tim thay `AppData/netmanager.db` sau reset.
- `DatabasePathResolver` da la resolver dung chung.
- `DatabaseBootstrapper` dung `DatabasePathResolver.Resolve(...)`.
- `DatabaseOptions` default ve `DatabasePathResolver.DefaultDatabaseFileName` la `internet_cafe.db`.
- `DbInitializer` dung `DatabaseOptions.DatabasePath`, nen cung di qua resolver chung.
- Ket luan code check: **Pass** muc dung chung 1 path.

### 2. Test clean database

- [x] Reset/xoa DB theo quy trinh project.
- [x] Chay lai app.
- [x] Xac nhan DB tu tao lai.
- [x] Xac nhan khong co loi path.
- [x] Xac nhan schema duoc apply lai dung.

Lenh goi y de kiem tra nhanh bang/index neu co `sqlite3`:

```powershell
sqlite3 internet_cafe.db ".tables"
sqlite3 internet_cafe.db "SELECT type, name, tbl_name FROM sqlite_master WHERE type IN ('table','index') ORDER BY type, name;"
```

### 3. Xac nhan schema baseline

- [x] `DatabaseSchema.sql` co du bang `AuthUsers`, `Machines`, `AuthSessions`.
- [x] `DatabaseSchema.sql` co du index baseline.
- [x] `Database01.csproj` copy `DatabaseSchema.sql` ra output theo `TargetPath` la `Database\DatabaseSchema.sql`.
- [x] `DatabaseBootstrapper` co goi seed user va seed machine.
- [x] `internet_cafe.db` hien tai khop baseline schema.

Sau reset, ket qua mong doi phai co:

```text
table|AuthSessions|AuthSessions
table|AuthUsers|AuthUsers
table|Machines|Machines
index|IX_AuthSessions_MachineId_State|AuthSessions
index|IX_AuthSessions_UserId_State|AuthSessions
index|IX_AuthUsers_MachineId|AuthUsers
index|IX_AuthUsers_Username|AuthUsers
```

Co the co them `sqlite_autoindex_*` do SQLite tu tao cho primary key/unique constraint.

## Evidence hien tai

Thoi diem kiem tra: 2026-06-06.

### Setup/reset result

- File DB hien co: `internet_cafe.db` tai root repo.
- Backup DB truoc reset: `internet_cafe.db.pre-reset-20260606142558.bak`.
- Da reset DB bang cach backup DB cu, cho app bootstrap lai DB moi.
- Khong tim thay `AppData/netmanager.db` trong workspace sau reset.
- `PRAGMA integrity_check` tra ve `ok`.
- DB hien tai dat baseline schema.

### Code check result

| Hang muc | Trang thai | Evidence |
| --- | --- | --- |
| `DatabaseSchema.sql` co bang `AuthUsers` | Pass | `CREATE TABLE IF NOT EXISTS AuthUsers` |
| `DatabaseSchema.sql` co bang `Machines` | Pass | `CREATE TABLE IF NOT EXISTS Machines` |
| `DatabaseSchema.sql` co bang `AuthSessions` | Pass | `CREATE TABLE IF NOT EXISTS AuthSessions` |
| `DatabaseSchema.sql` co index baseline | Pass | Co `IX_AuthUsers_Username`, `IX_AuthUsers_MachineId`, `IX_AuthSessions_UserId_State`, `IX_AuthSessions_MachineId_State` |
| Schema file duoc copy ra runtime output | Pass | `Database01.csproj` va `ServerApp.csproj` dat `CopyToOutputDirectory=PreserveNewest`, `TargetPath=Database\DatabaseSchema.sql` |
| `DatabaseBootstrapper` bootstrap DB | Pass | `CreateAsync` goi `store.InitializeAsync`, `SeedUsersAsync`, `SeedMachinesAsync` |
| `DbInitializer` doc schema file | Pass | Doc `Database\DatabaseSchema.sql`, co fallback schema day du bang va index |
| `DatabaseOptions` dung root `internet_cafe.db` | Pass | Default dung `DatabasePathResolver.DefaultDatabaseFileName` |
| Tat ca module dung chung path resolver | Pass | Co `DatabasePathResolver`; `DatabaseBootstrapper`, `DbInitializer`, `DatabaseOptions` cung di qua resolver chung |
| DB hien tai khop schema baseline | Pass | DB sau reset co du 3 bang va 4 index baseline |

### Schema thuc te trong `internet_cafe.db`

Ket qua `.tables` hien tai:

```text
AuthSessions  AuthUsers  Machines
```

Ket qua `sqlite_master` hien tai:

```text
index|IX_AuthSessions_MachineId_State|AuthSessions
index|IX_AuthSessions_UserId_State|AuthSessions
index|IX_AuthUsers_MachineId|AuthUsers
index|IX_AuthUsers_Username|AuthUsers
index|sqlite_autoindex_AuthSessions_1|AuthSessions
index|sqlite_autoindex_AuthUsers_1|AuthUsers
index|sqlite_autoindex_AuthUsers_2|AuthUsers
index|sqlite_autoindex_Machines_1|Machines
index|sqlite_autoindex_Machines_2|Machines
table|AuthSessions|AuthSessions
table|AuthUsers|AuthUsers
table|Machines|Machines
```

Seed check sau reset:

```text
USER_COUNT|3
MACHINE_COUNT|3
```

### Ket luan hien tai

Trang thai: **Ready for demo**.

Ly do:

- Reset DB xong app tu tao lai `internet_cafe.db` tai root repo.
- Khong tao DB o path cu `AppData/netmanager.db`.
- Schema DB sau reset khop baseline trong `DatabaseSchema.sql`.
- Co du bang `AuthUsers`, `Machines`, `AuthSessions`.
- Co du index baseline.
- Seed user/machine da co du.
- `DatabaseBootstrapper`, `DbInitializer`, `DatabaseOptions` dung chung `DatabasePathResolver`.

## Viec can lam tiep

- [x] Reset DB va chay lai app de bootstrap schema moi.
- [x] Kiem tra viec copy/use `DatabaseSchema.sql` khi runtime trong csproj.
- [x] Neu DB sau reset van thieu `Machines` hoac index, can kiem tra app co dang chay dung build output moi khong.
- [x] Tao/hoan thien `DatabasePathResolver` dung chung cho:
  - `DatabaseBootstrapper`
  - `DbInitializer`
  - `DatabaseOptions`
- [x] Cap nhat `DatabaseOptions` de default path tro ve `internet_cafe.db` o root repo, khong phai `AppData/netmanager.db`.
- [x] Chay lai checklist va cap nhat ket qua cuoi cung.

## Mau ket luan da nop

```text
Nhom 2 da reset database va chay lai app bootstrap thanh cong.
DB moi duoc tao dung tai root repo: internet_cafe.db.
Khong phat sinh DB o path cu: AppData/netmanager.db.
SQLite integrity_check: ok.
Schema sau reset khop DatabaseSchema.sql.
DB co du bang baseline: AuthUsers, Machines, AuthSessions.
DB co du index baseline: IX_AuthUsers_Username, IX_AuthUsers_MachineId, IX_AuthSessions_UserId_State, IX_AuthSessions_MachineId_State.
Seed data sau reset hop le: USER_COUNT=3, MACHINE_COUNT=3.
DatabaseBootstrapper, DbInitializer, DatabaseOptions dung chung DatabasePathResolver.
Ket luan: Ready for demo.
```
