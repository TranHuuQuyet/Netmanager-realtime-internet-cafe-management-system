// Namespace goc cua ung dung client.
namespace ClientApp;

// Cau hinh khoi dong client: ma may, dia chi server va cong server.
public sealed record ClientLaunchOptions(string MachineId, string ServerHost, int ServerPort)
{
    // Gia tri mac dinh neu nguoi dung khong truyen tham so dong lenh.
    public const string DefaultMachineId = "PC-01";
    public const string DefaultServerHost = "127.0.0.1";
    public const int DefaultServerPort = 5000;

    // Doi tuong options mac dinh.
    public static ClientLaunchOptions Default { get; } =
        new(DefaultMachineId, DefaultServerHost, DefaultServerPort);

    // Doc tham so dong lenh thanh ClientLaunchOptions.
    // Ho tro dang "--machine-id PC01" va "--machine-id=PC01".
    public static bool TryParse(string[] args, out ClientLaunchOptions options, out string error)
    {
        string machineId = Default.MachineId;
        string serverHost = Default.ServerHost;
        int serverPort = Default.ServerPort;

        // Duyet tung tham so va lay gia tri tuong ung.
        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];

            // Neu tham so khong thuoc 3 option duoc ho tro thi bao loi.
            if (!TryGetValue(args, ref index, argument, "--machine-id", out string? value)
                && !TryGetValue(args, ref index, argument, "--server-host", out value)
                && !TryGetValue(args, ref index, argument, "--server-port", out value))
            {
                options = Default;
                error = $"Tham số không được hỗ trợ: {argument}";
                return false;
            }

            // Gan gia tri vao bien cau hinh tuong ung.
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

        // Chuan hoa chuoi va validate cac gia tri bat buoc.
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

    // Lay gia tri cua mot option dong lenh.
    private static bool TryGetValue(
        string[] args,
        ref int index,
        string argument,
        string optionName,
        out string? value)
    {
        // Dang "--option value".
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

        // Dang "--option=value".
        string prefix = $"{optionName}=";
        if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = argument[prefix.Length..];
            return true;
        }

        value = null;
        return false;
    }

    // Kiem tra argument co phai option can xu ly khong.
    private static bool MatchesOption(string argument, string optionName) =>
        string.Equals(argument, optionName, StringComparison.OrdinalIgnoreCase)
        || argument.StartsWith($"{optionName}=", StringComparison.OrdinalIgnoreCase);
}
