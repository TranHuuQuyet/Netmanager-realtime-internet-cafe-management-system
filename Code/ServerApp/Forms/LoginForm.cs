namespace ServerApp;

public partial class LoginForm : Form
{
    public LoginForm()
    {
        InitializeComponent();

        txtUsername.Text = "admin";
        txtPassword.Text = "123";
        txtMachineId.Text = "PC00";
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblMessage.Text = ValidateInputs();

        if (lblMessage.Text.Length > 0)
        {
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private string ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            return UiStrings.LoginUsernameRequired;
        }

        if (string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            return UiStrings.LoginPasswordRequired;
        }

        if (string.IsNullOrWhiteSpace(txtMachineId.Text))
        {
            return UiStrings.LoginMachineIdRequired;
        }

        return string.Empty;
    }
}
