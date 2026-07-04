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

    WriteDiagnostic(
        "PASS: Client -> ServerApp listener -> auth/status/chat/reconnect dispatcher -> controlled results -> Client");
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
