using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows.Forms;
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

Console.WriteLine("NETManager ServerApp listener JSON-line smoke test");
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
            Console.WriteLine($"TRACE {trace.Direction} {trace.ClientId}: {trace.Message}");
        }
    };
    server.CommandResultEmitted += result =>
    {
        lock (commandResults)
        {
            commandResults.Add(result);
        }

        Console.WriteLine(
            $"COMMAND RESULT {result.Command} {result.MachineId}: {result.Status} {result.ErrorCode ?? string.Empty} {result.RequestId}");
    };
    server.ChatReceived += (machineId, payload) =>
    {
        lock (chatMessages)
        {
            chatMessages.Add((machineId, payload));
        }

        Console.WriteLine($"CHAT IN {machineId}: {payload.Message}");
    };

    server.Start();
    int port = server.LocalEndpoint.Port;
    Console.WriteLine($"ServerApp listener active on 127.0.0.1:{port}");

    await AssertLoginAndDisconnectEmitStatusAsync(port, authRuntime.SessionRepository, traces);
    await AssertAdminUiLockUnlockCommandTraceAsync(port, authRuntime, server, traces, commandResults);
    await AssertTwoClientChatRoutingAsync(port, authRuntime, server, chatMessages);
    await AssertBillingTimerRoutingAsync(port, authRuntime, server);
    await AssertStatusRouteAcceptedAsync(port, authRuntime.SessionRepository);
    await AssertLoginSuccessAsync(port, authRuntime.SessionRepository, authRuntime.Machines, "TC-N03");
    await AssertRepeatedLoginRejectedWhileActiveAsync(port, authRuntime.SessionRepository, authRuntime.Machines);
    await AssertLoginFailureAsync(port, password: "wrong-password", machineId: "PC-01", expectedErrorCode: "INVALID_CREDENTIALS");
    await AssertLoginFailureAsync(port, password: "123", machineId: "PC-02", expectedErrorCode: "ACCOUNT_MACHINE_MISMATCH");
    await AssertRejectedLineDoesNotStopServerAsync(
        port,
        authRuntime.SessionRepository,
        authRuntime.Machines,
        "{ invalid json",
        "invalid JSON",
        "TC-N04");
    await AssertRejectedLineDoesNotStopServerAsync(
        port,
        authRuntime.SessionRepository,
        authRuntime.Machines,
        """{"type":"UNKNOWN","source":"PC-01","target":"server","requestId":"unsupported-unknown","timestamp":"2026-06-02T00:00:00Z","payload":{}}""",
        "unknown packet type",
        "TC-N05");

    PrintCasePass("PASS: Client -> ServerApp listener -> auth dispatcher -> controlled invalid/unsupported handling -> Client");
}
finally
{
    SqliteConnection.ClearAllPools();

    if (File.Exists(databasePath))
    {
        DeleteTempDatabase(databasePath);
    }
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

        Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC-01");
        object response = await SendLoginOnOpenStreamAsync(reader, writer, loginPacket);
        Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);
        sessionId = resultPacket.TypedPayload.SessionId;

        await WaitForStatusTraceAsync(traces, "PC-01", "Online");

        using var mainForm = new ServerApp.MainForm(authRuntime.Machines, server);
        SynchronizationContext.SetSynchronizationContext(null);
        mainForm.ApplyMachineStatusUpdate("PC-01", "Online");

        ClickMachineActionButton(mainForm, "btnLockMachine");
        await WaitForCommandTraceAsync(traces, PacketType.LOCK, "PC-01").ConfigureAwait(false);
        Packet lockCommand = await AssertCommandReceivedAsync(reader, PacketType.LOCK, "PC-01").ConfigureAwait(false);
        await SendCommandAckAsync(
            stream,
            lockCommand,
            "PC-02",
            "Success",
            "Wrong machine ACK.").ConfigureAwait(false);
        await WaitForCommandAckErrorTraceAsync(traces, "UNAUTHORIZED_COMMAND").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            lockCommand.RequestId!,
            CommandType.LOCK,
            "PC-02",
            isError: true,
            expectedErrorCode: "UNAUTHORIZED_COMMAND").ConfigureAwait(false);

        string unknownRequestId = $"ack-missing-{Guid.NewGuid():N}";
        await SendCommandAckAsync(
            stream,
            lockCommand,
            "PC-01",
            "Success",
            "Unknown request ACK.",
            requestIdOverride: unknownRequestId).ConfigureAwait(false);
        await WaitForCommandAckErrorTraceAsync(traces, "ACK_UNKNOWN_REQUEST").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            unknownRequestId,
            CommandType.LOCK,
            "PC-01",
            isError: true,
            expectedErrorCode: "ACK_UNKNOWN_REQUEST").ConfigureAwait(false);

        await SendCommandAckAsync(
            stream,
            lockCommand,
            "PC-01",
            "Success",
            "Wrong type ACK.",
            ackForOverride: PacketType.UNLOCK.ToString()).ConfigureAwait(false);
        await WaitForCommandAckErrorTraceAsync(traces, "ACK_TYPE_MISMATCH").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            lockCommand.RequestId!,
            CommandType.LOCK,
            "PC-01",
            isError: true,
            expectedErrorCode: "ACK_TYPE_MISMATCH").ConfigureAwait(false);

        await SendCommandAckAsync(
            stream,
            lockCommand,
            "PC-01",
            "Maybe",
            "Invalid ACK status.").ConfigureAwait(false);
        await WaitForCommandAckErrorTraceAsync(traces, "INVALID_PACKET").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            lockCommand.RequestId!,
            CommandType.LOCK,
            "PC-01",
            isError: true,
            expectedErrorCode: "INVALID_PACKET").ConfigureAwait(false);

        await SendCommandAckAsync(stream, lockCommand, "PC-01", "Success", "Lock applied.").ConfigureAwait(false);
        await WaitForCommandAckTraceAsync(traces, lockCommand, "PC-01", "Success").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            lockCommand.RequestId!,
            CommandType.LOCK,
            "PC-01",
            isError: false,
            expectedErrorCode: null).ConfigureAwait(false);
        PrintCasePass("PASS: admin UI Lock action emits real LOCK JSON command packet and receives typed ACK");

        ClickMachineActionButton(mainForm, "btnUnlockMachine");
        await WaitForCommandTraceAsync(traces, PacketType.UNLOCK, "PC-01").ConfigureAwait(false);
        Packet unlockCommand = await AssertCommandReceivedAsync(reader, PacketType.UNLOCK, "PC-01").ConfigureAwait(false);
        await SendCommandAckAsync(
            stream,
            unlockCommand,
            "PC-02",
            "Success",
            "Wrong machine unlock ACK.").ConfigureAwait(false);
        await WaitForCommandAckErrorTraceAsync(traces, "UNAUTHORIZED_COMMAND").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            unlockCommand.RequestId!,
            CommandType.UNLOCK,
            "PC-02",
            isError: true,
            expectedErrorCode: "UNAUTHORIZED_COMMAND").ConfigureAwait(false);

        await SendCommandAckAsync(stream, unlockCommand, "PC-01", "Success", "Unlock applied.").ConfigureAwait(false);
        await WaitForCommandAckTraceAsync(traces, unlockCommand, "PC-01", "Success").ConfigureAwait(false);
        await WaitForCommandResultAsync(
            commandResults,
            unlockCommand.RequestId!,
            CommandType.UNLOCK,
            "PC-01",
            isError: false,
            expectedErrorCode: null).ConfigureAwait(false);
        PrintCasePass("PASS: admin UI Unlock action emits real UNLOCK JSON command packet and receives typed ACK");

        await AssertCommandErrorAsync(server, traces).ConfigureAwait(false);

        lock (traces)
        {
            traces.Clear();
        }

        ClickMachineActionButton(mainForm, "btnLockMachine");
        await WaitForCommandTraceAsync(traces, PacketType.LOCK, "PC-01").ConfigureAwait(false);
        Packet disconnectedCommand = await AssertCommandReceivedAsync(reader, PacketType.LOCK, "PC-01").ConfigureAwait(false);
        TryShutdown(tcpClient);
        await WaitForCommandResultAsync(
            commandResults,
            disconnectedCommand.RequestId!,
            CommandType.LOCK,
            "PC-01",
            isError: true,
            expectedErrorCode: "COMMAND_CLIENT_DISCONNECTED").ConfigureAwait(false);
        PrintCasePass("PASS: pending command emits typed COMMAND_CLIENT_DISCONNECTED on client disconnect", "TC-N11");

        await authRuntime.SessionService.CloseSessionAsync(sessionId);
    }

    await WaitForClosedSessionAsync(authRuntime.SessionRepository, sessionId);
}

