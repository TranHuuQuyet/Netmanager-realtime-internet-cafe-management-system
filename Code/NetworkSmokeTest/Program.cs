using System.Net;
using System.Net.Sockets;
using Shared.DTOs.RequestPayloads;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;
using ServerApp.Networking;

Console.WriteLine("NETManager ServerApp listener JSON-line smoke test");

using var server = new TcpJsonLineServer(IPAddress.Loopback, port: 0);
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

await RunClientOnceAsync(port);

Console.WriteLine("PASS: Client -> ServerApp listener -> typed dispatcher -> ACK JSON-line -> Client");

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
            Password = "123",
            Role = "Client",
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

    if (ackPacket.TypedPayload.Status != "Success")
    {
        throw new InvalidOperationException(
            $"ACK status was {ackPacket.TypedPayload.Status}, expected Success.");
    }

    if (ackPacket.RequestId != loginPacket.RequestId)
    {
        throw new InvalidOperationException("ACK requestId did not match LOGIN requestId.");
    }
}
