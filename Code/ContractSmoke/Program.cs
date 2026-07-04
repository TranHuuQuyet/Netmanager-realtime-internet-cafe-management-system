using System.Text.Json;
using Shared.DTOs.RequestPayloads;
using Shared.DTOs.ResponsePayloads;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;

// File nay hien dang chua handler command runtime cua ClientApp trong project ContractSmoke.
// Comment duoc them de nguoi moi hoc C# biet day la luong nhan packet va phat event theo command.
Console.WriteLine("Contract smoke checks passed.");
Run("TC-C01: packet type serializes as string", PacketTypeSerializesAsString);
Run("TC-C02: numeric packet type is rejected", NumericPacketTypeIsRejected);
Run("TC-C03: login request deserializes as request", LoginRequestDeserializesAsRequest);
Run("TC-C04: login success deserializes as result", LoginSuccessDeserializesAsResult);
Run("TC-C05: login failure uses error envelope", LoginFailureUsesErrorEnvelope);
Run("TC-C06: outgoing message rejects embedded line breaks", OutgoingNetworkMessageRejectsEmbeddedLineBreaks);

static void Run(string name, Action check)
{
    check();
    Console.WriteLine($"PASS {name}");
}

static void PacketTypeSerializesAsString()
{
    var packet = PacketFactory.CreateLogin(
        "client01",
        "server",
        new LoginPayload
        {
            Username = "client01",
            Password = "123",
            Role = "Client",
            MachineId = "PC-01"
        },
        "req-0001");

    string json = JsonHelper.SerializeToJson(packet);
    using var doc = JsonDocument.Parse(json);

    JsonElement type = doc.RootElement.GetProperty("type");
    Assert(type.ValueKind == JsonValueKind.String, "Packet type must be serialized as a string.");
    Assert(type.GetString() == "LOGIN", "Packet type must serialize to LOGIN.");
}

static void NumericPacketTypeIsRejected()
{
    const string json = """
        {
          "type": 0,
          "source": "client01",
          "target": "server",
          "requestId": "req-0001",
          "timestamp": "2026-05-25T10:00:00Z",
          "payload": {}
        }
        """;

    AssertThrows(() => JsonHelper.DeserializePacket(json), "Numeric packet type should not deserialize.");
}

static void LoginRequestDeserializesAsRequest()
{
    string json = JsonHelper.SerializeToJson(PacketFactory.CreateLogin(
        "client01",
        "server",
        new LoginPayload
        {
            Username = "client01",
            Password = "123",
            Role = "Client",
            MachineId = "PC-01"
        },
        "req-0001"));

    object packet = JsonHelper.DeserializePacket(json);

    Assert(packet is Packet<LoginPayload>, "LOGIN request must deserialize as Packet<LoginPayload>.");
}

static void LoginSuccessDeserializesAsResult()
{
    string json = JsonHelper.SerializeToJson(PacketFactory.CreateLoginSuccess(
        "server",
        "client01",
        new LoginResultPayload
        {
            SessionId = "session-id",
            Username = "client01",
            Role = "Client",
            MachineId = "PC-01"
        },
        "req-0001"));

    object packet = JsonHelper.DeserializePacket(json);

    Assert(packet is Packet<LoginResultPayload>, "LOGIN success must deserialize as Packet<LoginResultPayload>.");

    var typedPacket = (Packet<LoginResultPayload>)packet;
    Assert(typedPacket.Success == true, "LOGIN success response must set success true.");
    Assert(typedPacket.TypedPayload.Username == "client01", "LOGIN success payload must include username.");
}

static void LoginFailureUsesErrorEnvelope()
{
    string json = JsonHelper.SerializeToJson(PacketFactory.CreateLoginFailed(
        "server",
        "client01",
        "INVALID_CREDENTIALS",
        "Username or password invalid",
        "req-0001"));

    object packet = JsonHelper.DeserializePacket(json);

    Assert(packet is Packet<EmptyPayload>, "LOGIN failure must deserialize as Packet<EmptyPayload>.");

    var typedPacket = (Packet<EmptyPayload>)packet;
    Assert(typedPacket.Success == false, "LOGIN failure response must set success false.");
    Assert(typedPacket.Error?.Code == "INVALID_CREDENTIALS", "LOGIN failure must use top-level error.code.");
    Assert(typedPacket.Payload is EmptyPayload, "LOGIN failure payload must be empty.");
}

static void OutgoingNetworkMessageRejectsEmbeddedLineBreaks()
{
    string json = """{"type":"PING","source":"client01","target":"server","requestId":"req-0001"}""";
    string unchanged = NetworkProtocol.ValidateOutgoingMessage(json);

    Assert(unchanged == json, "Valid single-line JSON must be preserved.");

    AssertThrowsArgumentException(
        () => NetworkProtocol.ValidateOutgoingMessage("""
            {"type":"PING",
            "source":"client01","target":"server","requestId":"req-0001"}
            """),
        "Embedded newline must be rejected.");

    AssertThrowsArgumentException(
        () => NetworkProtocol.ValidateOutgoingMessage("{\"type\":\"PING\"\r\n}"),
        "Embedded carriage return must be rejected.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows(Action action, string message)
{
    try
    {
        action();
    }
    catch
    {
        return;
    }

    throw new InvalidOperationException(message);
}

static void AssertThrowsArgumentException(Action action, string message)
{
    try
    {
        action();
    }
    catch (ArgumentException)
    {
        return;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(message, ex);
    }

    throw new InvalidOperationException(message);
}
