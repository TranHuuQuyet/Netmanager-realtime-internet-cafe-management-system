using System.IO;
using NETManager.Shared.DTOs.RequestPayloads;
using NETManager.Shared.Packets;
using NETManager.Shared.Utilities.JsonHelper;

namespace ServerApp.Networking;

public sealed class PacketDispatcher
{
    public string Dispatch(string inboundLine)
    {
        object packet = JsonHelper.DeserializePacket(inboundLine);

        return packet switch
        {
            Packet<LoginPayload> loginPacket => DispatchLoginBaseline(loginPacket),
            Packet typedPacket => throw new InvalidDataException($"Unsupported packet type: {typedPacket.Type}"),
            _ => throw new InvalidDataException("Unsupported packet envelope.")
        };
    }

    private static string DispatchLoginBaseline(Packet<LoginPayload> loginPacket)
    {
        var ackPacket = PacketFactory.CreateAck(
            source: NETManager.Shared.Networking.NetworkProtocol.ServerSource,
            target: loginPacket.Source,
            payload: new AckPayload
            {
                MachineId = loginPacket.TypedPayload.MachineId,
                AckFor = loginPacket.Type.ToString(),
                Status = "Success",
                Message = $"Round-trip received for {loginPacket.TypedPayload.Username}"
            },
            requestId: loginPacket.RequestId);

        return NETManager.Shared.Networking.NetworkProtocol.ValidateOutgoingMessage(
            JsonHelper.SerializeToJson(ackPacket));
    }
}
