namespace ClientApp.Forms;

public sealed class ClientMainForm : Form
{
    private const string TotalTime = "01:59:01";
    private const string UsedTime = "00:00:45";
    private const string RemainingTime = "01:58:16";
    private const string PlayCost = "63(VND)";
    private const string LoginTime = "11/17/2017 11:41:39 PM";

    public ClientMainForm(string username, string machineId, string host, int port)
    {
        Text = "Máy trạm";
        ClientSize = new Size(420, 380);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Padding = new Padding(16);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = MachineCaption(machineId),
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 0);
        root.Controls.Add(BuildInfoLayout(), 0, 1);
        root.Controls.Add(BuildActionStrip(), 0, 2);
        Controls.Add(root);

        ToolTip tooltip = new();
        tooltip.SetToolTip(this, $"Preview only: {username}/{machineId} at {host}:{port}. Runtime binding waits for M2 route.");
    }

    private static string MachineCaption(string machineId)
    {
        if (string.Equals(machineId, "PC-01", StringComparison.OrdinalIgnoreCase))
        {
            return "Máy 1";
        }

        return string.IsNullOrWhiteSpace(machineId) ? "Máy 1" : machineId.Trim();
    }

    private static Control BuildInfoLayout()
    {
        var infoLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Margin = new Padding(0, 8, 0, 8)
        };
        infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142F));
        infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (var row = 0; row < 5; row++)
        {
            infoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        }

        AddInfoRow(infoLayout, 0, "Tổng thời gian", TotalTime);
        AddInfoRow(infoLayout, 1, "Thời gian sử dụng", UsedTime);
        AddInfoRow(infoLayout, 2, "Thời gian còn lại", RemainingTime);
        AddInfoRow(infoLayout, 3, "Chi phí giờ chơi", PlayCost);
        AddInfoRow(infoLayout, 4, "Giờ đăng nhập", LoginTime);
        return infoLayout;
    }

    private static void AddInfoRow(TableLayoutPanel layout, int row, string label, string value)
    {
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = label,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        layout.Controls.Add(new TextBox
        {
            Dock = DockStyle.Fill,
            Text = value,
            ReadOnly = true,
            TabStop = false,
            Margin = new Padding(3, 9, 3, 3)
        }, 1, row);
    }

    private Control BuildActionStrip()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0)
        };

        panel.Controls.Add(BuildButton("Giao tiếp", Communication_Click));
        panel.Controls.Add(BuildButton("Đăng xuất", Logout_Click));
        panel.Controls.Add(BuildButton("Đổi mật khẩu", ChangePassword_Click));
        return panel;
    }

    private static Button BuildButton(string text, EventHandler clickHandler)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(115, 32),
            Margin = new Padding(8, 0, 0, 0),
            UseVisualStyleBackColor = true
        };
        button.Click += clickHandler;
        return button;
    }

    private void ChangePassword_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(this, "Chức năng đổi mật khẩu đang chờ tích hợp hệ thống xác thực.", "Mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void Logout_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void Communication_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(this, "Giao tiếp với máy chủ sẽ được bật sau khi route CHAT sẵn sàng.", "Giao tiếp", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
