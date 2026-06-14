using System.Net;
using System.Net.Sockets;
using System.Reflection;
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

string databasePath = Path.Combine(Path.GetTempPath(), $"netmanager-network-smoke-{Guid.NewGuid():N}.db");

try
{
    AuthRuntime authRuntime = await AuthBootstrapper.CreateAsync(databasePath);
    List<NetworkTraceEntry> traces = [];

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

    server.Start();
    int port = server.LocalEndpoint.Port;
    Console.WriteLine($"ServerApp listener active on 127.0.0.1:{port}");

    await AssertLoginAndDisconnectEmitStatusAsync(port, authRuntime.SessionRepository, traces);
    await AssertAdminUiLockUnlockCommandTraceAsync(port, authRuntime, server, traces);
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
    await AssertRejectedLineDoesNotStopServerAsync(
        port,
        authRuntime.SessionRepository,
        """{"type":"STATUS","source":"PC-01","target":"server","requestId":"unsupported-status","timestamp":"2026-06-02T00:00:00Z","payload":{}}""",
        "packet type without an open route");

    Console.WriteLine("PASS: Client -> ServerApp listener -> auth dispatcher -> controlled invalid/unsupported handling -> Client");
}
finally
{
    SqliteConnection.ClearAllPools();

    if (File.Exists(databasePath))
    {
        File.Delete(databasePath);
    }
}

static async Task AssertAdminUiLockUnlockCommandTraceAsync(
    int port,
    AuthRuntime authRuntime,
    TcpJsonLineServer server,
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

        using var mainForm = new ServerApp.MainForm(authRuntime.Machines, server);
        mainForm.ApplyMachineStatusUpdate("PC-01", "Online");

        await InvokeSendMachineCommandAsync(mainForm, CommandType.LOCK, "Lock", "PC-01");
        await WaitForCommandTraceAsync(traces, PacketType.LOCK, "PC-01");
        Console.WriteLine("PASS: admin UI Lock action emits real LOCK JSON command packet");

        await InvokeSendMachineCommandAsync(mainForm, CommandType.UNLOCK, "Unlock", "PC-01");
        await WaitForCommandTraceAsync(traces, PacketType.UNLOCK, "PC-01");
        Console.WriteLine("PASS: admin UI Unlock action emits real UNLOCK JSON command packet");
    }

    await WaitForClosedSessionAsync(authRuntime.SessionRepository, sessionId);
}

static async Task InvokeSendMachineCommandAsync(
    ServerApp.MainForm mainForm,
    CommandType command,
    string action,
    string machineName)
{
    MethodInfo handler = typeof(ServerApp.MainForm)
        .GetMethod("SendMachineCommandAsync", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Could not find MainForm.SendMachineCommandAsync.");

    object? result = handler.Invoke(mainForm, [command, action, machineName]);
    if (result is not Task task)
    {
        throw new InvalidOperationException("MainForm.SendMachineCommandAsync did not return a Task.");
    }

    await task.ConfigureAwait(false);
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

    string outboundLine = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(loginPacket));
    Console.WriteLine($"CLIENT OUT: {outboundLine}");

    await writer.WriteLineAsync(outboundLine);

    string? inboundLine = await reader.ReadLineAsync();
    if (string.IsNullOrWhiteSpace(inboundLine))
    {
        throw new InvalidOperationException("Client did not receive a LOGIN JSON-line packet.");
    }

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

        using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        string? inboundLine = await reader.ReadLineAsync(timeoutTokenSource.Token);

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
    using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

    while (!timeoutTokenSource.IsCancellationRequested)
    {
        var session = await sessions.GetByIdAsync(sessionId, timeoutTokenSource.Token);

        if (session?.State == ServerApp.Auth.Models.SessionState.Closed)
        {
            Console.WriteLine($"PASS: disconnected socket closes session {sessionId}");
            return;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25), timeoutTokenSource.Token);
    }

    throw new InvalidOperationException($"Session {sessionId} remained active after socket disconnect.");
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

static void AssertMatchingRequestId(Packet<LoginPayload> request, Packet response)
{
    if (response.RequestId != request.RequestId)
    {
        throw new InvalidOperationException("LOGIN response requestId did not match LOGIN requestId.");
    }
}
