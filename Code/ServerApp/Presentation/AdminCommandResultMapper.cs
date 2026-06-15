using ServerApp.Networking;

namespace ServerApp.Presentation;

public static class AdminCommandResultMapper
{
    public static AdminCommandResult FromNetworkAck(MachineCommandAckResult result)
        => new(
            result.MachineId,
            result.Command,
            result.Status,
            result.Message,
            result.IsError,
            result.ErrorCode,
            result.RequestId);
}
