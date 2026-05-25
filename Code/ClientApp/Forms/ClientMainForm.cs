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
        ClientUiTheme.ApplyClassicWindow(this, "MÁY TRẠM", new Size(420, 380));
        StartPosition = FormStartPosition.CenterParent;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8),
            BackColor = ClientUiTheme.Window
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));

        root.Controls.Add(ClientUiTheme.HeaderLabel(MachineCaption(machineId)), 0, 0);
        root.Controls.Add(BuildInfoPanel(), 0, 1);
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

    private static Panel BuildInfoPanel()
    {
        var infoPanel = ClientUiTheme.ClassicPanel(12);
        infoPanel.Margin = new Padding(0, 8, 0, 8);

        var infoLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5
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
        AddInfoRow(infoLayout, 4, "Giờ Đăng Nhập", LoginTime);

        infoPanel.Controls.Add(infoLayout);
        return infoPanel;
    }

    private static void AddInfoRow(TableLayoutPanel layout, int row, string label, string value)
    {
        layout.Controls.Add(ClientUiTheme.FieldLabel(label), 0, row);
        layout.Controls.Add(ClientUiTheme.ValueBox(value), 1, row);
    }

    private Control BuildActionStrip()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = ClientUiTheme.Window,
            Padding = new Padding(0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

        panel.Controls.Add(ClientUiTheme.ActionButton("Mật khẩu", DrawKeyIcon, ChangePassword_Click), 0, 0);
        panel.Controls.Add(ClientUiTheme.ActionButton("Đăng xuất", DrawPowerIcon, Logout_Click), 1, 0);
        panel.Controls.Add(ClientUiTheme.ActionButton("Giao tiếp", DrawChatIcon, Communication_Click), 2, 0);
        return panel;
    }

    private static void DrawKeyIcon(Graphics graphics, Rectangle bounds)
    {
        using Pen pen = new(Color.Black, 3F);
        graphics.DrawEllipse(pen, bounds.X + 2, bounds.Y + 2, 11, 11);
        graphics.DrawLine(pen, bounds.X + 12, bounds.Y + 13, bounds.Right - 2, bounds.Bottom - 2);
        graphics.DrawLine(pen, bounds.Right - 10, bounds.Bottom - 2, bounds.Right - 10, bounds.Bottom - 8);
        graphics.DrawLine(pen, bounds.Right - 5, bounds.Bottom - 2, bounds.Right - 5, bounds.Bottom - 6);
    }

    private static void DrawPowerIcon(Graphics graphics, Rectangle bounds)
    {
        using Pen pen = new(Color.Black, 2.5F);
        graphics.DrawArc(pen, bounds.X + 5, bounds.Y + 5, 20, 20, 130, 280);
        graphics.DrawLine(pen, bounds.X + 15, bounds.Y + 2, bounds.X + 15, bounds.Y + 14);
    }

    private static void DrawChatIcon(Graphics graphics, Rectangle bounds)
    {
        using SolidBrush brush = new(Color.Black);
        graphics.FillRectangle(brush, bounds.X + 2, bounds.Y + 4, 24, 15);
        Point[] tail =
        [
            new(bounds.X + 8, bounds.Y + 19),
            new(bounds.X + 6, bounds.Y + 25),
            new(bounds.X + 15, bounds.Y + 19)
        ];
        graphics.FillPolygon(brush, tail);

        using SolidBrush dotBrush = new(Color.White);
        graphics.FillEllipse(dotBrush, bounds.X + 7, bounds.Y + 10, 3, 3);
        graphics.FillEllipse(dotBrush, bounds.X + 14, bounds.Y + 10, 3, 3);
        graphics.FillEllipse(dotBrush, bounds.X + 21, bounds.Y + 10, 3, 3);
    }

    private void ChangePassword_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(this, "Chức năng đổi mật khẩu đang chờ tích hợp hệ thống xác thực.", "Mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void Logout_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void InitializeComponent()
    {

    }

    private void Communication_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(this, "Giao tiếp với máy chủ sẽ được bật sau khi route CHAT sẵn sàng.", "Giao tiếp", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
