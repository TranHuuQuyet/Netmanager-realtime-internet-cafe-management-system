using ServerApp.Auth.Contracts;
using ServerApp.Auth.Services;

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
            Application.Run(new MainForm());
        }
    }

    private static async Task<IAuthService> CreateAuthServiceAsync()
    {
        AuthRuntime authRuntime = await AuthBootstrapper.CreateAsync().ConfigureAwait(false);
        return authRuntime.Auth;
    }
}
