using ClientApp;

namespace ClientApp.Forms;

public partial class ConnectForm : Form
{
    private readonly ClientLaunchOptions _launchOptions;

    public ConnectForm(ClientLaunchOptions launchOptions)
    {
        _launchOptions = launchOptions ?? throw new ArgumentNullException(nameof(launchOptions));

        InitializeComponent();
        txtMachineId.Text = _launchOptions.MachineId;
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblMessage.ForeColor = Color.FromArgb(170, 45, 45);
        lblMessage.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            ShowValidationMessage("Vui lòng nhập tên tài khoản.", txtUsername);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            ShowValidationMessage("Vui lòng nhập mật khẩu.", txtPassword);
            return;
        }

        lblMessage.ForeColor = SystemColors.ControlText;
        lblMessage.Text = "Đăng nhập máy chủ chưa được tích hợp.";
    }

    private void BtnExit_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void ShowValidationMessage(string message, Control focusTarget)
    {
        lblMessage.Text = message;
        focusTarget.Focus();
    }
}
