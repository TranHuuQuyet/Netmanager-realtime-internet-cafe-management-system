using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows.Forms;
using ClientApp.Networking;
using Microsoft.Data.Sqlite;
using Shared.Enums;
using Shared.DTOs.Bidrectional;
using Shared.DTOs.CommandPayloads;
using Shared.DTOs.RequestPayloads;
using Shared.DTOs.ResponsePayloads;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;
using ServerApp.Auth.Services;
using ServerApp.Database.Contracts;
using ServerApp.Database.Models;
using ServerApp.Networking;
using ServerApp.Presentation;

WriteDiagnostic("NETManager ServerApp listener JSON-line smoke test");
// The smoke runs WinForms handlers without a message loop, so keep continuations on thread-pool context.
WindowsFormsSynchronizationContext.AutoInstall = false;
SynchronizationContext.SetSynchronizationContext(null);

string databasePath = Path.Combine(Path.GetTempPath(), $"netmanager-network-smoke-{Guid.NewGuid():N}.db");

try
{
    AuthRuntime authRuntime = await AuthBootstrapper.CreateAsync(databasePath);
    List<NetworkTraceEntry> traces = [];
    List<MachineCommandAckResult> commandResults = [];
    List<(string MachineId, ChatPayload Payload)> chatMessages = [];

    using var server = new TcpJsonLineServer(
        IPAddress.Loopback,
        port: 0,
        new PacketDispatcher(authRuntime.Auth, authRuntime.SessionRepository, authRuntime.Machines),
        authRuntime.SessionService);
    server.TraceEmitted += trace =>
    {
        lock (traces)
        {
            traces.Add(trace);
        }

        if (!string.IsNullOrWhiteSpace(trace.Message))
        {
            WriteDiagnostic($"TRACE {trace.Direction} {trace.ClientId}: {trace.Message}");
        }
    };
    server.CommandResultEmitted += result =>
    {
        lock (commandResults)
        {
            commandResults.Add(result);
        }

        WriteDiagnostic(
            $"COMMAND RESULT {result.Command} {result.MachineId}: {result.Status} {result.ErrorCode ?? string.Empty} {result.RequestId}");
    };
    server.ChatReceived += (machineId, payload) =>
    {
        lock (chatMessages)
        {
            chatMessages.Add((machineId, payload));
        }

        WriteDiagnostic($"CHAT IN {machineId}: {payload.Message}");
    };

    WriteCaseHeader("TC-N01", "Server starts and listens locally.");
    server.Start();
    int port = server.LocalEndpoint.Port;
    WriteDiagnostic($"ServerApp listener active on 127.0.0.1:{port}");
    WriteCasePass("TC-N01", "Listener accepts connections and reports a local endpoint.");

    WriteCaseHeader("TC-N02", "Client connects over TCP.");
    await AssertClientConnectsAsync(port);

    WriteCaseHeader("TC-N03", "Valid JSON-line LOGIN round trip works.");
    await AssertLoginSuccessAsync(port, authRuntime.SessionRepository);

    WriteCaseHeader("TC-N04", "Invalid JSON fails gracefully.");
    await AssertRejectedLineDoesNotStopServerAsync(
        port,
        authRuntime.SessionRepository,
        "{ invalid json",
        "invalid JSON",
        "TC-N04",
        "Malformed JSON disconnects only the offending client and server remains available.");

    WriteCaseHeader("TC-N05", "Unknown packet type fails gracefully.");
    await AssertRejectedLineDoesNotStopServerAsync(
        port,
        authRuntime.SessionRepository,
        """{"type":"UNKNOWN","source":"PC01","target":"server","requestId":"unsupported-unknown","timestamp":"2026-06-02T00:00:00Z","payload":{}}""",
        "unknown packet type",
        "TC-N05",
        "Unknown packet type is controlled and server remains available.");

    WriteCaseHeader("TC-N06", "Known STATUS packet route is controlled.");
    await AssertStatusRouteAcceptedAsync(port, authRuntime.SessionRepository);

    ClearTraces(traces);
    WriteCaseHeader("TC-N07", "Login emits Online status.");
    await AssertLoginAndDisconnectEmitStatusAsync(port, authRuntime.SessionRepository, traces);

    WriteCaseHeader("TC-N09", "Multiple clients remain distinct.");
    await AssertTwoClientChatRoutingAsync(port, authRuntime, server, chatMessages, commandResults);

    ClearTraces(traces);
    WriteCaseHeader("TC-N10", "Sequential sends are not interleaved.");
    await AssertSequentialJsonLinesRemainDistinctAsync(port, authRuntime.SessionRepository);

    ClearTraces(traces);
    WriteCaseHeader("TC-N11", "Abrupt client loss does not crash server.");
    await AssertAbruptClientLossDoesNotCrashServerAsync(port, authRuntime.SessionRepository, traces);

    WriteCaseHeader("TC-N12", "Client reconnect behavior is controlled.");
    await AssertClientReconnectBehaviorAsync();

    WriteCaseHeader("TC-N13", "Repeated login is rejected while the machine session is active.");
    await AssertRepeatedLoginRejectedWhileActiveAsync(port, authRuntime.SessionRepository);

    ClearTraces(traces);
    WriteCaseHeader("TC-N14", "Admin UI LOCK/UNLOCK commands and typed ACK results are routed.");
    await AssertAdminUiLockUnlockCommandTraceAsync(port, authRuntime, server, traces, commandResults);

    WriteCaseHeader("TC-N15", "Billing TIMER, expiry LOCK and STATUS resync are routed.");
    await AssertBillingTimerRoutingAsync(port, authRuntime, server);

    ClearTraces(traces);
    WriteCaseHeader("TC-N16", "Top-up request decisions update balance, TIMER and lock state.");
    await AssertTopUpRequestFlowAsync(port, authRuntime, server, traces);

    WriteDiagnostic(
        "PASS: Client -> ServerApp listener -> auth/command/billing/top-up dispatcher -> controlled results -> Client");
}
finally
{
    SqliteConnection.ClearAllPools();

    if (File.Exists(databasePath))
    {
        DeleteTempDatabase(databasePath);
    }
}

static void WriteCaseHeader(string id, string description)
{
    Console.WriteLine();
    Console.WriteLine(id);
    Console.WriteLine($"Test case: {description}");
}

static void WriteCasePass(string id, string message)
{
    _ = id;
    Console.WriteLine($"PASS: {message}");
}

static void WriteDiagnostic(string message)
{
    Console.WriteLine(message);
}

static void ClearTraces(List<NetworkTraceEntry> traces)
{
    lock (traces)
    {
        traces.Clear();
    }
}

static async Task AssertClientConnectsAsync(int port)
{
    using var tcpClient = new TcpClient();
    await tcpClient.ConnectAsync(IPAddress.Loopback, port);
    WriteCasePass("TC-N02", "Connection succeeds without UI or server freeze.");

}

