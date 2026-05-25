namespace ClientApp;

public partial class LockForm : Form
{
    public LockForm()
    {
        InitializeComponent();
    }

    private void UnlockButton_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
        Close();
    }
}
