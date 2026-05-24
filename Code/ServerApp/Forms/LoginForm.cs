namespace ServerApp;


public partial class LoginForm : Form
{
    public LoginForm()
    {
        InitializeComponent();
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
