using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;

namespace ServerApp;

public partial class LoginForm : Form
{
    private readonly Task<IAuthService> _authServiceTask;

    public LoginForm(Task<IAuthService> authServiceTask)
    {
        _authServiceTask = authServiceTask ?? throw new ArgumentNullException(nameof(authServiceTask));

        InitializeComponent();
        btnLogin.DialogResult = DialogResult.None;
        Shown += LoginForm_Shown;
    }

    private async void LoginForm_Shown(object? sender, EventArgs e)
    {
        try
        {
            await _authServiceTask;
        }
        catch (Exception)
        {
            lblMessage.Text = "Authentication data could not be initialized.";
            btnLogin.Enabled = false;
        }
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
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

        btnLogin.Enabled = false;

        try
        {
            IAuthService authService = await _authServiceTask;
            AuthResult result = await authService.AuthenticateAsync(
                new AuthRequest(
                    txtUsername.Text,
                    txtPassword.Text,
                    txtMachineId.Text,
                    UserRole.Admin));

            if (!result.IsSuccess)
            {
                lblMessage.Text = result.Message;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception)
        {
            lblMessage.Text = "Authentication could not be completed.";
        }
        finally
        {
            btnLogin.Enabled = true;
        }
    }

    private void ShowValidationMessage(string message, Control focusTarget)
    {
        lblMessage.Text = message;
        focusTarget.Focus();
    }
}