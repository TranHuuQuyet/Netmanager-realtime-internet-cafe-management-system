using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using Shared.Enums;
using Shared.DTOs.CommandPayloads;
using Shared.DTOs.RequestPayloads;
using Shared.DTOs.ResponsePayloads;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;
using ServerApp.Auth.Services;
using ServerApp.Database.Contracts;
using ServerApp.Networking;

Console.WriteLine("NETManager ServerApp listener JSON-line smoke test");
WindowsFormsSynchronizationContext.AutoInstall = false;
SynchronizationContext.SetSynchronizationContext(null);

string databasePath = Path.Combine(Path.GetTempPath(), $"netmanager-network-smoke-{Guid.NewGuid():N}.db");

try
{
    AuthRuntime authRuntime = await AuthBootstrapper.CreateAsync(databasePath);
    List<NetworkTraceEntry> traces = [];
    List<MachineCommandAckResult> commandResults = [];

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

    server.Start();
    int port = server.LocalEndpoint.Port;
    Console.WriteLine($"ServerApp listener active on 127.0.0.1:{port}");

    await AssertLoginAndDisconnectEmitStatusAsync(port, authRuntime.SessionRepository, traces);
    await AssertAdminUiLockUnlockCommandTraceAsync(port, authRuntime, server, traces, commandResults);
    await AssertStatusRouteAcceptedAsync(port, authRuntime.SessionRepository);
    await AssertLoginSuccessAsync(port, authRuntime.SessionRepository);
    await AssertRepeatedLoginRejectedWhileActiveAsync(port, authRuntime.SessionRepository);
    await AssertLoginFailureAsync(port, password: "wrong-password", machineId: "PC-01", expectedErrorCode: "INVALID_CREDENTIALS");
    await AssertLoginFailureAsync(port, password: "123", machineId: "PC-02", expectedErrorCode: "ACCOUNT_MACHINE_MISMATCH");
    await AssertRejectedLineDoesNotStopServerAsync(port, authRuntime.SessionRepository, "{ invalid json", "invalid JSON");
    await AssertRejectedLineDoesNotStopServerAsync(
        port,
        authRuntime.SessionRepository,
        """{"type":"UNKNOWN","source":"PC-01","target":"server","requestId":"unsupported-unknown","timestamp":"2026-06-02T00:00:00Z","payload":{}}""",
        "unknown packet type");

    Console.WriteLine("PASS: Client -> ServerApp listener -> auth dispatcher -> controlled invalid/unsupported handling -> Client");
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
        Console.WriteLine("PASS: admin UI Lock action emits real LOCK JSON command packet and receives typed ACK");

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
        Console.WriteLine("PASS: admin UI Unlock action emits real UNLOCK JSON command packet and receives typed ACK");

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
        Console.WriteLine("PASS: pending command emits typed COMMAND_CLIENT_DISCONNECTED on client disconnect");

        await authRuntime.SessionService.CloseSessionAsync(sessionId);
    }

    await WaitForClosedSessionAsync(authRuntime.SessionRepository, sessionId);
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
        Console.WriteLine("PASS: authenticated STATUS route returns Accepted ACK");
        TryShutdown(tcpClient);
    }

    await WaitForClosedSessionAsync(sessions, sessionId);
}

static void ClickMachineActionButton(ServerApp.MainForm mainForm, string buttonName)
{
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

static async Task AssertLoginSuccessAsync(int port, ISessionRepository sessions)
{
    Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC-01");
    object response = await SendLoginAsync(port, loginPacket);
    Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);

    Console.WriteLine("PASS: valid LOGIN returns authenticated session payload");
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
        || resultPacket.TypedPayload.MachineId != "PC-01"
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
    await AssertLoginSuccessAsync(port, sessions);
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

        Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC-01");
        object response = await SendLoginOnStreamAsync(stream, loginPacket);
        Packet<LoginResultPayload> resultPacket = AssertLoginSuccessResponse(loginPacket, response);
        sessionId = resultPacket.TypedPayload.SessionId;

        await WaitForStatusTraceAsync(traces, "PC-01", "Online");
        Console.WriteLine("PASS: authenticated LOGIN emits STATUS Online");
    }

    await WaitForClosedSessionAsync(sessions, sessionId);
    await WaitForStatusTraceAsync(traces, "PC-01", "Offline");
    Console.WriteLine("PASS: authenticated disconnect emits STATUS Offline");
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
    string outboundLine,
    string scenario)
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

    await AssertLoginSuccessAsync(port, sessions);
    Console.WriteLine($"PASS: {scenario} disconnects only the offending client and server remains available");
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
        "PC-99",
        lockMachine: true,
        issuedBy: "NetworkSmokeTest",
        reason: "offline command test");

    if (result.Sent || result.ErrorCode != "MACHINE_OFFLINE")
    {
        throw new InvalidOperationException("Offline machine command did not return deterministic MACHINE_OFFLINE.");
    }

    await WaitForCommandErrorTraceAsync(traces, "PC-99", "MACHINE_OFFLINE");
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
