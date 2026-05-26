using ClientApp.Forms;

namespace ClientApp;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (!ClientLaunchOptions.TryParse(args, out ClientLaunchOptions options, out string error))
        {
            MessageBox.Show(
                $"Không thể khởi động máy trạm.\n\n{error}\n\n"
                + "Tham số hỗ trợ: --machine-id, --server-host, --server-port.",
                "Cấu hình NetManager Client",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new ConnectForm(options));
    }
}