static async Task AssertAdminUiLockUnlockCommandTraceAsync(
    int port,
    AuthRuntime authRuntime,
    TcpJsonLineServer server,
    List<NetworkTraceEntry> traces,
    List<MachineCommandAckResult> commandResults)
{
    // Command feature flow: login client, click admin UI buttons, receive JSON command, then ACK success/error paths.
    string sessionId;

    using (var tcpClient = new TcpClient())
    {
        await tcpClient.ConnectAsync(IPAddress.Loopback, port);
        await using NetworkStream stream = tcpClient.GetStream();
        using var reader = new StreamReader(stream, NetworkProtocol.TextEncoding, leaveOpen: true);
        await using var writer = new StreamWriter(stream, NetworkProtocol.TextEncoding, leaveOpen: true)
        {
            AutoFlush = true
        };

        Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC01");
        object response = await SendLoginOnOpenStreamAsync(reader, writer, loginPacket);
        Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);
        sessionId = resultPacket.TypedPayload.SessionId;

        await WaitForStatusTraceAsync(traces, "PC01", "Online");

        using var mainForm = new ServerApp.MainForm(authRuntime.Machines, server);
        SynchronizationContext.SetSynchronizationContext(null);
        mainForm.ApplyMachineStatusUpdate("PC01", "Online");

        ClickMachineActionButton(mainForm, "btnLockMachine");
        await WaitForCommandTraceAsync(traces, PacketType.LOCK, "PC01").ConfigureAwait(false);
        Packet lockCommand = await AssertCommandReceivedAsync(reader, PacketType.LOCK, "PC01").ConfigureAwait(false);
        await SendCommandAckAsync(
            stream,
            lockCommand,
            "PC02",
            "Success",
            "Wrong machine ACK.").ConfigureAwait(false);
        await WaitForCommandAckErrorTraceAsync(traces, "UNAUTHORIZED_COMMAND").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            lockCommand.RequestId!,
            CommandType.LOCK,
            "PC02",
            isError: true,
            expectedErrorCode: "UNAUTHORIZED_COMMAND").ConfigureAwait(false);

        string unknownRequestId = $"ack-missing-{Guid.NewGuid():N}";
        await SendCommandAckAsync(
            stream,
            lockCommand,
            "PC01",
            "Success",
            "Unknown request ACK.",
            requestIdOverride: unknownRequestId).ConfigureAwait(false);
        await WaitForCommandAckErrorTraceAsync(traces, "ACK_UNKNOWN_REQUEST").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            unknownRequestId,
            CommandType.LOCK,
            "PC01",
            isError: true,
            expectedErrorCode: "ACK_UNKNOWN_REQUEST").ConfigureAwait(false);

        await SendCommandAckAsync(
            stream,
            lockCommand,
            "PC01",
            "Success",
            "Wrong type ACK.",
            ackForOverride: PacketType.UNLOCK.ToString()).ConfigureAwait(false);
        await WaitForCommandAckErrorTraceAsync(traces, "ACK_TYPE_MISMATCH").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            lockCommand.RequestId!,
            CommandType.LOCK,
            "PC01",
            isError: true,
            expectedErrorCode: "ACK_TYPE_MISMATCH").ConfigureAwait(false);

        await SendCommandAckAsync(
            stream,
            lockCommand,
            "PC01",
            "Maybe",
            "Invalid ACK status.").ConfigureAwait(false);
        await WaitForCommandAckErrorTraceAsync(traces, "INVALID_PACKET").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            lockCommand.RequestId!,
            CommandType.LOCK,
            "PC01",
            isError: true,
            expectedErrorCode: "INVALID_PACKET").ConfigureAwait(false);

        await SendCommandAckAsync(stream, lockCommand, "PC01", "Success", "Lock applied.").ConfigureAwait(false);
        await WaitForCommandAckTraceAsync(traces, lockCommand, "PC01", "Success").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            lockCommand.RequestId!,
            CommandType.LOCK,
            "PC01",
            isError: false,
            expectedErrorCode: null).ConfigureAwait(false);
        Console.WriteLine("PASS: admin UI Lock action emits real LOCK JSON command packet and receives typed ACK");

        ClickMachineActionButton(mainForm, "btnUnlockMachine");
        await WaitForCommandTraceAsync(traces, PacketType.UNLOCK, "PC01").ConfigureAwait(false);
        Packet unlockCommand = await AssertCommandReceivedAsync(reader, PacketType.UNLOCK, "PC01").ConfigureAwait(false);
        await SendCommandAckAsync(
            stream,
            unlockCommand,
            "PC02",
            "Success",
            "Wrong machine unlock ACK.").ConfigureAwait(false);
        await WaitForCommandAckErrorTraceAsync(traces, "UNAUTHORIZED_COMMAND").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            unlockCommand.RequestId!,
            CommandType.UNLOCK,
            "PC02",
            isError: true,
            expectedErrorCode: "UNAUTHORIZED_COMMAND").ConfigureAwait(false);

        await SendCommandAckAsync(stream, unlockCommand, "PC01", "Success", "Unlock applied.").ConfigureAwait(false);
        await WaitForCommandAckTraceAsync(traces, unlockCommand, "PC01", "Success").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            unlockCommand.RequestId!,
            CommandType.UNLOCK,
            "PC01",
            isError: false,
            expectedErrorCode: null).ConfigureAwait(false);
        Console.WriteLine("PASS: admin UI Unlock action emits real UNLOCK JSON command packet and receives typed ACK");

        await AssertCommandErrorAsync(server, traces).ConfigureAwait(false);

        lock (traces)
        {
            traces.Clear();
        }

        ClickMachineActionButton(mainForm, "btnLockMachine");
        await WaitForCommandTraceAsync(traces, PacketType.LOCK, "PC01").ConfigureAwait(false);
        Packet disconnectedCommand = await AssertCommandReceivedAsync(reader, PacketType.LOCK, "PC01").ConfigureAwait(false);
        TryShutdown(tcpClient);
        await WaitForCommandResultAsync(
            commandResults,
            disconnectedCommand.RequestId!,
            CommandType.LOCK,
            "PC01",
            isError: true,
            expectedErrorCode: "COMMAND_CLIENT_DISCONNECTED").ConfigureAwait(false);
        Console.WriteLine("PASS: pending command emits typed COMMAND_CLIENT_DISCONNECTED on client disconnect");

        await authRuntime.SessionService.CloseSessionAsync(sessionId);
    }

    await WaitForClosedSessionAsync(authRuntime.SessionRepository, sessionId);
}

static async Task AssertTwoClientChatRoutingAsync(
    int port,
    AuthRuntime authRuntime,
    TcpJsonLineServer server,
    List<(string MachineId, ChatPayload Payload)> chatMessages,
    List<MachineCommandAckResult> commandResults)
{
    string pc01SessionId;
    string pc02SessionId;

    using var pc01Client = new TcpClient();
    using var pc02Client = new TcpClient();
    await pc01Client.ConnectAsync(IPAddress.Loopback, port);
    await pc02Client.ConnectAsync(IPAddress.Loopback, port);

    await using NetworkStream pc01Stream = pc01Client.GetStream();
    await using NetworkStream pc02Stream = pc02Client.GetStream();
    using var pc01Reader = new StreamReader(pc01Stream, NetworkProtocol.TextEncoding, leaveOpen: true);
    using var pc02Reader = new StreamReader(pc02Stream, NetworkProtocol.TextEncoding, leaveOpen: true);
    await using var pc01Writer = new StreamWriter(pc01Stream, NetworkProtocol.TextEncoding, leaveOpen: true)
    {
        AutoFlush = true
    };
    await using var pc02Writer = new StreamWriter(pc02Stream, NetworkProtocol.TextEncoding, leaveOpen: true)
    {
        AutoFlush = true
    };

    Packet<LoginPayload> pc01Login = CreateClientLoginPacket("client01", "PC01", "123");
    Packet<LoginPayload> pc02Login = CreateClientLoginPacket("client02", "PC02", "123");
    pc01SessionId = AssertClientLoginSuccess(pc01Login, await SendLoginOnOpenStreamAsync(pc01Reader, pc01Writer, pc01Login), "client01", "PC01");
    pc02SessionId = AssertClientLoginSuccess(pc02Login, await SendLoginOnOpenStreamAsync(pc02Reader, pc02Writer, pc02Login), "client02", "PC02");

    MachineCommandSendResult pc02Command = await server.SendMachineCommandWithResultAsync(
        "PC02",
        lockMachine: true,
        issuedBy: "NetworkSmoke",
        reason: "Selected-client routing check").ConfigureAwait(false);

    if (!pc02Command.Sent)
    {
        throw new InvalidOperationException($"PC02 command route failed: {pc02Command.ErrorCode} {pc02Command.Message}");
    }

    Packet pc02LockCommand = await AssertCommandReceivedAsync(pc02Reader, PacketType.LOCK, "PC02").ConfigureAwait(false);
    await SendCommandAckAsync(
        pc02Stream,
        pc02LockCommand,
        "PC02",
        "Success",
        "PC02 lock applied.").ConfigureAwait(false);

    MachineCommandSendResult pc02Shutdown = await server.SendMachineCommandWithResultAsync(
        "PC02",
        CommandType.SHUTDOWN,
        issuedBy: "NetworkSmoke",
        reason: "Selected-client shutdown check").ConfigureAwait(false);

    if (!pc02Shutdown.Sent)
    {
        throw new InvalidOperationException($"PC02 shutdown route failed: {pc02Shutdown.ErrorCode} {pc02Shutdown.Message}");
    }

    Packet pc02ShutdownCommand = await AssertCommandReceivedAsync(pc02Reader, PacketType.SHUTDOWN, "PC02").ConfigureAwait(false);
    await SendCommandAckAsync(
        pc02Stream,
        pc02ShutdownCommand,
        "PC02",
        "Success",
        "PC02 shutdown accepted.").ConfigureAwait(false);

    await WaitForCommandResultAsync(
        commandResults,
        pc02ShutdownCommand.RequestId ?? string.Empty,
        CommandType.SHUTDOWN,
        "PC02",
        isError: false,
        expectedErrorCode: null).ConfigureAwait(false);

    MachineChatSendResult sendResult = await server.SendChatAsync(
        "PC01",
        "Server",
        "Hello selected client.").ConfigureAwait(false);

    if (!sendResult.Sent)
    {
        throw new InvalidOperationException($"CHAT send failed: {sendResult.ErrorCode} {sendResult.Message}");
    }

    Packet<ChatPayload> pc01Chat = await ReadChatPacketAsync(pc01Reader, "PC01 admin CHAT").ConfigureAwait(false);
    if (pc01Chat.TypedPayload.Message != "Hello selected client."
        || pc01Chat.Target != "PC01")
    {
        throw new InvalidOperationException("Selected client did not receive the expected admin CHAT.");
    }

    MachineNotificationSendResult notificationResult = await server.SendNotificationAsync(
        "PC01",
        "Maintenance starts in 5 minutes.",
        "Warning").ConfigureAwait(false);

    if (!notificationResult.Sent)
    {
        throw new InvalidOperationException(
            $"NOTIFICATION send failed: {notificationResult.ErrorCode} {notificationResult.Message}");
    }

    Packet<NotificationPayload> pc01Notification =
        await ReadNotificationPacketAsync(pc01Reader, "PC01 admin NOTIFICATION").ConfigureAwait(false);
    if (pc01Notification.TypedPayload.Message != "Maintenance starts in 5 minutes."
        || pc01Notification.TypedPayload.Severity != "Warning"
        || pc01Notification.TypedPayload.Scope != "Direct"
        || pc01Notification.Target != "PC01")
    {
        throw new InvalidOperationException("Selected client did not receive the expected admin NOTIFICATION.");
    }

    MachineNotificationBroadcastResult broadcastResult = await server.BroadcastNotificationAsync(
        "Store closes in 15 minutes.",
        "Info").ConfigureAwait(false);

    if (!broadcastResult.Sent || broadcastResult.TargetCount != 2 || broadcastResult.SentCount != 2)
    {
        throw new InvalidOperationException(
            $"NOTIFICATION broadcast failed: {broadcastResult.ErrorCode} {broadcastResult.Message}");
    }

    Packet<NotificationPayload> pc01Broadcast =
        await ReadNotificationPacketAsync(pc01Reader, "PC01 broadcast NOTIFICATION").ConfigureAwait(false);
    Packet<NotificationPayload> pc02Broadcast =
        await ReadNotificationPacketAsync(pc02Reader, "PC02 broadcast NOTIFICATION").ConfigureAwait(false);

    if (pc01Broadcast.TypedPayload.Message != "Store closes in 15 minutes."
        || pc02Broadcast.TypedPayload.Message != "Store closes in 15 minutes."
        || pc01Broadcast.TypedPayload.Scope != "Broadcast"
        || pc02Broadcast.TypedPayload.Scope != "Broadcast")
    {
        throw new InvalidOperationException("Broadcast NOTIFICATION did not reach all selected active clients.");
    }

    Packet<ChatPayload> reply = PacketFactory.CreateChat(
        source: "PC01",
        target: NetworkProtocol.ServerSource,
        payload: new ChatPayload
        {
            Sender = "PC01",
            Receiver = NetworkProtocol.ServerSource,
            Message = "Client reply."
        },
        requestId: $"chat-{Guid.NewGuid():N}");
    string replyLine = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(reply));
    Console.WriteLine($"CLIENT OUT: {replyLine}");
    await pc01Writer.WriteLineAsync(replyLine).ConfigureAwait(false);
    await WaitForChatMessageAsync(chatMessages, "PC01", "Client reply.").ConfigureAwait(false);

    MachineChatSendResult offlineResult = await server.SendChatAsync(
        "PC99",
        "Server",
        "Offline target").ConfigureAwait(false);

    if (offlineResult.Sent || offlineResult.ErrorCode != "MACHINE_OFFLINE")
    {
        throw new InvalidOperationException("Offline CHAT target did not return MACHINE_OFFLINE.");
    }

    TryShutdown(pc01Client);
    TryShutdown(pc02Client);
    await WaitForClosedSessionAsync(authRuntime.SessionRepository, pc01SessionId).ConfigureAwait(false);
    await WaitForClosedSessionAsync(authRuntime.SessionRepository, pc02SessionId).ConfigureAwait(false);
    WriteCasePass("TC-N09", "PC01 and PC02 keep distinct sessions and selected-client routing stays isolated.");
}

