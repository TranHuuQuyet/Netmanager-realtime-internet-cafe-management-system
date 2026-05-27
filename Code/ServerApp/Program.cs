using System.Net;
using System.Net.Sockets;
using ServerApp.Auth.Contracts;
using ServerApp.Auth.Services;
using ServerApp.Networking;

namespace ServerApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Task<IAuthService> authServiceTask = Task.Run(CreateAuthServiceAsync);
        using var loginForm = new LoginForm(authServiceTask);

        if (loginForm.ShowDialog() == DialogResult.OK)
        {
            using TcpJsonLineServer? networkServer = TryStartNetworkServer();
            Application.Run(new MainForm());
        }
    }

    private static async Task<IAuthService> CreateAuthServiceAsync()
    {
        AuthRuntime authRuntime = await AuthBootstrapper.CreateAsync().ConfigureAwait(false);
        return authRuntime.Auth;
    }

    private static TcpJsonLineServer? TryStartNetworkServer()
    {
        var server = new TcpJsonLineServer(IPAddress.Loopback, 5000);

        try
        {
            server.Start();
            return server;
        }
        catch (SocketException ex)
        {
            server.Dispose();
            MessageBox.Show(
                $"Khong the mo cong TCP 127.0.0.1:5000.\n\n{ex.Message}",
                "NetManager Network",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return null;
        }
    }
}
