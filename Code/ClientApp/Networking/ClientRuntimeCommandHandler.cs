using System.Text.Json;
using Shared.DTOs.Bidrectional;
using Shared.DTOs.CommandPayloads;
using Shared.DTOs.RequestPayloads;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;

// Namespace chua code network cua ClientApp.
namespace ClientApp.Networking;

// Lop lang nghe packet tu server va phat event tuong ung cho UI client xu ly.
public sealed class ClientRuntimeCommandHandler : IDisposable
{
    // Ket noi TCP hien tai va ma may cua client nay.
    private readonly TcpClientConnection _connection;
    private readonly string _machineId;

    // Dang ky lang nghe MessageReceived tu TcpClientConnection.
    public ClientRuntimeCommandHandler(TcpClientConnection connection, string machineId)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _machineId = machineId?.Trim() ?? string.Empty;
        _connection.MessageReceived += Connection_MessageReceived;
    }

    // Cac event cho UI biet server yeu cau thao tac nao.
    public event Action<Packet<LockPayload>>? LockRequested;

    public event Action<Packet<UnlockPayload>>? UnlockRequested;

    public event Action<Packet<ShutdownPayload>>? ShutdownRequested;

    public event Action<Packet<ChatPayload>>? ChatReceived;

    public event Action<Packet<NotificationPayload>>? NotificationReceived;

    public event Action<Packet<TimerPayload>>? TimerReceived;

    public event Action? InvalidPacketIgnored;

    // Xu ly chuoi JSON nhan tu server: deserialize thanh packet va phan loai theo payload.
    private void Connection_MessageReceived(string message)
    {
        try
        {
            object packet = JsonHelper.DeserializePacket(message);

            // Pattern matching giup chay dung nhanh theo kieu packet cu the.
            switch (packet)
            {
                case Packet<LockPayload> lockPacket:
                    // Neu command dung may nay thi phat event, neu sai may thi gui ACK Ignored.
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
                    // Xu ly lenh mo khoa may.
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
                    // Xu ly lenh shutdown.
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
                    // Chi nhan chat neu receiver/target la may nay.
                    if (IsChatForThisMachine(chatPacket.TypedPayload, chatPacket.Target))
                    {
                        ChatReceived?.Invoke(chatPacket);
                    }

                    break;
                case Packet<NotificationPayload> notificationPacket:
                    // Nhan thong bao neu gui truc tiep den may nay hoac broadcast.
                    if (IsNotificationForThisMachine(notificationPacket.TypedPayload, notificationPacket.Target))
                    {
                        NotificationReceived?.Invoke(notificationPacket);
                    }

                    break;
                case Packet<TimerPayload> timerPacket:
                    // Dong bo timer billing neu timer thuoc may nay.
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

    // Kiem tra command co danh cho client hien tai khong.
    private bool IsCommandForThisMachine(string? payloadMachineId, string? packetTarget)
    {
        string commandMachineId = ResolveCommandMachineId(payloadMachineId, packetTarget);

        return string.Equals(commandMachineId, _machineId, StringComparison.OrdinalIgnoreCase);
    }

    // Uu tien machineId trong payload; neu rong thi dung target cua packet.
    private static string ResolveCommandMachineId(string? payloadMachineId, string? packetTarget)
        => string.IsNullOrWhiteSpace(payloadMachineId)
            ? packetTarget?.Trim() ?? string.Empty
            : payloadMachineId.Trim();

    // Kiem tra chat co gui den may nay khong.
    private bool IsChatForThisMachine(ChatPayload payload, string? packetTarget)
    {
        string receiver = string.IsNullOrWhiteSpace(payload.Receiver)
            ? packetTarget?.Trim() ?? string.Empty
            : payload.Receiver.Trim();

        return string.Equals(receiver, _machineId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(packetTarget?.Trim(), _machineId, StringComparison.OrdinalIgnoreCase);
    }

    // Kiem tra notification co gui den may nay hoac broadcast khong.
    private bool IsNotificationForThisMachine(NotificationPayload payload, string? packetTarget)
    {
        string target = packetTarget?.Trim() ?? string.Empty;
        string scope = payload.Scope?.Trim() ?? string.Empty;

        return string.Equals(target, _machineId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase)
            || string.Equals(scope, "Broadcast", StringComparison.OrdinalIgnoreCase);
    }

    // Gui ACK ve server khi client bo qua command hoac can bao trang thai xu ly.
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

    // Huy dang ky event de tranh memory leak khi handler khong con dung.
    public void Dispose()
    {
        _connection.MessageReceived -= Connection_MessageReceived;
    }
}