static async Task AssertBillingTimerRoutingAsync(
    int port,
    AuthRuntime authRuntime,
    TcpJsonLineServer server)
{
    var billingService = new NetworkAdminBillingService(
        authRuntime.Billing,
        authRuntime.SessionRepository,
        server);
    server.StatusEmitted += status =>
    {
        if (string.Equals(status.Status, "Online", StringComparison.OrdinalIgnoreCase))
        {
            _ = billingService.SyncMachineAsync(status.MachineId);
        }
    };

    using var pc01Client = new TcpClient();
    using var pc02Client = new TcpClient();
    await pc01Client.ConnectAsync(IPAddress.Loopback, port);
    await pc02Client.ConnectAsync(IPAddress.Loopback, port);

    await using NetworkStream pc01Stream = pc01Client.GetStream();
    await using NetworkStream pc02Stream = pc02Client.GetStream();
    using var pc01Reader = new StreamReader(pc01Stream, NetworkProtocol.TextEncoding, leaveOpen: true);
    using var pc02Reader = new StreamReader(pc02Stream, NetworkProtocol.TextEncoding, leaveOpen: true);
    await using var pc01Writer = new StreamWriter(pc01Stream, NetworkProtocol.TextEncoding, leaveOpen: true)
    {
        AutoFlush = true
    };
    await using var pc02Writer = new StreamWriter(pc02Stream, NetworkProtocol.TextEncoding, leaveOpen: true)
    {
        AutoFlush = true
    };

    Packet<LoginPayload> pc01Login = CreateClientLoginPacket("client01", "PC01", "123");
    Packet<LoginPayload> pc02Login = CreateClientLoginPacket("client02", "PC02", "123");
    string pc01SessionId = AssertClientLoginSuccess(
        pc01Login,
        await SendLoginOnOpenStreamAsync(pc01Reader, pc01Writer, pc01Login),
        "client01",
        "PC01");
    string pc02SessionId = AssertClientLoginSuccess(
        pc02Login,
        await SendLoginOnOpenStreamAsync(pc02Reader, pc02Writer, pc02Login),
        "client02",
        "PC02");

    AdminBillingResult timed = await billingService.StartTimedAsync("PC01", 4);
    if (!timed.IsSuccess)
    {
        throw new InvalidOperationException($"Timed billing failed: {timed.ErrorCode} {timed.Message}");
    }

    Packet<TimerPayload> timedTimer = await ReadTimerPacketAsync(pc01Reader, "PC01 timed billing TIMER");
    if (timedTimer.TypedPayload.RemainingSeconds is null
        || timedTimer.TypedPayload.RemainingSeconds > 300
        || !timedTimer.TypedPayload.IsWarning)
    {
        throw new InvalidOperationException("Timed billing TIMER did not carry 5-minute warning state.");
    }

    AdminBillingResult openEnded = await billingService.StartOpenEndedAsync("PC02");
    if (!openEnded.IsSuccess)
    {
        throw new InvalidOperationException($"Open-ended billing failed: {openEnded.ErrorCode} {openEnded.Message}");
    }

    Packet<TimerPayload> openEndedTimer = await ReadTimerPacketAsync(pc02Reader, "PC02 open-ended TIMER");
    if (openEndedTimer.TypedPayload.ExpiresAt is not null
        || openEndedTimer.TypedPayload.RemainingSeconds is not null
        || openEndedTimer.TypedPayload.AmountVnd < 0)
    {
        throw new InvalidOperationException("Open-ended TIMER did not carry nullable expiry/remaining billing state.");
    }

    AdminBillingResult extended = await billingService.ExtendAsync("PC01", 2);
    if (!extended.IsSuccess)
    {
        throw new InvalidOperationException($"Billing extend failed: {extended.ErrorCode} {extended.Message}");
    }

    Packet<TimerPayload> extendedTimer = await ReadTimerPacketAsync(pc01Reader, "PC01 extended TIMER");
    if (!string.Equals(extendedTimer.TypedPayload.RentalMode, BillingRentalMode.Extend.ToString(), StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Extended TIMER did not mark rentalMode Extend.");
    }

    AdminBillingResult closed = await billingService.CloseAsync("PC01");
    if (!closed.IsSuccess)
    {
        throw new InvalidOperationException($"Billing close failed: {closed.ErrorCode} {closed.Message}");
    }

    Packet<TimerPayload> closedTimer = await ReadTimerPacketAsync(pc01Reader, "PC01 closed TIMER");
    if (!string.Equals(closedTimer.TypedPayload.Status, "Closed", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Closed billing TIMER did not mark status Closed.");
    }

    SessionRecord pc01Session = await authRuntime.SessionRepository.GetActiveByMachineIdAsync("PC01")
        ?? throw new InvalidOperationException("Expected active PC01 auth session for expired billing check.");
    AdminBillingResult expiredOpen = await OpenExpiredBillingAsync(authRuntime, pc01Session);
    if (!expiredOpen.IsSuccess)
    {
        throw new InvalidOperationException($"Expired billing setup failed: {expiredOpen.ErrorCode} {expiredOpen.Message}");
    }

    await billingService.RefreshActiveSessionsAsync();
    Packet<TimerPayload> expiredTimer = await ReadTimerPacketAsync(pc01Reader, "PC01 expired TIMER");
    if (!expiredTimer.TypedPayload.ShouldLockNow || expiredTimer.TypedPayload.RemainingSeconds != 0)
    {
        throw new InvalidOperationException("Expired billing TIMER did not request lock.");
    }

    Packet lockCommand = await AssertCommandReceivedAsync(pc01Reader, PacketType.LOCK, "PC01");
    await SendCommandAckAsync(pc01Stream, lockCommand, "PC01", "Success", "Billing expiry lock applied.");

    Packet<TimerPayload> pc02RefreshTimer = await ReadTimerPacketAsync(pc02Reader, "PC02 refresh TIMER");
    if (pc02RefreshTimer.TypedPayload.ExpiresAt is not null)
    {
        throw new InvalidOperationException("Refresh TIMER did not preserve PC02 open-ended session.");
    }

    await SendStatusAsync(pc02Writer, "PC02", pc02SessionId);
    object firstStatusResponse = await ReadAnyPacketAsync(pc02Reader, "PC02 STATUS response");
    object secondStatusResponse = await ReadAnyPacketAsync(pc02Reader, "PC02 STATUS billing response");
    if (firstStatusResponse is not Packet<AckPayload> && secondStatusResponse is not Packet<AckPayload>)
    {
        throw new InvalidOperationException("STATUS did not return ACK while resyncing billing.");
    }

    Packet<TimerPayload> resyncTimer =
        firstStatusResponse as Packet<TimerPayload>
        ?? secondStatusResponse as Packet<TimerPayload>
        ?? throw new InvalidOperationException("STATUS did not trigger billing TIMER resync.");
    if (resyncTimer.TypedPayload.ExpiresAt is not null
        || !string.Equals(resyncTimer.TypedPayload.RentalMode, BillingRentalMode.OpenEnded.ToString(), StringComparison.Ordinal))
    {
        throw new InvalidOperationException("STATUS-triggered billing resync did not preserve open-ended session.");
    }

    await billingService.CloseAsync("PC01");
    await billingService.CloseAsync("PC02");
    TryShutdown(pc01Client);
    TryShutdown(pc02Client);
    await WaitForClosedSessionAsync(authRuntime.SessionRepository, pc01SessionId);
    await WaitForClosedSessionAsync(authRuntime.SessionRepository, pc02SessionId);
    Console.WriteLine("PASS: billing TIMER route supports timed warning, open-ended, extend/close, expiry LOCK and STATUS resync");
}

static async Task AssertTopUpRequestFlowAsync(
    int port,
    AuthRuntime authRuntime,
    TcpJsonLineServer server,
    List<NetworkTraceEntry> traces)
{
    const string customerId = "topup-customer-01";
    const string username = "topup01";
    const string machineId = "PC01";
    const long topUpAmount = 10_000;

    await authRuntime.Customers.AddAsync(new CustomerRecord(
        customerId,
        "Top",
        "Up",
        "0900000001",
        "ID-TOPUP-01",
        "2000-01-01",
        username,
        "123",
        AccountBalance: 0));

    if (!ServerApp.MainForm.TryParseTopUpRequest(
            new AdminChatMessage(machineId, machineId, $"{machineId} yêu cầu nạp {topUpAmount:N0} VND", DateTimeOffset.Now),
            out long parsedAmount)
        || parsedAmount != topUpAmount)
    {
        throw new InvalidOperationException("Top-up parser did not accept the expected client request format.");
    }

    if (ServerApp.MainForm.TryParseTopUpRequest(
            new AdminChatMessage("PC00", "PC00", $"PC00 yêu cầu nạp {topUpAmount} VND", DateTimeOffset.Now),
            out _))
    {
        throw new InvalidOperationException("Top-up parser must ignore server machine PC00.");
    }

    if (ServerApp.MainForm.TryParseTopUpRequest(
            new AdminChatMessage(machineId, machineId, $"PC02 yêu cầu nạp {topUpAmount} VND", DateTimeOffset.Now),
            out _))
    {
        throw new InvalidOperationException("Top-up parser must ignore requests whose message machine differs from sender.");
    }

    if (ServerApp.MainForm.TryParseTopUpRequest(
            new AdminChatMessage(machineId, machineId, "Tin nhan chat binh thuong", DateTimeOffset.Now),
            out _))
    {
        throw new InvalidOperationException("Top-up parser must ignore normal chat messages.");
    }

    var billingService = new NetworkAdminBillingService(
        authRuntime.Billing,
        authRuntime.SessionRepository,
        server,
        authRuntime.Customers);

    using var pc01Client = new TcpClient();
    await pc01Client.ConnectAsync(IPAddress.Loopback, port);
    await using NetworkStream pc01Stream = pc01Client.GetStream();
    using var pc01Reader = new StreamReader(pc01Stream, NetworkProtocol.TextEncoding, leaveOpen: true);
    await using var pc01Writer = new StreamWriter(pc01Stream, NetworkProtocol.TextEncoding, leaveOpen: true)
    {
        AutoFlush = true
    };

    Packet<LoginPayload> login = CreateClientLoginPacket(username, machineId, "123");
    string sessionId = AssertClientLoginSuccess(
        login,
        await SendLoginOnOpenStreamAsync(pc01Reader, pc01Writer, login),
        username,
        machineId);

    AdminBillingResult opened = await billingService.StartOpenEndedAsync(machineId);
    if (!opened.IsSuccess)
    {
        throw new InvalidOperationException($"Top-up billing setup failed: {opened.ErrorCode} {opened.Message}");
    }

    Packet<TimerPayload> emptyBalanceTimer = await ReadTimerPacketAsync(pc01Reader, "PC01 empty-balance TIMER");
    if (!emptyBalanceTimer.TypedPayload.ShouldLockNow
        || emptyBalanceTimer.TypedPayload.RemainingBalanceVnd != 0)
    {
        throw new InvalidOperationException("Empty-balance TIMER did not request lock before top-up.");
    }

    Packet initialLock = await AssertCommandReceivedAsync(pc01Reader, PacketType.LOCK, machineId);
    await SendCommandAckAsync(pc01Stream, initialLock, machineId, "Success", "Empty balance lock applied.");

    using var mainForm = new ServerApp.MainForm(authRuntime.Machines, server, billingService, authRuntime.Customers);
    SynchronizationContext.SetSynchronizationContext(null);

    await mainForm.HandleTopUpRequestDecisionAsync(machineId, 5_000, DialogResult.No);
    Packet<TimerPayload> rejectedEmptyTimer = await ReadTimerPacketAsync(pc01Reader, "PC01 rejected empty-balance TIMER");
    if (!rejectedEmptyTimer.TypedPayload.ShouldLockNow
        || rejectedEmptyTimer.TypedPayload.RemainingUsageSeconds != 0)
    {
        throw new InvalidOperationException("Rejected empty-balance top-up should keep billing countdown depleted.");
    }

    Packet rejectedEmptyLock = await AssertCommandReceivedAsync(pc01Reader, PacketType.LOCK, machineId);
    await SendCommandAckAsync(pc01Stream, rejectedEmptyLock, machineId, "Success", "Rejected empty-balance lock applied.");

    await mainForm.HandleTopUpRequestDecisionAsync(machineId, topUpAmount, DialogResult.Yes);

    CustomerRecord updatedCustomer = await authRuntime.Customers.GetByIdAsync(customerId)
        ?? throw new InvalidOperationException("Top-up customer was not found after confirm.");
    if (updatedCustomer.AccountBalance != topUpAmount)
    {
        throw new InvalidOperationException(
            $"Top-up confirm should add {topUpAmount}, got balance {updatedCustomer.AccountBalance}.");
    }

    Packet<TimerPayload> topUpTimer = await ReadTimerPacketAsync(pc01Reader, "PC01 top-up TIMER");
    if (topUpTimer.TypedPayload.ShouldLockNow
        || topUpTimer.TypedPayload.TotalBalanceVnd != topUpAmount
        || topUpTimer.TypedPayload.RemainingBalanceVnd is null
        || topUpTimer.TypedPayload.RemainingBalanceVnd <= 0
        || topUpTimer.TypedPayload.RemainingUsageSeconds is null
        || topUpTimer.TypedPayload.RemainingUsageSeconds <= 0)
    {
        throw new InvalidOperationException("Top-up TIMER did not expose updated balance/time state.");
    }

    Packet unlockCommand = await AssertCommandReceivedAsync(pc01Reader, PacketType.UNLOCK, machineId);
    await SendCommandAckAsync(pc01Stream, unlockCommand, machineId, "Success", "Top-up unlock applied.");

    int lockCountBeforePositiveReject = CountCommandTraces(traces, PacketType.LOCK, machineId);
    await mainForm.HandleTopUpRequestDecisionAsync(machineId, 5_000, DialogResult.No);
    Packet<TimerPayload> rejectedPositiveTimer = await ReadTimerPacketAsync(pc01Reader, "PC01 rejected positive-balance TIMER");
    if (rejectedPositiveTimer.TypedPayload.ShouldLockNow
        || rejectedPositiveTimer.TypedPayload.RemainingUsageSeconds is null
        || rejectedPositiveTimer.TypedPayload.RemainingUsageSeconds <= 0)
    {
        throw new InvalidOperationException("Rejected positive-balance top-up should keep money-based countdown available.");
    }

    await AssertCommandTraceCountUnchangedAsync(
        traces,
        PacketType.LOCK,
        machineId,
        lockCountBeforePositiveReject,
        "Rejected positive-balance top-up must not send LOCK.");

    pc01Client.Client.Dispose();
    await WaitForClosedSessionAsync(authRuntime.SessionRepository, sessionId);
    Console.WriteLine("PASS: top-up request parser, confirm balance update/unlock and money-based reject lock flow");
}

static async Task<AdminBillingResult> OpenExpiredBillingAsync(AuthRuntime authRuntime, SessionRecord session)
{
    var opened = await authRuntime.Billing.OpenSessionAsync(
        new ServerApp.Billing.Models.BillingSessionRequest(
            session.Id,
            session.UserId,
            session.Username,
            session.MachineId ?? string.Empty,
            BillingRentalMode.Timed,
            DateTimeOffset.UtcNow.AddMinutes(-10),
            10_000,
            DateTimeOffset.UtcNow.AddMinutes(-1)));

    if (opened.IsFailure || opened.Session is null)
    {
        return AdminBillingResult.ControlledError(
            session.MachineId ?? string.Empty,
            opened.ErrorCode ?? "BILLING_OPEN_FAILED",
            opened.Message);
    }

    return AdminBillingResult.Success(
        session.MachineId ?? string.Empty,
        opened.Message,
        new TimerPayload
        {
            MachineId = session.MachineId ?? string.Empty,
            RentalMode = BillingRentalMode.Timed.ToString(),
            StartedAt = opened.Session.Session.StartedAtUtc,
            ExpiresAt = opened.Session.Session.ExpiresAtUtc,
            RatePerHour = opened.Session.Session.RatePerHour,
            ChargedMinutes = opened.Session.Calculation.ChargedMinutes,
            AmountVnd = opened.Session.Calculation.AmountVnd,
            Status = opened.Session.Session.State.ToString()
        });
}

static Packet<LoginPayload> CreateClientLoginPacket(string username, string machineId, string password)
{
    return PacketFactory.CreateLogin(
        source: machineId,
        target: NetworkProtocol.ServerSource,
        payload: new LoginPayload
        {
            Username = username,
            Password = password,
            Role = "Client",
            MachineId = machineId
        },
        requestId: $"roundtrip-{Guid.NewGuid():N}");
}

static string AssertClientLoginSuccess(
    Packet<LoginPayload> loginPacket,
    object response,
    string username,
    string machineId)
{
    var resultPacket = response as Packet<LoginResultPayload>
        ?? throw new InvalidOperationException($"Client expected LOGIN success packet for {machineId}.");

    if (resultPacket.Success != true
        || resultPacket.TypedPayload.Username != username
        || resultPacket.TypedPayload.MachineId != machineId
        || string.IsNullOrWhiteSpace(resultPacket.TypedPayload.SessionId))
    {
        throw new InvalidOperationException($"LOGIN success payload did not match {username}/{machineId}.");
    }

    AssertMatchingRequestId(loginPacket, resultPacket);
    return resultPacket.TypedPayload.SessionId;
}

static async Task<Packet<ChatPayload>> ReadChatPacketAsync(StreamReader reader, string description)
{
    string inboundLine = await ReadRequiredLineAsync(reader, description).ConfigureAwait(false);
    Console.WriteLine($"CLIENT IN : {inboundLine}");

    return JsonHelper.DeserializePacket(inboundLine) as Packet<ChatPayload>
        ?? throw new InvalidOperationException($"Client expected CHAT packet for {description}.");
}

static Task<Packet<TimerPayload>> ReadTimerPacketAsync(StreamReader reader, string description)
    => ReadPacketAsync<TimerPayload>(reader, description);

static Task<Packet<NotificationPayload>> ReadNotificationPacketAsync(StreamReader reader, string description)
    => ReadPacketAsync<NotificationPayload>(reader, description);

static async Task<Packet<TPayload>> ReadPacketAsync<TPayload>(StreamReader reader, string description)
    where TPayload : class
{
    DateTime deadline = DateTime.UtcNow.AddSeconds(5);

    while (DateTime.UtcNow < deadline)
    {
        string inboundLine = await ReadRequiredLineAsync(reader, description).ConfigureAwait(false);
        Console.WriteLine($"CLIENT IN : {inboundLine}");
        object packet = JsonHelper.DeserializePacket(inboundLine);
        if (packet is Packet<TPayload> typedPacket)
        {
            return typedPacket;
        }
    }

    throw new InvalidOperationException($"Client expected {typeof(TPayload).Name} packet for {description}.");
}

static async Task<object> ReadAnyPacketAsync(StreamReader reader, string description)
{
    string inboundLine = await ReadRequiredLineAsync(reader, description).ConfigureAwait(false);
    Console.WriteLine($"CLIENT IN : {inboundLine}");
    return JsonHelper.DeserializePacket(inboundLine);
}

static async Task SendStatusAsync(StreamWriter writer, string machineId, string sessionId)
{
    var statusPacket = PacketFactory.CreateStatus(
        source: machineId,
        target: NetworkProtocol.ServerSource,
        payload: new StatusPayload
        {
            MachineId = machineId,
            SessionId = sessionId,
            MachineName = machineId,
            Status = "Online",
            IpAddress = IPAddress.Loopback.ToString(),
            LastSeen = DateTime.UtcNow
        },
        requestId: $"status-{Guid.NewGuid():N}");

    string outboundLine = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(statusPacket));
    Console.WriteLine($"CLIENT OUT: {outboundLine}");
    await writer.WriteLineAsync(outboundLine).ConfigureAwait(false);
}

static async Task WaitForChatMessageAsync(
    List<(string MachineId, ChatPayload Payload)> chatMessages,
    string machineId,
    string expectedMessage)
{
    DateTime deadline = DateTime.UtcNow.AddSeconds(5);

    while (DateTime.UtcNow < deadline)
    {
        lock (chatMessages)
        {
            if (chatMessages.Any(item =>
                    string.Equals(item.MachineId, machineId, StringComparison.OrdinalIgnoreCase)
                    && item.Payload.Message == expectedMessage))
            {
                return;
            }
        }

        await Task.Delay(25).ConfigureAwait(false);
    }

    throw new InvalidOperationException($"CHAT reply from {machineId} was not emitted.");
}

static async Task AssertStatusRouteAcceptedAsync(int port, ISessionRepository sessions)
{
    string sessionId;

    using (var tcpClient = new TcpClient())
    {
        await tcpClient.ConnectAsync(IPAddress.Loopback, port);
        await using NetworkStream stream = tcpClient.GetStream();
        using var reader = new StreamReader(stream, NetworkProtocol.TextEncoding, leaveOpen: true);
        await using var writer = new StreamWriter(stream, NetworkProtocol.TextEncoding, leaveOpen: true)
        {
            AutoFlush = true
        };

        Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC01");
        object response = await SendLoginOnOpenStreamAsync(reader, writer, loginPacket);
        Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);
        sessionId = resultPacket.TypedPayload.SessionId;

        var statusPacket = PacketFactory.CreateStatus(
            source: "PC01",
            target: NetworkProtocol.ServerSource,
            payload: new StatusPayload
            {
                MachineId = "PC01",
                SessionId = sessionId,
                MachineName = "PC01",
                Status = "Online",
                IpAddress = IPAddress.Loopback.ToString(),
                LastSeen = DateTime.UtcNow
            },
            requestId: $"status-{Guid.NewGuid():N}");

        string outboundLine = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(statusPacket));
        Console.WriteLine($"CLIENT OUT: {outboundLine}");
        await writer.WriteLineAsync(outboundLine);

        string inboundLine = await ReadRequiredLineAsync(reader, "STATUS ACK");
        Console.WriteLine($"CLIENT IN : {inboundLine}");

        var ackPacket = JsonHelper.DeserializePacket(inboundLine) as Packet<AckPayload>
            ?? throw new InvalidOperationException("Client expected STATUS ACK packet.");

        if (ackPacket.TypedPayload.AckFor != PacketType.STATUS.ToString()
            || ackPacket.TypedPayload.Status != "Accepted"
            || !string.Equals(ackPacket.TypedPayload.MachineId, "PC01", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("STATUS route did not return an Accepted ACK for PC01.");
        }

        AssertMatchingRequestId(statusPacket, ackPacket);
        WriteCasePass("TC-N06", "Authenticated STATUS route returns Accepted ACK.");
        TryShutdown(tcpClient);
    }

    await WaitForClosedSessionAsync(sessions, sessionId);
}