static async Task AssertTwoClientChatRoutingAsync(
    int port,
    AuthRuntime authRuntime,
    TcpJsonLineServer server,
    List<(string MachineId, ChatPayload Payload)> chatMessages)
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

    Packet<LoginPayload> pc01Login = CreateClientLoginPacket("client01", "PC-01", "123");
    Packet<LoginPayload> pc02Login = CreateClientLoginPacket("client02", "PC-02", "123");
    pc01SessionId = AssertClientLoginSuccess(pc01Login, await SendLoginOnOpenStreamAsync(pc01Reader, pc01Writer, pc01Login), "client01", "PC-01");
    pc02SessionId = AssertClientLoginSuccess(pc02Login, await SendLoginOnOpenStreamAsync(pc02Reader, pc02Writer, pc02Login), "client02", "PC-02");

    MachineCommandSendResult pc02Command = await server.SendMachineCommandWithResultAsync(
        "PC-02",
        lockMachine: true,
        issuedBy: "NetworkSmoke",
        reason: "Selected-client routing check").ConfigureAwait(false);

    if (!pc02Command.Sent)
    {
        throw new InvalidOperationException($"PC-02 command route failed: {pc02Command.ErrorCode} {pc02Command.Message}");
    }

    Packet pc02LockCommand = await AssertCommandReceivedAsync(pc02Reader, PacketType.LOCK, "PC-02").ConfigureAwait(false);
    await SendCommandAckAsync(
        pc02Stream,
        pc02LockCommand,
        "PC-02",
        "Success",
        "PC-02 lock applied.").ConfigureAwait(false);

    MachineChatSendResult sendResult = await server.SendChatAsync(
        "PC-01",
        "Server",
        "Hello selected client.").ConfigureAwait(false);

    if (!sendResult.Sent)
    {
        throw new InvalidOperationException($"CHAT send failed: {sendResult.ErrorCode} {sendResult.Message}");
    }

    Packet<ChatPayload> pc01Chat = await ReadChatPacketAsync(pc01Reader, "PC-01 admin CHAT").ConfigureAwait(false);
    if (pc01Chat.TypedPayload.Message != "Hello selected client."
        || pc01Chat.Target != "PC-01")
    {
        throw new InvalidOperationException("Selected client did not receive the expected admin CHAT.");
    }

    await AssertNoLineWithinAsync(pc02Reader, TimeSpan.FromMilliseconds(300), "non-selected client CHAT").ConfigureAwait(false);

    Packet<ChatPayload> reply = PacketFactory.CreateChat(
        source: "PC-01",
        target: NetworkProtocol.ServerSource,
        payload: new ChatPayload
        {
            Sender = "PC-01",
            Receiver = NetworkProtocol.ServerSource,
            Message = "Client reply."
        },
        requestId: $"chat-{Guid.NewGuid():N}");
    string replyLine = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(reply));
    Console.WriteLine($"CLIENT OUT: {replyLine}");
    await pc01Writer.WriteLineAsync(replyLine).ConfigureAwait(false);
    await WaitForChatMessageAsync(chatMessages, "PC-01", "Client reply.").ConfigureAwait(false);

    MachineChatSendResult offlineResult = await server.SendChatAsync(
        "PC-99",
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
    PrintCasePass("PASS: selected-client CHAT route delivers only to target and accepts client reply", "TC-N09");
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

    Packet<LoginPayload> pc01Login = CreateClientLoginPacket("client01", "PC-01", "123");
    Packet<LoginPayload> pc02Login = CreateClientLoginPacket("client02", "PC-02", "123");
    string pc01SessionId = AssertClientLoginSuccess(
        pc01Login,
        await SendLoginOnOpenStreamAsync(pc01Reader, pc01Writer, pc01Login),
        "client01",
        "PC-01");
    string pc02SessionId = AssertClientLoginSuccess(
        pc02Login,
        await SendLoginOnOpenStreamAsync(pc02Reader, pc02Writer, pc02Login),
        "client02",
        "PC-02");

    AdminBillingResult timed = await billingService.StartTimedAsync("PC-01", 4);
    if (!timed.IsSuccess)
    {
        throw new InvalidOperationException($"Timed billing failed: {timed.ErrorCode} {timed.Message}");
    }

    Packet<TimerPayload> timedTimer = await ReadTimerPacketAsync(pc01Reader, "PC-01 timed billing TIMER");
    if (timedTimer.TypedPayload.RemainingSeconds is null
        || timedTimer.TypedPayload.RemainingSeconds > 300
        || !timedTimer.TypedPayload.IsWarning)
    {
        throw new InvalidOperationException("Timed billing TIMER did not carry 5-minute warning state.");
    }

    AdminBillingResult openEnded = await billingService.StartOpenEndedAsync("PC-02");
    if (!openEnded.IsSuccess)
    {
        throw new InvalidOperationException($"Open-ended billing failed: {openEnded.ErrorCode} {openEnded.Message}");
    }

    Packet<TimerPayload> openEndedTimer = await ReadTimerPacketAsync(pc02Reader, "PC-02 open-ended TIMER");
    if (openEndedTimer.TypedPayload.ExpiresAt is not null
        || openEndedTimer.TypedPayload.RemainingSeconds is not null
        || openEndedTimer.TypedPayload.AmountVnd < 0)
    {
        throw new InvalidOperationException("Open-ended TIMER did not carry nullable expiry/remaining billing state.");
    }

    AdminBillingResult extended = await billingService.ExtendAsync("PC-01", 2);
    if (!extended.IsSuccess)
    {
        throw new InvalidOperationException($"Billing extend failed: {extended.ErrorCode} {extended.Message}");
    }

    Packet<TimerPayload> extendedTimer = await ReadTimerPacketAsync(pc01Reader, "PC-01 extended TIMER");
    if (!string.Equals(extendedTimer.TypedPayload.RentalMode, BillingRentalMode.Extend.ToString(), StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Extended TIMER did not mark rentalMode Extend.");
    }

    AdminBillingResult closed = await billingService.CloseAsync("PC-01");
    if (!closed.IsSuccess)
    {
        throw new InvalidOperationException($"Billing close failed: {closed.ErrorCode} {closed.Message}");
    }

    Packet<TimerPayload> closedTimer = await ReadTimerPacketAsync(pc01Reader, "PC-01 closed TIMER");
    if (!string.Equals(closedTimer.TypedPayload.Status, "Closed", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Closed billing TIMER did not mark status Closed.");
    }

    SessionRecord pc01Session = await authRuntime.SessionRepository.GetActiveByMachineIdAsync("PC-01")
        ?? throw new InvalidOperationException("Expected active PC-01 auth session for expired billing check.");
    AdminBillingResult expiredOpen = await OpenExpiredBillingAsync(authRuntime, pc01Session);
    if (!expiredOpen.IsSuccess)
    {
        throw new InvalidOperationException($"Expired billing setup failed: {expiredOpen.ErrorCode} {expiredOpen.Message}");
    }

    await billingService.RefreshActiveSessionsAsync();
    Packet<TimerPayload> expiredTimer = await ReadTimerPacketAsync(pc01Reader, "PC-01 expired TIMER");
    if (!expiredTimer.TypedPayload.ShouldLockNow || expiredTimer.TypedPayload.RemainingSeconds != 0)
    {
        throw new InvalidOperationException("Expired billing TIMER did not request lock.");
    }

    Packet lockCommand = await AssertCommandReceivedAsync(pc01Reader, PacketType.LOCK, "PC-01");
    await SendCommandAckAsync(pc01Stream, lockCommand, "PC-01", "Success", "Billing expiry lock applied.");

    Packet<TimerPayload> pc02RefreshTimer = await ReadTimerPacketAsync(pc02Reader, "PC-02 refresh TIMER");
    if (pc02RefreshTimer.TypedPayload.ExpiresAt is not null)
    {
        throw new InvalidOperationException("Refresh TIMER did not preserve PC-02 open-ended session.");
    }

    await SendStatusAsync(pc02Writer, "PC-02", pc02SessionId);
    object firstStatusResponse = await ReadAnyPacketAsync(pc02Reader, "PC-02 STATUS response");
    object secondStatusResponse = await ReadAnyPacketAsync(pc02Reader, "PC-02 STATUS billing response");
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

    await billingService.CloseAsync("PC-01");
    await billingService.CloseAsync("PC-02");
    TryShutdown(pc01Client);
    TryShutdown(pc02Client);
    await WaitForClosedSessionAsync(authRuntime.SessionRepository, pc01SessionId);
    await WaitForClosedSessionAsync(authRuntime.SessionRepository, pc02SessionId);
    PrintCasePass("PASS: billing TIMER route supports timed warning, open-ended, extend/close, expiry LOCK and STATUS resync");
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

static async Task AssertNoLineWithinAsync(StreamReader reader, TimeSpan timeout, string description)
{
    Task<string?> readTask = reader.ReadLineAsync();
    Task delayTask = Task.Delay(timeout);
    Task completed = await Task.WhenAny(readTask, delayTask).ConfigureAwait(false);

    if (completed == readTask)
    {
        string? line = await readTask.ConfigureAwait(false);
        throw new InvalidOperationException($"{description} unexpectedly received line: {line}");
    }
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

        Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC-01");
        object response = await SendLoginOnOpenStreamAsync(reader, writer, loginPacket);
        Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);
        sessionId = resultPacket.TypedPayload.SessionId;

        var statusPacket = PacketFactory.CreateStatus(
            source: "PC-01",
            target: NetworkProtocol.ServerSource,
            payload: new StatusPayload
            {
                MachineId = "PC-01",
                SessionId = sessionId,
                MachineName = "PC-01",
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
            || !string.Equals(ackPacket.TypedPayload.MachineId, "PC-01", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("STATUS route did not return an Accepted ACK for PC-01.");
        }

        AssertMatchingRequestId(statusPacket, ackPacket);
        PrintCasePass("PASS: authenticated STATUS route returns Accepted ACK", "TC-N06");
        TryShutdown(tcpClient);
    }

    await WaitForClosedSessionAsync(sessions, sessionId);
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
    IMachineRepository machines,
    string? testCaseId = null)
{
    Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC-01");
    object response = await SendLoginAsync(port, loginPacket);
    Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);

    PrintCasePass("PASS: valid LOGIN returns authenticated session payload", testCaseId);
    await WaitForClosedSessionAsync(sessions, resultPacket.TypedPayload.SessionId);
    await WaitForMachineStatusAsync(machines, "PC-01", "Offline");
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
        || resultPacket.TypedPayload.MachineId != "PC-01"
        || string.IsNullOrWhiteSpace(resultPacket.TypedPayload.SessionId))
    {
        throw new InvalidOperationException("LOGIN success payload did not match the authenticated account.");
    }

    AssertMatchingRequestId(loginPacket, resultPacket);
    return resultPacket;
}

static async Task AssertRepeatedLoginRejectedWhileActiveAsync(
    int port,
    ISessionRepository sessions,
    IMachineRepository machines)
{
    string sessionId;

    using (var tcpClient = new TcpClient())
    {
        await tcpClient.ConnectAsync(IPAddress.Loopback, port);
        await using NetworkStream stream = tcpClient.GetStream();

        Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC-01");
        object response = await SendLoginOnStreamAsync(stream, loginPacket);
        Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);
        sessionId = resultPacket.TypedPayload.SessionId;

        await AssertLoginFailureAsync(
            port,
            password: "123",
            machineId: "PC-01",
            expectedErrorCode: "MACHINE_ALREADY_ACTIVE");
    }

    await WaitForClosedSessionAsync(sessions, sessionId);
    await WaitForMachineStatusAsync(machines, "PC-01", "Offline");
    await AssertLoginSuccessAsync(port, sessions, machines);
    PrintCasePass("PASS: repeated LOGIN is rejected while active and succeeds after disconnect");
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

        Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC-01");
        object response = await SendLoginOnStreamAsync(stream, loginPacket);
        Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);
        sessionId = resultPacket.TypedPayload.SessionId;

        await WaitForStatusTraceAsync(traces, "PC-01", "Online");
        PrintCasePass("PASS: authenticated LOGIN emits STATUS Online", "TC-N07");
    }

    await WaitForClosedSessionAsync(sessions, sessionId);
    await WaitForStatusTraceAsync(traces, "PC-01", "Offline");
    PrintCasePass("PASS: authenticated disconnect emits STATUS Offline", "TC-N08");
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
    PrintCasePass($"PASS: rejected LOGIN returns {expectedErrorCode}");
}

