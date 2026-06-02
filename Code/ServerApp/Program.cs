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

        Task<AuthRuntime> authRuntimeTask = Task.Run(CreateAuthRuntimeAsync);
        Task<IAuthService> authServiceTask = GetAuthServiceAsync(authRuntimeTask);
        using var loginForm = new LoginForm(authServiceTask);

        if (loginForm.ShowDialog() == DialogResult.OK)
        {
            AuthRuntime authRuntime = authRuntimeTask.GetAwaiter().GetResult();
            using TcpJsonLineServer? networkServer = TryStartNetworkServer(authRuntime);
            Application.Run(new MainForm());
        }
    }

    private static Task<AuthRuntime> CreateAuthRuntimeAsync()
    {
        return AuthBootstrapper.CreateAsync();
    }

    private static async Task<IAuthService> GetAuthServiceAsync(Task<AuthRuntime> authRuntimeTask)
    {
        AuthRuntime authRuntime = await authRuntimeTask.ConfigureAwait(false);
        return authRuntime.Auth;
    }

    private static TcpJsonLineServer? TryStartNetworkServer(AuthRuntime authRuntime)
    {
        var server = new TcpJsonLineServer(
            IPAddress.Loopback,
            5000,
            new PacketDispatcher(authRuntime.Auth),
            authRuntime.SessionService);

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
