namespace ClientApp.Forms;

public sealed class LockScreenForm : Form
{
    public LockScreenForm()
    {
        ClientUiTheme.ApplyClassicWindow(this, "MÁY TRẠM", new Size(258, 190));
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(4),
            BackColor = ClientUiTheme.Window
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 37F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

        root.Controls.Add(ClientUiTheme.HeaderLabel("Máy đang khóa"), 0, 0);

        var messagePanel = ClientUiTheme.ClassicPanel(9);
        messagePanel.Margin = new Padding(0, 4, 0, 3);
        messagePanel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Máy trạm đang tạm khóa. Vui lòng liên hệ máy chủ để tiếp tục sử dụng.",
            Font = ClientUiTheme.BoldFont,
            ForeColor = ClientUiTheme.Ink,
            TextAlign = ContentAlignment.MiddleCenter
        });
        root.Controls.Add(messagePanel, 0, 1);

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = ClientUiTheme.Window
        };
        buttonPanel.Controls.Add(ClientUiTheme.CommandButton("Đóng xem trước", (_, _) => Close()), 0, 0);
        root.Controls.Add(buttonPanel, 0, 2);

        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "LOCK/UNLOCK thật đang chờ route điều khiển.",
            ForeColor = ClientUiTheme.MutedInk,
            Font = ClientUiTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 3);

        Controls.Add(root);
    }
}
