namespace ClientApp.Forms;

public sealed class LockScreenForm : Form
{
    private bool _unlockedByServer;

    public LockScreenForm()
    {
        Text = "Máy trạm bị khóa";
        ClientSize = new Size(258, 190);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        StartPosition = FormStartPosition.CenterParent;
        TopMost = true;
        Padding = new Padding(12);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "MÁY ĐANG BỊ KHÓA",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Máy trạm đã bị quản trị viên khóa.\nVui lòng liên hệ quầy để tiếp tục sử dụng.",
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 1);
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Đang chờ lệnh mở khóa từ máy chủ.",
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 2);

        Controls.Add(layout);
        FormClosing += LockScreenForm_FormClosing;
    }

    // The runtime command handler will call this after receiving UNLOCK from the server.
    public void UnlockFromServer()
    {
        if (InvokeRequired)
        {
            BeginInvoke(UnlockFromServer);
            return;
        }

        _unlockedByServer = true;
        Close();
    }

    private void LockScreenForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_unlockedByServer && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
        }
    }
}
