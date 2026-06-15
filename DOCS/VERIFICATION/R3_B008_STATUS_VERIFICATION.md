# B-008 Status Route Verification

Prepared by: Codex local audit for M6 handoff

Date: `2026-06-15`
Branch: `testing_branch`
Commit: `222c68c Add server ACK handling`
Runtime mode: Local

## Scope

This note verifies only the carry-forward `B-008/G2-05` gap:

- authenticated client can send a typed `STATUS` packet after login;
- server dispatcher accepts the packet instead of rejecting `STATUS` as unsupported;
- server emits a runtime status update usable by the Admin dashboard;
- bug disposition can move from `Open` to closed for the status gap.

This does not approve the full `G3` control gate. `LOCK`/`UNLOCK`, visible client reaction, ACK display in Admin UI and repeated one-client demo remain tracked by `G3-01` through `G3-05`.

## Code Path Checked

- `ClientApp.Forms.ClientMainForm.SendResumeStatusAsync()` sends `STATUS` with `machineId`, `sessionId`, `MachineName`, `Status=Online` and `LastSeen`.
- `ServerApp.Networking.PacketDispatcher.DispatchAsync()` routes `Packet<StatusPayload>` into `DispatchStatusAsync()`.
- `DispatchStatusAsync()` validates the optional session/machine binding, updates machine status and returns typed `ACK` with `ackFor=STATUS`, `status=Accepted`.
- `TcpJsonLineServer` binds the machine, emits `STATUS`, and raises `StatusEmitted` for `Program` to call `MainForm.ApplyMachineStatusUpdate()`.

## Commands Run

```powershell
dotnet build Code\NetManager.sln --no-restore -v:minimal
dotnet run --project Code\NetworkSmokeTest\NetworkSmoke.csproj --no-restore
dotnet run --project Code\Auth_Test\Auth_Test.csproj --no-restore
dotnet run --project Code\ContractSmoke\ContractSmoke.csproj --no-restore
```

## Result

- Build passed with `0` warnings and `0` errors.
- `NetworkSmokeTest` passed `PASS: authenticated STATUS route returns Accepted ACK`.
- The trace includes client-originated `STATUS` from `PC-01` to `server`, server `ACK` with `ackFor=STATUS` and `status=Accepted`, and emitted `STATUS Online`.
- `Auth_Test` and `ContractSmoke` remained passing.

## Disposition

`B-008` is closed for the client-sent `STATUS` route. `G2-05` and `G3-00` can be treated as pass for this specific blocker on the tested candidate.