static async Task AssertSequentialJsonLinesRemainDistinctAsync(int port, ISessionRepository sessions)
{
    string sessionId;

    using (var tcpClient = new TcpClient())
    {
        await tcpClient.ConnectAsync(IPAddress.Loopback, port);
        await using NetworkStream stream = tcpClient.GetStream();
        using var reader = new StreamReader(stream, NetworkProtocol.TextEncoding, leaveOpen: true);
        await using var writer = new StreamWriter(stream, NetworkProtocol.TextEncoding, leaveOpen: true)
        {
            AutoFlush = true
        };

        Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC01");
        object response = await SendLoginOnOpenStreamAsync(reader, writer, loginPacket);
        Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);
        sessionId = resultPacket.TypedPayload.SessionId;

        List<Packet<StatusPayload>> statusPackets = [];
        for (int index = 0; index < 5; index++)
        {
            statusPackets.Add(PacketFactory.CreateStatus(
                source: "PC01",
                target: NetworkProtocol.ServerSource,
                payload: new StatusPayload
                {
                    MachineId = "PC01",
                    SessionId = sessionId,
                    MachineName = "PC01",
                    Status = "Online",
                    IpAddress = IPAddress.Loopback.ToString(),
                    LastSeen = DateTime.UtcNow.AddMilliseconds(index)
                },
                requestId: $"status-seq-{index}-{Guid.NewGuid():N}"));
        }

        foreach (Packet<StatusPayload> statusPacket in statusPackets)
        {
            string outboundLine = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(statusPacket));
            Console.WriteLine($"CLIENT OUT: {outboundLine}");
            await writer.WriteLineAsync(outboundLine).ConfigureAwait(false);
        }

        HashSet<string> expectedRequestIds = statusPackets
            .Select(packet => packet.RequestId ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        for (int index = 0; index < statusPackets.Count; index++)
        {
            string inboundLine = await ReadRequiredLineAsync(reader, $"sequential STATUS ACK {index + 1}");
            Console.WriteLine($"CLIENT IN : {inboundLine}");

            var ackPacket = JsonHelper.DeserializePacket(inboundLine) as Packet<AckPayload>
                ?? throw new InvalidOperationException("Sequential send response was not one complete ACK packet.");

            if (!expectedRequestIds.Remove(ackPacket.RequestId ?? string.Empty)
                || ackPacket.TypedPayload.AckFor != PacketType.STATUS.ToString()
                || ackPacket.TypedPayload.Status != "Accepted")
            {
                throw new InvalidOperationException("Sequential STATUS ACK did not match a single sent JSON-line packet.");
            }
        }

        if (expectedRequestIds.Count != 0)
        {
            throw new InvalidOperationException("Sequential STATUS send lost one or more JSON-line responses.");
        }

        TryShutdown(tcpClient);
    }

    await WaitForClosedSessionAsync(sessions, sessionId);
    WriteCasePass("TC-N10", "Rapid JSON-line sends are received as complete, distinct packets.");
}

