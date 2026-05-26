using System.Net;
using System.Net.Sockets;
using NETManager.Shared.DTOs.RequestPayloads;
using NETManager.Shared.Packets;
using NETManager.Shared.Networking;
using NETManager.Shared.Utilities.JsonHelper;

Console.WriteLine("NETManager JSON-line TCP round-trip smoke test");

using var listener = new TcpListener(IPAddress.Loopback, port: 0);
listener.Start();

int port = ((IPEndPoint)listener.LocalEndpoint).Port;
Console.WriteLine($"Server listening on 127.0.0.1:{port}");

Task serverTask = RunServerOnceAsync(listener);
await RunClientOnceAsync(port);
await serverTask;

Console.WriteLine("PASS: Client -> TCP JSON-line -> Server -> ACK JSON-line -> Client");

static async Task RunServerOnceAsync(TcpListener listener)
{
    using TcpClient serverClient = await listener.AcceptTcpClientAsync();
    await using NetworkStream stream = serverClient.GetStream();
    using var reader = new StreamReader(stream, NetworkProtocol.TextEncoding, leaveOpen: true);
    await using var writer = new StreamWriter(stream, NetworkProtocol.TextEncoding, leaveOpen: true)
    {
        AutoFlush = true
    };

    string? inboundLine = await reader.ReadLineAsync();
    if (string.IsNullOrWhiteSpace(inboundLine))
    {
        throw new InvalidOperationException("Server did not receive a JSON-line packet.");
    }

    Console.WriteLine($"SERVER IN : {inboundLine}");

    string outboundLine = DispatchPacket(inboundLine);

    Console.WriteLine($"SERVER OUT: {outboundLine}");

    await writer.WriteLineAsync(outboundLine);
    listener.Stop();
}

static async Task RunClientOnceAsync(int port)
{
    using var tcpClient = new TcpClient();
    await tcpClient.ConnectAsync(IPAddress.Loopback, port);

    await using NetworkStream stream = tcpClient.GetStream();
    using var reader = new StreamReader(stream, NetworkProtocol.TextEncoding, leaveOpen: true);
    await using var writer = new StreamWriter(stream, NetworkProtocol.TextEncoding, leaveOpen: true)
    {
        AutoFlush = true
    };

    var loginPacket = PacketFactory.CreateLogin(
        source: "PC-01",
        target: NetworkProtocol.ServerSource,
        payload: new LoginPayload
        {
            Username = "client01",
            Password = "123456",
            Role = "client",
            MachineId = "PC-01"
        },
        requestId: $"roundtrip-{Guid.NewGuid():N}");

    string outboundLine = NetworkProtocol.ValidateOutgoingMessage(JsonHelper.SerializeToJson(loginPacket));
    Console.WriteLine($"CLIENT OUT: {outboundLine}");

    await writer.WriteLineAsync(outboundLine);

    string? inboundLine = await reader.ReadLineAsync();
    if (string.IsNullOrWhiteSpace(inboundLine))
    {
        throw new InvalidOperationException("Client did not receive an ACK JSON-line packet.");
    }

    Console.WriteLine($"CLIENT IN : {inboundLine}");

    var ackPacket = JsonHelper.DeserializePacket(inboundLine) as Packet<AckPayload>
        ?? throw new InvalidOperationException("Client expected ACK packet.");

    if (ackPacket.TypedPayload.Status != "OK")
    {
        throw new InvalidOperationException($"ACK status was {ackPacket.TypedPayload.Status}, expected OK.");
    }
}

static string DispatchPacket(string inboundLine)
{
    Packet packet = (Packet)JsonHelper.DeserializePacket(inboundLine);

    switch (packet.Type.ToString())
    {
        case "LOGIN":
            return HandleLoginPacket((Packet<LoginPayload>)packet);

        default:
            throw new InvalidOperationException($"Unsupported packet type: {packet.Type}");
    }
}

static string HandleLoginPacket(Packet<LoginPayload> loginPacket)
{
    var ackPacket = PacketFactory.CreateAck(
        source: NetworkProtocol.ServerSource,
        target: loginPacket.Source,
        payload: new AckPayload
        {
            MachineId = loginPacket.TypedPayload.MachineId,
            AckFor = loginPacket.Type.ToString(),
            Status = "OK",
            Message = $"Round-trip received for {loginPacket.TypedPayload.Username}"
        },
        requestId: loginPacket.RequestId);

    return NetworkProtocol.ValidateOutgoingMessage(
        JsonHelper.SerializeToJson(ackPacket));
}
