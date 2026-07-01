using System.Net;
using System.Net.Sockets;
using ServerApp.Auth.Contracts;
using ServerApp.Auth.Services;
using ServerApp.Networking;
using ServerApp.Presentation;

namespace ServerApp;

static class Program
{
    private const int NetworkPort = 5000;
    private static readonly IPAddress NetworkBindAddress = IPAddress.Any;

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
            var billingService = new NetworkAdminBillingService(
                authRuntime.Billing,
                authRuntime.SessionRepository,
                networkServer);
            using var mainForm = new MainForm(authRuntime.Machines, networkServer, billingService, authRuntime.Customers);

            if (networkServer is not null)
            {
                networkServer.StatusEmitted += status =>
                {
                    mainForm.ApplyMachineStatusUpdate(status.MachineId, status.Status);
                    if (string.Equals(status.Status, "Online", StringComparison.OrdinalIgnoreCase))
                    {
                        _ = mainForm.SyncBillingForMachineAsync(status.MachineId);
                    }
                };
                // Network emits typed command results; presentation owns the UI-facing shape.
                networkServer.CommandResultEmitted += result =>
                    mainForm.ApplyCommandResultUpdate(AdminCommandResultMapper.FromNetworkAck(result));
            }

            _ = mainForm.RefreshBillingSessionsAsync();

            Application.Run(mainForm);
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
            NetworkBindAddress,
            NetworkPort,
            new PacketDispatcher(authRuntime.Auth, authRuntime.SessionRepository, authRuntime.Machines),
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
                $"Khong the mo cong TCP {NetworkBindAddress}:{NetworkPort}.\n\n{ex.Message}",
                "NetManager Network",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return null;
        }
    }
}
