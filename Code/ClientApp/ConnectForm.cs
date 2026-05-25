namespace ClientApp;

public sealed class ConnectForm : Form
{
    public ConnectForm()
    {
        Text = "NetManager Client";
        ClientSize = new Size(420, 180);
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            Text = "Client shell is buildable. Network login binding is pending G1/G2.",
            TextAlign = ContentAlignment.MiddleCenter
        });
    }
}
