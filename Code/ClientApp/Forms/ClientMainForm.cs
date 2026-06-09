using ClientApp.Networking;
using Shared.DTOs.RequestPayloads;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;

namespace ClientApp.Forms;

public sealed class ClientMainForm : Form
{
    private readonly TcpClientConnection _connection;
    private readonly DateTime _loginTimeUtc;
    private readonly string _machineId;
    private readonly string _sessionId;
    private readonly string _serverEndpoint;
    private readonly System.Windows.Forms.Timer _sessionTimer = new();
    private TextBox _usedTimeTextBox = null!;
    private TextBox _serverTextBox = null!;

    public ClientMainForm(
        string username,
        string machineId,
        string host,
        int port,
        string sessionId,
        DateTime loginTimeUtc,
        TcpClientConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _loginTimeUtc = DateTime.SpecifyKind(loginTimeUtc, DateTimeKind.Utc);
        _machineId = machineId.Trim();
        _sessionId = sessionId.Trim();
        _serverEndpoint = $"{host}:{port}";

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
        root.Controls.Add(BuildInfoLayout(username, sessionId, host, port), 0, 1);
        root.Controls.Add(BuildActionStrip(), 0, 2);
        Controls.Add(root);

        ToolTip tooltip = new();
        tooltip.SetToolTip(this, $"Đã đăng nhập {username}/{machineId} tại {host}:{port}.");

        _sessionTimer.Interval = 1000;
        _sessionTimer.Tick += (_, _) => UpdateUsedTime();
        _sessionTimer.Start();
        UpdateUsedTime();

        _connection.Connected += Connection_Connected;
        _connection.Disconnected += Connection_Disconnected;
        _connection.ReconnectFailed += Connection_ReconnectFailed;
        UpdateServerConnectionStatus(_connection.IsConnected
            ? "connected"
            : "disconnected - reconnecting");
    }

    private static string MachineCaption(string machineId)
    {
        if (string.Equals(machineId, "PC-01", StringComparison.OrdinalIgnoreCase))
        {
            return "Máy 1";
        }

        return string.IsNullOrWhiteSpace(machineId) ? "Máy 1" : machineId.Trim();
    }

    private Control BuildInfoLayout(string username, string sessionId, string host, int port)
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

        AddInfoRow(infoLayout, 0, "Tài khoản", username);
        AddInfoRow(infoLayout, 1, "Mã phiên", ShortSessionId(sessionId));
        _usedTimeTextBox = AddInfoRow(infoLayout, 2, "Thời gian sử dụng", "00:00:00");
        AddInfoRow(infoLayout, 3, "Máy chủ", $"{host}:{port}");
        AddInfoRow(infoLayout, 4, "Giờ đăng nhập", FormatLoginTime(_loginTimeUtc));
        _serverTextBox = infoLayout.GetControlFromPosition(1, 3) as TextBox ?? _serverTextBox;
        return infoLayout;
    }

    private static TextBox AddInfoRow(TableLayoutPanel layout, int row, string label, string value)
    {
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = label,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        var textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = value,
            ReadOnly = true,
            TabStop = false,
            Margin = new Padding(3, 9, 3, 3)
        };

        layout.Controls.Add(textBox, 1, row);
        return textBox;
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

    private void UpdateUsedTime()
    {
        TimeSpan elapsed = DateTime.UtcNow - _loginTimeUtc;

        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        _usedTimeTextBox.Text = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

    private async void Connection_Connected()
    {
        UpdateServerConnectionStatus("connected");
        await SendResumeStatusAsync();
    }

    private void Connection_Disconnected()
    {
        UpdateServerConnectionStatus("disconnected - reconnecting");
    }

    private void Connection_ReconnectFailed(Exception _)
    {
        UpdateServerConnectionStatus("waiting for server");
    }

    private async Task SendResumeStatusAsync()
    {
        try
        {
            var statusPacket = PacketFactory.CreateStatus(
                source: _machineId,
                target: NetworkProtocol.ServerSource,
                payload: new StatusPayload
                {
                    MachineId = _machineId,
                    SessionId = _sessionId,
                    MachineName = _machineId,
                    Status = "Online",
                    LastSeen = DateTime.UtcNow
                },
                requestId: Guid.NewGuid().ToString("N"));

            await _connection.SendAsync(JsonHelper.SerializeToJson(statusPacket));
        }
        catch (Exception)
        {
            UpdateServerConnectionStatus("connected - status pending");
        }
    }

    private void UpdateServerConnectionStatus(string status)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateServerConnectionStatus(status));
            return;
        }

        if (!IsDisposed)
        {
            _serverTextBox.Text = $"{_serverEndpoint} ({status})";
        }
    }

    private static string ShortSessionId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return "N/A";
        }

        return sessionId.Length <= 12 ? sessionId : sessionId[..12];
    }

    private static string FormatLoginTime(DateTime loginTimeUtc)
    {
        return loginTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _connection.Connected -= Connection_Connected;
        _connection.Disconnected -= Connection_Disconnected;
        _connection.ReconnectFailed -= Connection_ReconnectFailed;
        _sessionTimer.Stop();
        _sessionTimer.Dispose();
        base.OnFormClosed(e);
    }
}
