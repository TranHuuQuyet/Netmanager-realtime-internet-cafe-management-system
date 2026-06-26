namespace ClientApp;

public sealed record ClientLaunchOptions(string MachineId, string ServerHost, int ServerPort)
{
    public const string DefaultMachineId = "PC-01";
    public const string DefaultServerHost = "127.0.0.1";
    public const int DefaultServerPort = 5000;

    public static ClientLaunchOptions Default { get; } =
        new(DefaultMachineId, DefaultServerHost, DefaultServerPort);

    public static bool TryParse(string[] args, out ClientLaunchOptions options, out string error)
    {
        string machineId = Default.MachineId;
        string serverHost = Default.ServerHost;
        int serverPort = Default.ServerPort;

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];

            if (!TryGetValue(args, ref index, argument, "--machine-id", out string? value)
                && !TryGetValue(args, ref index, argument, "--server-host", out value)
                && !TryGetValue(args, ref index, argument, "--server-port", out value))
            {
                options = Default;
                error = $"Tham số không được hỗ trợ: {argument}";
                return false;
            }

            if (MatchesOption(argument, "--machine-id"))
            {
                machineId = value!;
            }
            else if (MatchesOption(argument, "--server-host"))
            {
                serverHost = value!;
            }
            else if (!int.TryParse(value, out serverPort) || serverPort is < 1 or > 65535)
            {
                options = Default;
                error = "Cổng máy chủ phải là số từ 1 đến 65535.";
                return false;
            }
        }

        machineId = machineId.Trim();
        serverHost = serverHost.Trim();

        if (machineId.Length == 0)
        {
            options = Default;
            error = "Mã máy trạm không được để trống.";
            return false;
        }

        if (serverHost.Length == 0)
        {
            options = Default;
            error = "Địa chỉ máy chủ không được để trống.";
            return false;
        }

        options = new ClientLaunchOptions(machineId, serverHost, serverPort);
        error = string.Empty;
        return true;
    }

    private static bool TryGetValue(
        string[] args,
        ref int index,
        string argument,
        string optionName,
        out string? value)
    {
        if (string.Equals(argument, optionName, StringComparison.OrdinalIgnoreCase))
        {
            if (index + 1 >= args.Length
                || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = string.Empty;
                return true;
            }

            value = args[++index];
            return true;
        }

        string prefix = $"{optionName}=";
        if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = argument[prefix.Length..];
            return true;
        }

        value = null;
        return false;
    }

    private static bool MatchesOption(string argument, string optionName) =>
        string.Equals(argument, optionName, StringComparison.OrdinalIgnoreCase)
        || argument.StartsWith($"{optionName}=", StringComparison.OrdinalIgnoreCase);
}