static async Task AssertAbruptClientLossDoesNotCrashServerAsync(
    int port,
    ISessionRepository sessions,
    List<NetworkTraceEntry> traces)
{
    string sessionId;

    using (var tcpClient = new TcpClient())
    {
        await tcpClient.ConnectAsync(IPAddress.Loopback, port);
        await using NetworkStream stream = tcpClient.GetStream();

        Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC01");
        object response = await SendLoginOnStreamAsync(stream, loginPacket);
        Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);
        sessionId = resultPacket.TypedPayload.SessionId;

        await WaitForStatusTraceAsync(traces, "PC01", "Online");
        TryShutdown(tcpClient);
    }

    await WaitForClosedSessionAsync(sessions, sessionId);
    await WaitForStatusTraceAsync(traces, "PC01", "Offline");
    await AssertLoginSuccessAsync(port, sessions, writePass: false);
    WriteCasePass("TC-N11", "Server closes the lost session and accepts a new login after abrupt client loss.");
}

static async Task AssertClientReconnectBehaviorAsync()
{
    TcpListener? listener = null;

    using var connection = new TcpClientConnection
    {
        ReconnectDelay = TimeSpan.FromMilliseconds(100)
    };

    int connectedCount = 0;
    int disconnectedCount = 0;
    int reconnectFailedCount = 0;

    connection.Connected += () => Interlocked.Increment(ref connectedCount);
    connection.Disconnected += () => Interlocked.Increment(ref disconnectedCount);
    connection.ReconnectFailed += _ => Interlocked.Increment(ref reconnectFailedCount);
    connection.EnableAutoReconnect();

    try
    {
        listener = CreateLoopbackListener(port: 0);
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task<TcpClient> firstAcceptTask = listener.AcceptTcpClientAsync();

        await connection.ConnectAsync(IPAddress.Loopback.ToString(), port).ConfigureAwait(false);
        using TcpClient firstAccepted = await WaitForTaskAsync(firstAcceptTask, "initial reconnect-test accept")
            .ConfigureAwait(false);

        firstAccepted.Close();
        listener.Stop();
        listener = null;

        await WaitForConditionAsync(
            () => Volatile.Read(ref disconnectedCount) >= 1,
            "client disconnect event after server drop").ConfigureAwait(false);
        await WaitForConditionAsync(
            () => Volatile.Read(ref reconnectFailedCount) >= 1,
            "ReconnectFailed event while server is down",
            TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        listener = CreateLoopbackListener(port);
        Task<TcpClient> secondAcceptTask = listener.AcceptTcpClientAsync();

        await WaitForConditionAsync(
            () => Volatile.Read(ref connectedCount) >= 2,
            "client reconnect after server restore",
            TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        using TcpClient secondAccepted = await WaitForTaskAsync(secondAcceptTask, "restored reconnect-test accept")
            .ConfigureAwait(false);

        connection.Disconnect();
        WriteCasePass("TC-N12", "Client waits ReconnectDelay, raises ReconnectFailed, and reconnects after server restore.");
    }
    finally
    {
        connection.Disconnect();
        listener?.Stop();
    }
}

static TcpListener CreateLoopbackListener(int port)
{
    var listener = new TcpListener(IPAddress.Loopback, port);
    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    listener.Start();
    return listener;
}

static async Task<T> WaitForTaskAsync<T>(Task<T> task, string description, TimeSpan? timeout = null)
{
    Task completedTask = await Task.WhenAny(task, Task.Delay(timeout ?? TimeSpan.FromSeconds(5))).ConfigureAwait(false);
    if (completedTask != task)
    {
        throw new TimeoutException($"Timed out waiting for {description}.");
    }

    return await task.ConfigureAwait(false);
}

static async Task WaitForConditionAsync(Func<bool> condition, string description, TimeSpan? timeout = null)
{
    DateTime deadlineUtc = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(2));

    while (DateTime.UtcNow < deadlineUtc)
    {
        if (condition())
        {
            return;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25)).ConfigureAwait(false);
    }

    throw new TimeoutException($"Timed out waiting for {description}.");
}

