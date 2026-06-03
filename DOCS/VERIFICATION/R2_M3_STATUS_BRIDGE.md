# R2 M3 Status Bridge Evidence

Date: 2026-06-03
Branch: quyet-serverapp-member3
Scope: Member 3 server/admin UI readiness for R2 status rendering.

## Implemented

- `MainForm` sample dashboard now uses canonical recovery client machine IDs:
  - `PC-01`
  - `PC-02`
- Dashboard status rendering normalizes:
  - `Online`
  - `Offline`
  - `Stale`
  - `Disconnected`
- `ApplyMachineStatusUpdate(MachineStatusUpdate update)` is available as the typed UI bridge for a future M2 runtime status event.
- `ServerApp` supports an opt-in M3 bridge smoke flag:

```powershell
dotnet run --project Code\ServerApp\ServerApp.csproj -- --m3-status-bridge-smoke
```

After a successful admin login, the smoke applies:

```text
PC-01 -> Online
PC-01 -> Offline
```

This is UI bridge evidence only. It does not claim `G2-05` or `G2-06` because real `STATUS` routing still depends on `R2-N02`.

## Build Evidence

Command:

```powershell
dotnet build Code\NetManager.sln --artifacts-path .audit-artifacts --no-restore -v:minimal
```

Result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Pending External Verification

- `G2-01`: M6/manual verification must confirm admin login `admin` / `123` / `PC00` opens `MainForm`.
- `G2-05`: blocked until M2/M4 deliver authenticated client status routing through `R2-N02`.
- `G2-06`: blocked until M2 delivers disconnect/offline status event behavior.