static Packet<LoginPayload> CreateLoginPacket(string password, string machineId)
{
    return PacketFactory.CreateLogin(
        source: "PC-01",
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
    IMachineRepository machines,
    string outboundLine,
    string scenario,
    string testCaseId)
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

    await AssertLoginSuccessAsync(port, sessions, machines);
    PrintCasePass($"PASS: {scenario} disconnects only the offending client and server remains available", testCaseId);
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
            PrintCasePass($"PASS: disconnected socket closes session {sessionId}");
            return;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25));
    }

    throw new InvalidOperationException($"Session {sessionId} remained active after socket disconnect.");
}

static async Task WaitForMachineStatusAsync(IMachineRepository machines, string machineId, string status)
{
    DateTime deadlineUtc = DateTime.UtcNow.AddSeconds(2);

    while (DateTime.UtcNow < deadlineUtc)
    {
        var machine = await machines.GetByMachineIdAsync(machineId);
        if (machine is not null
            && string.Equals(machine.Status, status, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25));
    }

    throw new InvalidOperationException($"Machine {machineId} did not become {status}.");
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
    PrintCasePass("PASS: invalid machine command returns deterministic INVALID_MACHINE_ID error");

    MachineCommandSendResult result = await server.SendMachineCommandWithResultAsync(
        "PC-99",
        lockMachine: true,
        issuedBy: "NetworkSmokeTest",
        reason: "offline command test");

    if (result.Sent || result.ErrorCode != "MACHINE_OFFLINE")
    {
        throw new InvalidOperationException("Offline machine command did not return deterministic MACHINE_OFFLINE.");
    }

    await WaitForCommandErrorTraceAsync(traces, "PC-99", "MACHINE_OFFLINE");
    PrintCasePass("PASS: offline command returns deterministic MACHINE_OFFLINE error");
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
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25), timeoutTokenSource.Token);
    }

    throw new InvalidOperationException($"{commandType} command JSON trace for {machineId} was not emitted.");
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

static void PrintCasePass(string message, string? testCaseId = null)
{
    Console.WriteLine();
    if (!string.IsNullOrWhiteSpace(testCaseId))
    {
        Console.WriteLine(testCaseId);
    }

    Console.WriteLine(message);
    Console.WriteLine();
}