static void ClickMachineActionButton(ServerApp.MainForm mainForm, string buttonName)
{
    // Exercise the same button Click event path the real admin UI uses, without showing a WinForms window.
    SynchronizationContext.SetSynchronizationContext(null);

    FieldInfo field = typeof(ServerApp.MainForm)
        .GetField(buttonName, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"Could not find MainForm.{buttonName}.");

    if (field.GetValue(mainForm) is not Button button)
    {
        throw new InvalidOperationException($"MainForm.{buttonName} is not a Button.");
    }

    MethodInfo onClick = typeof(Button)
        .GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Could not find Button.OnClick.");

    onClick.Invoke(button, [EventArgs.Empty]);
}

static void DeleteTempDatabase(string databasePath)
{
    for (int attempt = 0; attempt < 5; attempt++)
    {
        try
        {
            File.Delete(databasePath);
            return;
        }
        catch (IOException) when (attempt < 4)
        {
            Thread.Sleep(100);
            SqliteConnection.ClearAllPools();
        }
    }
}

static async Task AssertLoginSuccessAsync(
    int port,
    ISessionRepository sessions,
    bool writePass = true)
{
    Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC01");
    object response = await SendLoginAsync(port, loginPacket);
    Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);

    if (writePass)
    {
        WriteCasePass("TC-N03", "Response has matching requestId, success:true, and LoginResultPayload.");
    }

    await WaitForClosedSessionAsync(sessions, resultPacket.TypedPayload.SessionId);
}

static Packet<LoginResultPayload> AssertLoginSuccessResponse(Packet<LoginPayload> loginPacket, object response)
{
    var resultPacket = response as Packet<LoginResultPayload>
        ?? throw new InvalidOperationException("Client expected LOGIN success packet.");

    if (resultPacket.Success != true)
    {
        throw new InvalidOperationException("LOGIN success response must set success to true.");
    }

    if (resultPacket.TypedPayload.Username != "client01"
        || resultPacket.TypedPayload.MachineId != "PC01"
        || string.IsNullOrWhiteSpace(resultPacket.TypedPayload.SessionId))
    {
        throw new InvalidOperationException("LOGIN success payload did not match the authenticated account.");
    }

    AssertMatchingRequestId(loginPacket, resultPacket);
    return resultPacket;
}

static async Task AssertRepeatedLoginRejectedWhileActiveAsync(int port, ISessionRepository sessions)
{
    string sessionId;

    using (var tcpClient = new TcpClient())
    {
        await tcpClient.ConnectAsync(IPAddress.Loopback, port);
        await using NetworkStream stream = tcpClient.GetStream();

        Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC01");
        object response = await SendLoginOnStreamAsync(stream, loginPacket);
        Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);
        sessionId = resultPacket.TypedPayload.SessionId;

        await AssertLoginFailureAsync(
            port,
            password: "123",
            machineId: "PC01",
            expectedErrorCode: "MACHINE_ALREADY_ACTIVE");
    }

    await WaitForClosedSessionAsync(sessions, sessionId);
    await AssertLoginSuccessAsync(port, sessions, writePass: false);
    Console.WriteLine("PASS: repeated LOGIN is rejected while active and succeeds after disconnect");
}

