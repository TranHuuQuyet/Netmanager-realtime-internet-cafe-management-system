using System.Text.Json;
using Shared.DTOs.CommandPayloads;
using Shared.Packets;
using Shared.Utilities.JsonHelper;

namespace ClientApp.Networking;

public sealed class ClientRuntimeCommandHandler : IDisposable
{
    private readonly TcpClientConnection _connection;
    private readonly string _machineId;

    public ClientRuntimeCommandHandler(TcpClientConnection connection, string machineId)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _machineId = machineId?.Trim() ?? string.Empty;
        _connection.MessageReceived += Connection_MessageReceived;
    }

    public event Action<LockPayload>? LockRequested;

    public event Action<UnlockPayload>? UnlockRequested;

    public event Action? InvalidPacketIgnored;

    private void Connection_MessageReceived(string message)
    {
        try
        {
            object packet = JsonHelper.DeserializePacket(message);

            switch (packet)
            {
                case Packet<LockPayload> lockPacket
                    when IsCommandForThisMachine(lockPacket.TypedPayload.MachineId, lockPacket.Target):
                    LockRequested?.Invoke(lockPacket.TypedPayload);
                    break;
                case Packet<UnlockPayload> unlockPacket
                    when IsCommandForThisMachine(unlockPacket.TypedPayload.MachineId, unlockPacket.Target):
                    UnlockRequested?.Invoke(unlockPacket.TypedPayload);
                    break;
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException or JsonException)
        {
            InvalidPacketIgnored?.Invoke();
        }
    }

    private bool IsCommandForThisMachine(string? payloadMachineId, string? packetTarget)
    {
        string commandMachineId = string.IsNullOrWhiteSpace(payloadMachineId)
            ? packetTarget?.Trim() ?? string.Empty
            : payloadMachineId.Trim();

        return string.Equals(commandMachineId, _machineId, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _connection.MessageReceived -= Connection_MessageReceived;
    }
}
