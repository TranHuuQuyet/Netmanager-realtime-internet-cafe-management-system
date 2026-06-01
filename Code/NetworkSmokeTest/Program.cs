using System.Net;
using System.Net.Sockets;
using Microsoft.Data.Sqlite;
using Shared.DTOs.RequestPayloads;
using Shared.DTOs.ResponsePayloads;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;
using ServerApp.Auth.Services;
using ServerApp.Networking;

Console.WriteLine("NETManager ServerApp listener JSON-line smoke test");

string databasePath = Path.Combine(Path.GetTempPath(), $"netmanager-network-smoke-{Guid.NewGuid():N}.db");

try
{
    AuthRuntime authRuntime = await AuthBootstrapper.CreateAsync(databasePath);

    using var server = new TcpJsonLineServer(
        IPAddress.Loopback,
        port: 0,
        new PacketDispatcher(authRuntime.Auth));
    server.TraceEmitted += trace =>
    {
        if (!string.IsNullOrWhiteSpace(trace.Message))
        {
            Console.WriteLine($"TRACE {trace.Direction} {trace.ClientId}: {trace.Message}");
        }
    };

    server.Start();
    int port = server.LocalEndpoint.Port;
    Console.WriteLine($"ServerApp listener active on 127.0.0.1:{port}");

    await AssertLoginSuccessAsync(port);
    await AssertLoginFailureAsync(port, password: "wrong-password", machineId: "PC-01", expectedErrorCode: "INVALID_CREDENTIALS");
    await AssertLoginFailureAsync(port, password: "123", machineId: "PC-02", expectedErrorCode: "ACCOUNT_MACHINE_MISMATCH");

    Console.WriteLine("PASS: Client -> ServerApp listener -> auth dispatcher -> LOGIN success/failure JSON-line -> Client");
}
finally
{
    SqliteConnection.ClearAllPools();

    if (File.Exists(databasePath))
    {
        File.Delete(databasePath);
    }
}

static async Task AssertLoginSuccessAsync(int port)
{
    Packet<LoginPayload> loginPacket = CreateLoginPacket(password: "123", machineId: "PC-01");
    object response = await SendLoginAsync(port, loginPacket);

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
    Console.WriteLine("PASS: valid LOGIN returns authenticated session payload");
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

static void AssertMatchingRequestId(Packet<LoginPayload> request, Packet response)
{
    if (response.RequestId != request.RequestId)
    {
        throw new InvalidOperationException("LOGIN response requestId did not match LOGIN requestId.");
    }
}