static async Task AssertLoginAndDisconnectEmitStatusAsync(
    int port,
    ISessionRepository sessions,
    List<NetworkTraceEntry> traces)
{
    string sessionId;

    using (var tcpClient = new TcpClient())
    {
        await tcpClient.ConnectAsync(IPAddress.Loopback, port);
        await using NetworkStream stream = tcpClient.GetStream();

        Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC01");
        object response = await SendLoginOnStreamAsync(stream, loginPacket);
        Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);
        sessionId = resultPacket.TypedPayload.SessionId;

        await WaitForStatusTraceAsync(traces, "PC01", "Online");
        WriteCasePass("TC-N07", "Server emits STATUS with machineId=PC01 and status=Online.");
    }

    WriteCaseHeader("TC-N08", "Disconnect emits Offline status.");
    await WaitForClosedSessionAsync(sessions, sessionId);
    await WaitForStatusTraceAsync(traces, "PC01", "Offline");
    WriteCasePass("TC-N08", "Server emits STATUS with machineId=PC01 and status=Offline.");
}

static async Task AssertLoginFailureAsync(
    int port,
    string password,
    string machineId,
    string expectedErrorCode)
{
    Packet<LoginPayload> loginPacket = CreateLoginPacket(password, machineId);
    object response = await SendLoginAsync(port, loginPacket);

    var resultPacket = response as Packet<EmptyPayload>
        ?? throw new InvalidOperationException("Client expected LOGIN failure packet.");

    if (resultPacket.Success != false)
    {
        throw new InvalidOperationException("LOGIN failure response must set success to false.");
    }

    if (resultPacket.Error?.Code != expectedErrorCode)
    {
        throw new InvalidOperationException(
            $"LOGIN error code was {resultPacket.Error?.Code}, expected {expectedErrorCode}.");
    }

    AssertMatchingRequestId(loginPacket, resultPacket);
    Console.WriteLine($"PASS: rejected LOGIN returns {expectedErrorCode}");
}

static Packet<LoginPayload> CreateLoginPacket(string password, string machineId)
{
    return PacketFactory.CreateLogin(
        source: "PC01",
        target: NetworkProtocol.ServerSource,
        payload: new LoginPayload
        {
            Username = "client01",
            Password = password,
            Role = "Client",
            MachineId = machineId
        },
        requestId: $"roundtrip-{Guid.NewGuid():N}");
}

static async Task<object> SendLoginAsync(int port, Packet<LoginPayload> loginPacket)
{
    using var tcpClient = new TcpClient();
    await tcpClient.ConnectAsync(IPAddress.Loopback, port);

    await using NetworkStream stream = tcpClient.GetStream();
    return await SendLoginOnStreamAsync(stream, loginPacket);
}

static async Task<object> SendLoginOnStreamAsync(NetworkStream stream, Packet<LoginPayload> loginPacket)
{
    using var reader = new StreamReader(stream, NetworkProtocol.TextEncoding, leaveOpen: true);
    await using var writer = new StreamWriter(stream, NetworkProtocol.TextEncoding, leaveOpen: true)
    {
        AutoFlush = true
    };

    return await SendLoginOnOpenStreamAsync(reader, writer, loginPacket);
}

static async Task<object> SendLoginOnOpenStreamAsync(
    StreamReader reader,
    StreamWriter writer,
    Packet<LoginPayload> loginPacket)
{
    string outboundLine = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(loginPacket));
    Console.WriteLine($"CLIENT OUT: {outboundLine}");

    await writer.WriteLineAsync(outboundLine);

    string inboundLine = await ReadRequiredLineAsync(reader, "LOGIN response");

    Console.WriteLine($"CLIENT IN : {inboundLine}");
    return JsonHelper.DeserializePacket(inboundLine);
}

static async Task AssertRejectedLineDoesNotStopServerAsync(
    int port,
    ISessionRepository sessions,
    string outboundLine,
    string scenario,
    string testCaseId,
    string passMessage)
{
    using (var tcpClient = new TcpClient())
    {
        await tcpClient.ConnectAsync(IPAddress.Loopback, port);

        await using NetworkStream stream = tcpClient.GetStream();
        using var reader = new StreamReader(stream, NetworkProtocol.TextEncoding, leaveOpen: true);
        await using var writer = new StreamWriter(stream, NetworkProtocol.TextEncoding, leaveOpen: true)
        {
            AutoFlush = true
        };

        Console.WriteLine($"CLIENT OUT: {outboundLine}");
        await writer.WriteLineAsync(outboundLine);

        string? inboundLine = await ReadOptionalLineAsync(reader, scenario, TimeSpan.FromSeconds(2));

        if (inboundLine is not null)
        {
            throw new InvalidOperationException(
                $"Client expected a controlled disconnect for {scenario}, but received: {inboundLine}");
        }
    }

    await AssertLoginSuccessAsync(port, sessions, writePass: false);
    WriteCasePass(testCaseId, passMessage);
}

static async Task WaitForClosedSessionAsync(ISessionRepository sessions, string sessionId)
{
    DateTime deadlineUtc = DateTime.UtcNow.AddSeconds(2);

    while (DateTime.UtcNow < deadlineUtc)
    {
        var readTask = sessions.GetByIdAsync(sessionId);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(1));
        Task completedTask = await Task.WhenAny(readTask, timeoutTask);

        if (completedTask != readTask)
        {
            throw new TimeoutException($"Timed out reading session {sessionId}.");
        }

        var session = await readTask;

        if (session?.State == ServerApp.Auth.Models.SessionState.Closed)
        {
            Console.WriteLine($"PASS: disconnected socket closes session {sessionId}");
            return;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25));
    }

    throw new InvalidOperationException($"Session {sessionId} remained active after socket disconnect.");
}

