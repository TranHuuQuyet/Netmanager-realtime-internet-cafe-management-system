# DEMO CHECKLIST

This checklist defines the final demo path and fallback plan. Member 6 owns the checklist, and Member 1 approves final demo readiness.

## 1. Primary Demo Goal

Show a LAN-based internet cafe management flow:

1. Start server.
2. Start one or more clients.
3. Login admin.
4. Login clients with bound `machineId`.
5. Show realtime machine status.
6. Lock one client.
7. Show client lock screen.
8. Unlock that client.
9. Send notification.
10. Show timer update.
11. Send one 1-1 chat message.

## 2. Core Demo Path

This path must work even if secondary features are reduced.

| Step | Expected Result | Status |
| --- | --- | --- |
| Start server | Server is listening and UI is responsive | Pending |
| Start client | Client can connect to server | Pending |
| Client login | Correct account and `machineId` succeed | Pending |
| Wrong machine test | Wrong `machineId` is rejected clearly | Pending |
| Status sync | Server dashboard shows client online | Pending |
| Lock command | Client enters locked state | Pending |
| ACK after lock | Server can see command result | Pending |
| Unlock command | Client exits locked state | Pending |
| Disconnect test | Server remains running | Pending |

## 3. Secondary Demo Path

These features should be demonstrated only after the core path is stable.

| Feature | Expected Result | Status |
| --- | --- | --- |
| Notification | Client receives message with severity | Pending |
| Timer | Client sees remaining time update | Pending |
| Chat | Admin and one client exchange text | Pending |
| Two clients | Server tracks both clients separately | Pending |
| Real LAN | Client machine connects through LAN IP | Pending |

## 4. Fallback Scope

If the team is behind schedule, preserve these features:

- login
- machine-bound client identity
- status sync
- lock/unlock
- ACK
- local multi-instance demo

Cut or simplify in this order:

1. chat polish
2. timer persistence
3. notification broadcast
4. reconnect polish
5. real LAN demo, if local multi-instance is stable and accepted as fallback

Do not cut:

- correct `machineId` validation
- server/client connection
- lock/unlock demo path

## 5. Pre-Demo Environment Check

Before the final demo:

- server machine has .NET 8 installed
- server port is known
- firewall rule is checked
- demo accounts are seeded
- local multi-instance fallback is tested
- database reset path is known
- latest build matches `RUN_GUIDE.md`
- high-severity bugs are closed or explicitly accepted by Member 1

## 6. Demo Roles

| Role | Responsibility |
| --- | --- |
| Member 1 | leads demo, decides fallback |
| Member 2 | supports network setup |
| Member 3 | demonstrates server dashboard |
| Member 4 | demonstrates client behavior |
| Member 5 | explains auth, session, and database |
| Member 6 | controls checklist and bug notes |
