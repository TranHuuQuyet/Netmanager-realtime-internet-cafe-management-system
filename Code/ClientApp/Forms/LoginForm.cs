namespace ClientApp;

public partial class LoginForm : Form
{
    public LoginForm()
    {
        InitializeComponent();
    }

    private void OpenMainFormButton_Click(object sender, EventArgs e)
    {
        using MainForm mainForm = new();
        mainForm.ShowDialog(this);
    }

    private void BackButton_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
