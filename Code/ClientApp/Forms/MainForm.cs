namespace ClientApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void UnlockButton_Click(object sender, EventArgs e)
    {
        using LockForm lockForm = new();
        lockForm.ShowDialog(this);
    }

    private void LogoutButton_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void ChatButton_Click(object sender, EventArgs e)
    {
        MessageBox.Show(
            this,
            "Client chat shell is not wired yet.",
            "NetManager Client",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
