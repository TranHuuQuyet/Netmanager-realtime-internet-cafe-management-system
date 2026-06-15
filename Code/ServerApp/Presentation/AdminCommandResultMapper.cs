using ServerApp.Networking;

namespace ServerApp.Presentation;

public static class AdminCommandResultMapper
{
    // Keep network ACK details out of Program.cs and map them at the presentation boundary.
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
