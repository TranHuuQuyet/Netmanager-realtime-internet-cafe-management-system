namespace ClientApp;

public partial class ConnectForm : Form
{
    public ConnectForm()
    {
        InitializeComponent();
    }

    private void OpenLoginFormButton_Click(object sender, EventArgs e)
    {
        using LoginForm loginForm = new();
        loginForm.ShowDialog(this);
    }

    private void ExitButton_Click(object sender, EventArgs e)
    {
        Close();
    }
}
