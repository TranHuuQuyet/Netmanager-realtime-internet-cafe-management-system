using System.Text.Json;
using Shared.DTOs.Bidrectional;
using Shared.DTOs.CommandPayloads;
using Shared.DTOs.RequestPayloads;
using Shared.Networking;
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

    public event Action<Packet<LockPayload>>? LockRequested;

    public event Action<Packet<UnlockPayload>>? UnlockRequested;

    public event Action<Packet<ShutdownPayload>>? ShutdownRequested;

    public event Action<Packet<ChatPayload>>? ChatReceived;

    public event Action<Packet<NotificationPayload>>? NotificationReceived;

    public event Action<Packet<TimerPayload>>? TimerReceived;

    public event Action? InvalidPacketIgnored;

    private void Connection_MessageReceived(string message)
    {
        try
        {
            object packet = JsonHelper.DeserializePacket(message);

            switch (packet)
            {
                case Packet<LockPayload> lockPacket:
                    if (IsCommandForThisMachine(lockPacket.TypedPayload.MachineId, lockPacket.Target))
                    {
                        LockRequested?.Invoke(lockPacket);
                    }
                    else
                    {
                        _ = SendCommandAckAsync(
                            lockPacket,
                            ResolveCommandMachineId(lockPacket.TypedPayload.MachineId, lockPacket.Target),
                            "Ignored",
                            "LOCK ignored because target machine does not match this client.");
                    }

                    break;
                case Packet<UnlockPayload> unlockPacket:
                    if (IsCommandForThisMachine(unlockPacket.TypedPayload.MachineId, unlockPacket.Target))
                    {
                        UnlockRequested?.Invoke(unlockPacket);
                    }
                    else
                    {
                        _ = SendCommandAckAsync(
                            unlockPacket,
                            ResolveCommandMachineId(unlockPacket.TypedPayload.MachineId, unlockPacket.Target),
                            "Ignored",
                            "UNLOCK ignored because target machine does not match this client.");
                    }

                    break;
                case Packet<ShutdownPayload> shutdownPacket:
                    if (IsCommandForThisMachine(shutdownPacket.TypedPayload.MachineId, shutdownPacket.Target))
                    {
                        ShutdownRequested?.Invoke(shutdownPacket);
                    }
                    else
                    {
                        _ = SendCommandAckAsync(
                            shutdownPacket,
                            ResolveCommandMachineId(shutdownPacket.TypedPayload.MachineId, shutdownPacket.Target),
                            "Ignored",
                            "SHUTDOWN ignored because target machine does not match this client.");
                    }

                    break;
                case Packet<ChatPayload> chatPacket:
                    if (IsChatForThisMachine(chatPacket.TypedPayload, chatPacket.Target))
                    {
                        ChatReceived?.Invoke(chatPacket);
                    }

                    break;
                case Packet<NotificationPayload> notificationPacket:
                    if (IsNotificationForThisMachine(notificationPacket.TypedPayload, notificationPacket.Target))
                    {
                        NotificationReceived?.Invoke(notificationPacket);
                    }

                    break;
                case Packet<TimerPayload> timerPacket:
                    if (IsCommandForThisMachine(timerPacket.TypedPayload.MachineId, timerPacket.Target))
                    {
                        TimerReceived?.Invoke(timerPacket);
                    }

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
        string commandMachineId = ResolveCommandMachineId(payloadMachineId, packetTarget);

        return string.Equals(commandMachineId, _machineId, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveCommandMachineId(string? payloadMachineId, string? packetTarget)
        => string.IsNullOrWhiteSpace(payloadMachineId)
            ? packetTarget?.Trim() ?? string.Empty
            : payloadMachineId.Trim();

    private bool IsChatForThisMachine(ChatPayload payload, string? packetTarget)
    {
        string receiver = string.IsNullOrWhiteSpace(payload.Receiver)
            ? packetTarget?.Trim() ?? string.Empty
            : payload.Receiver.Trim();

        return string.Equals(receiver, _machineId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(packetTarget?.Trim(), _machineId, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsNotificationForThisMachine(NotificationPayload payload, string? packetTarget)
    {
        string target = packetTarget?.Trim() ?? string.Empty;
        string scope = payload.Scope?.Trim() ?? string.Empty;

        return string.Equals(target, _machineId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase)
            || string.Equals(scope, "Broadcast", StringComparison.OrdinalIgnoreCase);
    }

    private async Task SendCommandAckAsync(Packet commandPacket, string machineId, string status, string message)
    {
        try
        {
            var ackPacket = PacketFactory.CreateAck(
                source: _machineId,
                target: NetworkProtocol.ServerSource,
                payload: new AckPayload
                {
                    MachineId = string.IsNullOrWhiteSpace(machineId) ? _machineId : machineId,
                    AckFor = commandPacket.Type.ToString(),
                    Status = status,
                    Message = message
                },
                requestId: commandPacket.RequestId);

            await _connection.SendAsync(JsonHelper.SerializeToJson(ackPacket)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ObjectDisposedException)
        {
            InvalidPacketIgnored?.Invoke();
        }
    }

    public void Dispose()
    {
        _connection.MessageReceived -= Connection_MessageReceived;
    }
}