static async Task<Packet> AssertCommandReceivedAsync(StreamReader reader, PacketType commandType, string machineId)
{
    string inboundLine = await ReadRequiredLineAsync(reader, $"{commandType} command");
    Console.WriteLine($"CLIENT IN : {inboundLine}");

    object packet = JsonHelper.DeserializePacket(inboundLine);
    if (commandType == PacketType.LOCK
        && packet is Packet<LockPayload> lockPacket
        && lockPacket.Type == PacketType.LOCK
        && string.Equals(lockPacket.TypedPayload.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
    {
        return lockPacket;
    }

    if (commandType == PacketType.UNLOCK
        && packet is Packet<UnlockPayload> unlockPacket
        && unlockPacket.Type == PacketType.UNLOCK
        && string.Equals(unlockPacket.TypedPayload.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
    {
        return unlockPacket;
    }

    if (commandType == PacketType.SHUTDOWN
        && packet is Packet<ShutdownPayload> shutdownPacket
        && shutdownPacket.Type == PacketType.SHUTDOWN
        && string.Equals(shutdownPacket.TypedPayload.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
    {
        return shutdownPacket;
    }

    throw new InvalidOperationException($"Client did not receive expected {commandType} command for {machineId}.");
}

static Task SendCommandAckAsync(
    NetworkStream stream,
    Packet commandPacket,
    string machineId,
    string status,
    string message,
    string? requestIdOverride = null,
    string? ackForOverride = null)
{
    var ackPacket = PacketFactory.CreateAck(
        source: machineId,
        target: NetworkProtocol.ServerSource,
        payload: new AckPayload
        {
            MachineId = machineId,
            AckFor = ackForOverride ?? commandPacket.Type.ToString(),
            Status = status,
            Message = message
        },
        requestId: requestIdOverride ?? commandPacket.RequestId);

    string outboundLine = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(ackPacket));
    Console.WriteLine($"CLIENT OUT: {outboundLine}");
    byte[] bytes = NetworkProtocol.TextEncoding.GetBytes(outboundLine + Environment.NewLine);
    _ = Task.Run(() =>
    {
        try
        {
            stream.Write(bytes);
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    });

    return Task.CompletedTask;
}

static async Task WaitForCommandResultAsync(
    List<MachineCommandAckResult> commandResults,
    string requestId,
    CommandType command,
    string machineId,
    bool isError,
    string? expectedErrorCode)
{
    DateTime deadlineUtc = DateTime.UtcNow.AddSeconds(2);

    while (DateTime.UtcNow < deadlineUtc)
    {
        MachineCommandAckResult[] snapshot;
        lock (commandResults)
        {
            snapshot = [.. commandResults];
        }

        foreach (MachineCommandAckResult result in snapshot)
        {
            if (result.RequestId == requestId
                && result.Command == command
                && string.Equals(result.MachineId, machineId, StringComparison.OrdinalIgnoreCase)
                && result.IsError == isError
                && string.Equals(result.ErrorCode, expectedErrorCode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25)).ConfigureAwait(false);
    }

    throw new InvalidOperationException(
        $"Command result {command}/{machineId}/{requestId}/{expectedErrorCode ?? "OK"} was not emitted.");
}

static async Task AssertCommandErrorAsync(TcpJsonLineServer server, List<NetworkTraceEntry> traces)
{
    // Send-time errors are deterministic before any ACK exists.
    MachineCommandSendResult invalidMachineResult = await server.SendMachineCommandWithResultAsync(
        " ",
        lockMachine: true,
        issuedBy: "NetworkSmokeTest",
        reason: "invalid machine command test");

    if (invalidMachineResult.Sent || invalidMachineResult.ErrorCode != "INVALID_MACHINE_ID")
    {
        throw new InvalidOperationException("Blank machine command did not return deterministic INVALID_MACHINE_ID.");
    }

    await WaitForCommandErrorTraceAsync(traces, string.Empty, "INVALID_MACHINE_ID");
    Console.WriteLine("PASS: invalid machine command returns deterministic INVALID_MACHINE_ID error");

    MachineCommandSendResult result = await server.SendMachineCommandWithResultAsync(
        "PC99",
        lockMachine: true,
        issuedBy: "NetworkSmokeTest",
        reason: "offline command test");

    if (result.Sent || result.ErrorCode != "MACHINE_OFFLINE")
    {
        throw new InvalidOperationException("Offline machine command did not return deterministic MACHINE_OFFLINE.");
    }

    await WaitForCommandErrorTraceAsync(traces, "PC99", "MACHINE_OFFLINE");
    Console.WriteLine("PASS: offline command returns deterministic MACHINE_OFFLINE error");
}

static async Task WaitForCommandTraceAsync(List<NetworkTraceEntry> traces, PacketType commandType, string machineId)
{
    using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

    while (!timeoutTokenSource.IsCancellationRequested)
    {
        NetworkTraceEntry[] snapshot;
        lock (traces)
        {
            snapshot = [.. traces];
        }

        foreach (NetworkTraceEntry trace in snapshot)
        {
            if (!string.Equals(trace.Direction, "OUT_COMMAND", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(trace.Message))
            {
                continue;
            }

            object packet = JsonHelper.DeserializePacket(trace.Message);
            if (commandType == PacketType.LOCK
                && packet is Packet<LockPayload> lockPacket
                && lockPacket.Type == PacketType.LOCK
                && string.Equals(lockPacket.TypedPayload.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"COMMAND JSON LOCK  : {trace.Message}");
                return;
            }

            if (commandType == PacketType.UNLOCK
                && packet is Packet<UnlockPayload> unlockPacket
                && unlockPacket.Type == PacketType.UNLOCK
                && string.Equals(unlockPacket.TypedPayload.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"COMMAND JSON UNLOCK: {trace.Message}");
                return;
            }

            if (commandType == PacketType.SHUTDOWN
                && packet is Packet<ShutdownPayload> shutdownPacket
                && shutdownPacket.Type == PacketType.SHUTDOWN
                && string.Equals(shutdownPacket.TypedPayload.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"COMMAND JSON SHUTDOWN: {trace.Message}");
                return;
            }
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25), timeoutTokenSource.Token);
    }

    throw new InvalidOperationException($"{commandType} command JSON trace for {machineId} was not emitted.");
}

static async Task AssertCommandTraceCountUnchangedAsync(
    List<NetworkTraceEntry> traces,
    PacketType commandType,
    string machineId,
    int expectedCount,
    string failureMessage)
{
    await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
    int actualCount = CountCommandTraces(traces, commandType, machineId);
    if (actualCount != expectedCount)
    {
        throw new InvalidOperationException($"{failureMessage} Expected {expectedCount}, got {actualCount}.");
    }
}

static int CountCommandTraces(List<NetworkTraceEntry> traces, PacketType commandType, string machineId)
{
    NetworkTraceEntry[] snapshot;
    lock (traces)
    {
        snapshot = [.. traces];
    }

    string typeNeedle = $"\"type\":\"{commandType}\"";
    string machineNeedle = $"\"machineId\":\"{machineId}\"";
    return snapshot.Count(trace =>
        string.Equals(trace.Direction, "OUT_COMMAND", StringComparison.Ordinal)
        && trace.Message.Contains(typeNeedle, StringComparison.OrdinalIgnoreCase)
        && trace.Message.Contains(machineNeedle, StringComparison.OrdinalIgnoreCase));
}

static async Task WaitForCommandAckTraceAsync(
    List<NetworkTraceEntry> traces,
    Packet commandPacket,
    string machineId,
    string status)
{
    DateTime deadlineUtc = DateTime.UtcNow.AddSeconds(2);

    while (DateTime.UtcNow < deadlineUtc)
    {
        NetworkTraceEntry[] snapshot = [];
        try
        {
            snapshot = [.. traces];
        }
        catch (InvalidOperationException)
        {
        }

        foreach (NetworkTraceEntry trace in snapshot)
        {
            if (!string.Equals(trace.Direction, "COMMAND_ACK", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(trace.Message))
            {
                continue;
            }

            if (trace.Message.Contains($"\"requestId\":\"{commandPacket.RequestId}\"", StringComparison.Ordinal)
                && trace.Message.Contains($"\"machineId\":\"{machineId}\"", StringComparison.OrdinalIgnoreCase)
                && trace.Message.Contains($"\"ackFor\":\"{commandPacket.Type}\"", StringComparison.OrdinalIgnoreCase)
                && trace.Message.Contains($"\"status\":\"{status}\"", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"COMMAND ACK {commandPacket.Type}: {trace.Message}");
                return;
            }
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25)).ConfigureAwait(false);
    }

    throw new InvalidOperationException($"{commandPacket.Type} ACK trace for {machineId} was not emitted.");
}

static async Task WaitForCommandAckErrorTraceAsync(List<NetworkTraceEntry> traces, string errorCode)
{
    DateTime deadlineUtc = DateTime.UtcNow.AddSeconds(2);

    while (DateTime.UtcNow < deadlineUtc)
    {
        NetworkTraceEntry[] snapshot;
        lock (traces)
        {
            snapshot = [.. traces];
        }

        foreach (NetworkTraceEntry trace in snapshot)
        {
            if (string.Equals(trace.Direction, "COMMAND_ACK_ERROR", StringComparison.Ordinal)
                && trace.Message.Contains(errorCode, StringComparison.Ordinal))
            {
                return;
            }
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25)).ConfigureAwait(false);
    }

    throw new InvalidOperationException($"{errorCode} command ACK error trace was not emitted.");
}

static async Task WaitForCommandErrorTraceAsync(List<NetworkTraceEntry> traces, string machineId, string errorCode)
{
    using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

    while (!timeoutTokenSource.IsCancellationRequested)
    {
        NetworkTraceEntry[] snapshot;
        lock (traces)
        {
            snapshot = [.. traces];
        }

        foreach (NetworkTraceEntry trace in snapshot)
        {
            if (string.Equals(trace.Direction, "COMMAND_ERROR", StringComparison.Ordinal)
                && string.Equals(trace.ClientId, machineId, StringComparison.OrdinalIgnoreCase)
                && trace.Message.Contains(errorCode, StringComparison.Ordinal))
            {
                return;
            }
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25), timeoutTokenSource.Token);
    }

    throw new InvalidOperationException($"{errorCode} command error trace for {machineId} was not emitted.");
}

static async Task WaitForStatusTraceAsync(List<NetworkTraceEntry> traces, string machineId, string status)
{
    using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

    while (!timeoutTokenSource.IsCancellationRequested)
    {
        NetworkTraceEntry[] snapshot;
        lock (traces)
        {
            snapshot = [.. traces];
        }

        foreach (NetworkTraceEntry trace in snapshot)
        {
            if (!string.Equals(trace.Direction, "STATUS", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(trace.Message))
            {
                continue;
            }

            if (JsonHelper.DeserializePacket(trace.Message) is Packet<StatusPayload> statusPacket
                && statusPacket.Type == PacketType.STATUS
                && string.Equals(statusPacket.TypedPayload.MachineId, machineId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(statusPacket.TypedPayload.Status, status, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25), timeoutTokenSource.Token);
    }

    throw new InvalidOperationException($"STATUS {status} for {machineId} was not emitted.");
}

static async Task<string> ReadRequiredLineAsync(StreamReader reader, string scenario)
{
    string? line = await ReadOptionalLineAsync(reader, scenario, TimeSpan.FromSeconds(5));
    if (string.IsNullOrWhiteSpace(line))
    {
        throw new InvalidOperationException($"Client did not receive a JSON-line packet for {scenario}.");
    }

    return line;
}

static async Task<string?> ReadOptionalLineAsync(StreamReader reader, string scenario, TimeSpan timeout)
{
    using var timeoutTokenSource = new CancellationTokenSource(timeout);

    try
    {
        return await reader.ReadLineAsync(timeoutTokenSource.Token);
    }
    catch (OperationCanceledException)
    {
        throw new TimeoutException($"Timed out waiting for {scenario}.");
    }
}

static void TryShutdown(TcpClient tcpClient)
{
    try
    {
        tcpClient.Client.Shutdown(SocketShutdown.Both);
    }
    catch (SocketException)
    {
    }
    catch (ObjectDisposedException)
    {
    }
}

static void AssertMatchingRequestId(Packet request, Packet response)
{
    if (response.RequestId != request.RequestId)
    {
        throw new InvalidOperationException($"{request.Type} response requestId did not match requestId.");
    }
}
