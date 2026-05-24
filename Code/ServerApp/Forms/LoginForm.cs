using ServerApp.Auth.Models;
using ServerApp.Auth.Services;

namespace ServerApp;


public partial class LoginForm : Form
{
    private readonly Task<AuthRuntime> _authRuntimeTask;

    public LoginForm()
    {
        InitializeComponent();
        btnLogin.DialogResult = DialogResult.None;
        btnLogin.Click += BtnLogin_Click;
        _authRuntimeTask = AuthBootstrapper.CreateAsync();
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblMessage.Text = string.Empty;
        btnLogin.Enabled = false;

        try
        {
            AuthRuntime runtime = await _authRuntimeTask.ConfigureAwait(true);
            AuthResult result = await runtime.Auth.AuthenticateAsync(
                new AuthRequest(
                    txtUsername.Text,
                    txtPassword.Text,
                    txtMachineId.Text,
                    UserRole.Admin),
                CancellationToken.None).ConfigureAwait(true);

            if (result.IsSuccess)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            lblMessage.Text = "Sai thông tin đăng nhập.";
        }
        catch
        {
            lblMessage.Text = "Không thể kiểm tra đăng nhập lúc này.";
        }
        finally
        {
            btnLogin.Enabled = true;
        }
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblMessage.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            ShowValidationMessage(UiStrings.LoginUsernameRequired, txtUsername);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            ShowValidationMessage(UiStrings.LoginPasswordRequired, txtPassword);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtMachineId.Text))
        {
            ShowValidationMessage(UiStrings.LoginMachineIdRequired, txtMachineId);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowValidationMessage(string message, Control focusTarget)
    {
        lblMessage.Text = message;
        focusTarget.Focus();
    }
}
