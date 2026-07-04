using ClientApp.Networking;
using Shared.DTOs.Bidrectional;
using Shared.DTOs.CommandPayloads;
using Shared.DTOs.RequestPayloads;
using Shared.Enums;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;

namespace ClientApp.Forms;

public sealed class ClientMainForm : Form
{
    // Kết nối TCP được chuyển từ ConnectForm sang và được giữ trong suốt phiên sử dụng.
    private readonly TcpClientConnection _connection;

    // Thời điểm đăng nhập luôn được chuẩn hóa về UTC để tính thời lượng không phụ
    // thuộc múi giờ của máy trạm.
    private readonly DateTime _loginTimeUtc;
    private readonly string _machineId;
    private readonly string _sessionId;

    // Chuỗi host:port dùng để hiển thị cùng trạng thái kết nối hiện tại.
    private readonly string _serverEndpoint;

    // Timer WinForms chạy trên UI thread mỗi giây để cập nhật thời gian sử dụng.
    private readonly System.Windows.Forms.Timer _sessionTimer = new();

    // Handler lắng nghe packet điều khiển runtime và chuyển chúng thành sự kiện LOCK/UNLOCK.
    private readonly ClientRuntimeCommandHandler _commandHandler;

    // Form khóa chỉ tồn tại khi server đã khóa máy; null khi máy đang mở.
    private LockScreenForm? _lockScreen;
    private bool _isLockedByServer;
    private bool _hasShownEmptyBalanceWarning;
    private DateTime? _lockStartedUtc;

    // Hai TextBox được tạo bằng code trong BuildInfoLayout nên được gán sau constructor.
    private TextBox _usedTimeTextBox = null!;
    private TextBox _serverTextBox = null!;
    private TextBox _billingModeTextBox = null!;
    private TextBox _billingTimeTextBox = null!;
    private TextBox _billingBalanceTextBox = null!;
    private TextBox _billingAmountTextBox = null!;
    private TextBox _chatHistoryTextBox = null!;
    private TextBox _chatMessageTextBox = null!;
    private Button _sendChatButton = null!;
    private Button _topUpButton = null!;

    // Nhận thông tin phiên đã xác thực từ ConnectForm, dựng giao diện và đăng ký toàn
    // bộ sự kiện kết nối/lệnh cần thiết cho thời gian chạy của máy trạm.
    public ClientMainForm(
        string username,
        string machineId,
        string host,
        int port,
        string sessionId,
        DateTime loginTimeUtc,
        TcpClientConnection connection)
    {
        // Fail-fast nếu ConnectForm không chuyển kết nối hợp lệ; các giá trị định danh
        // được trim trước khi dùng làm source của packet.
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _loginTimeUtc = DateTime.SpecifyKind(loginTimeUtc, DateTimeKind.Utc);
        _machineId = machineId.Trim();
        _sessionId = sessionId.Trim();
        _serverEndpoint = $"{host}:{port}";

        // Cấu hình cửa sổ cố định để màn hình phiên làm việc có kích thước nhất quán.
        Text = $"Client - {_machineId}";
        ClientSize = new Size(600, 740);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Padding = new Padding(16);

        // Layout gốc chia form thành tiêu đề máy, thông tin phiên và dải nút thao tác.
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 354F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

        // Các layout con được tạo bằng helper để constructor chỉ điều phối cấu trúc tổng thể.
        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = MachineCaption(_machineId),
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 0);
        root.Controls.Add(BuildInfoLayout(username, sessionId, host, port), 0, 1);
        root.Controls.Add(BuildChatLayout(), 0, 2);
        root.Controls.Add(BuildActionStrip(), 0, 3);
        Controls.Add(root);

        // Tooltip lưu lại thông tin đăng nhập đầy đủ mà không chiếm diện tích form.
        ToolTip tooltip = new();
        tooltip.SetToolTip(this, $"Đã đăng nhập {username}/{machineId} tại {host}:{port}.");

        // Cập nhật thời gian ngay khi mở form, sau đó tăng mỗi giây bằng Timer UI.
        _sessionTimer.Interval = 1000;
        _sessionTimer.Tick += (_, _) => UpdateUsedTime();
        _sessionTimer.Start();
        UpdateUsedTime();

        // Đăng ký nhận lệnh runtime. Mỗi sự kiện được hủy đăng ký trong OnFormClosed.
        _commandHandler = new ClientRuntimeCommandHandler(_connection, _machineId);
        _commandHandler.LockRequested += ApplyLockCommand;
        _commandHandler.UnlockRequested += ApplyUnlockCommand;
        _commandHandler.ShutdownRequested += ApplyShutdownCommand;
        _commandHandler.ChatReceived += ApplyChatPacket;
        _commandHandler.NotificationReceived += ApplyNotificationPacket;
        _commandHandler.TimerReceived += ApplyTimerPacket;
        _commandHandler.InvalidPacketIgnored += CommandHandler_InvalidPacketIgnored;

        // Theo dõi vòng đời kết nối để cập nhật giao diện và gửi lại trạng thái sau reconnect.
        _connection.Connected += Connection_Connected;
        _connection.Disconnected += Connection_Disconnected;
        _connection.ReconnectFailed += Connection_ReconnectFailed;
        UpdateServerConnectionStatus(_connection.IsConnected
            ? "connected"
            : "disconnected - reconnecting");

        // Nếu socket vẫn nối từ bước đăng nhập, báo ngay trạng thái phiên đang hoạt động.
        if (_connection.IsConnected)
        {
            _ = SendResumeStatusAsync();
        }
    }

    // Giu MachineId that trong tieu de de hai instance local/LAN khong bi nham voi nhau.
    private static string MachineCaption(string machineId)
    {
        string normalizedMachineId = string.IsNullOrWhiteSpace(machineId)
            ? "UNKNOWN"
            : machineId.Trim();

        return $"Client {normalizedMachineId}";
    }

    // Tạo bảng hai cột chứa thông tin tài khoản/phiên và giữ tham chiếu tới các ô
    // cần cập nhật động là thời gian sử dụng và trạng thái máy chủ.
    private Control BuildInfoLayout(string username, string sessionId, string host, int port)
    {
        var infoLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            Margin = new Padding(0, 8, 0, 8)
        };
        infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F));
        infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Cac dong chia deu chieu cao cua vung thong tin.
        for (var row = 0; row < 10; row++)
        {
            infoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
        }

        AddInfoRow(infoLayout, 0, "Machine ID", _machineId);
        AddInfoRow(infoLayout, 1, "Account", username);
        AddInfoRow(infoLayout, 2, "Session", ShortSessionId(sessionId));
        _usedTimeTextBox = AddInfoRow(infoLayout, 3, "Used time", "00:00:00");
        AddInfoRow(infoLayout, 4, "Server", $"{host}:{port}");
        AddInfoRow(infoLayout, 5, "Login time", FormatLoginTime(_loginTimeUtc));
        _billingModeTextBox = AddInfoRow(infoLayout, 6, "Billing", "No active billing");
        _billingTimeTextBox = AddInfoRow(infoLayout, 7, "Timer", "N/A");
        _billingBalanceTextBox = AddInfoRow(infoLayout, 8, "Balance", "N/A");
        _billingAmountTextBox = AddInfoRow(infoLayout, 9, "Used cost", "0 VND");
        _serverTextBox = infoLayout.GetControlFromPosition(1, 4) as TextBox ?? _serverTextBox;
        return infoLayout;
    }

    // Thêm một cặp Label/TextBox chỉ đọc vào đúng hàng và trả về TextBox vừa tạo.
    private static TextBox AddInfoRow(TableLayoutPanel layout, int row, string label, string value)
    {
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = label,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        // TextBox chỉ dùng hiển thị, không nhận Tab focus và không cho người dùng sửa.
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

    // Tạo dải nút phía dưới. RightToLeft khiến nút đầu tiên được thêm nằm sát mép phải.
    private Control BuildActionStrip()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0)
        };

        _topUpButton = BuildButton("Nạp tiền", TopUp_Click);
        panel.Controls.Add(_topUpButton);
        panel.Controls.Add(BuildButton("Đăng xuất", Logout_Click));
        return panel;
    }

    // Factory dùng chung để ba nút có cùng kích thước, margin và cách gắn handler.
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

    private void TopUp_Click(object? sender, EventArgs e)
    {
        ShowTopUpRequestDialog();
    }

    // Đăng xuất bằng cách đóng form chính; OnFormClosed sẽ giải phóng kết nối và handler.
    private void Logout_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private Control BuildChatLayout()
    {
        var chatLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0, 4, 0, 4)
        };
        chatLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        chatLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        chatLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

        chatLayout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Chat voi Server",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        _chatHistoryTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            TabStop = false,
            Text = "Chua co tin nhan."
        };
        chatLayout.Controls.Add(_chatHistoryTextBox, 0, 1);

        var inputLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));

        _chatMessageTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 7, 8, 0)
        };
        _chatMessageTextBox.KeyDown += ChatMessageTextBox_KeyDown;
        inputLayout.Controls.Add(_chatMessageTextBox, 0, 0);

        _sendChatButton = new Button
        {
            Dock = DockStyle.Fill,
            Text = "Gui",
            Margin = new Padding(0, 4, 0, 0),
            UseVisualStyleBackColor = true
        };
        _sendChatButton.Click += SendChatButton_Click;
        inputLayout.Controls.Add(_sendChatButton, 1, 0);

        chatLayout.Controls.Add(inputLayout, 0, 2);
        return chatLayout;
    }

    private void ChatMessageTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            _ = SendClientChatAsync();
        }
    }

    private void SendChatButton_Click(object? sender, EventArgs e)
    {
        _ = SendClientChatAsync();
    }

    private async Task SendClientChatAsync()
    {
        string message = _chatMessageTextBox.Text.Trim();
        if (message.Length == 0)
        {
            return;
        }

        _sendChatButton.Enabled = false;
        try
        {
            await SendClientChatMessageAsync(message);
            AppendClientChatLine(_machineId, message);
            _chatMessageTextBox.Clear();
        }
        catch (Exception)
        {
            UpdateServerConnectionStatus("connected - chat pending");
        }
        finally
        {
            if (!IsDisposed)
            {
                _sendChatButton.Enabled = true;
                _chatMessageTextBox.Focus();
            }
        }
    }

    private async Task SendClientChatMessageAsync(string message)
    {
        var chatPacket = PacketFactory.CreateChat(
            source: _machineId,
            target: NetworkProtocol.ServerSource,
            payload: new ChatPayload
            {
                Sender = _machineId,
                Receiver = NetworkProtocol.ServerSource,
                Message = message
            },
            requestId: Guid.NewGuid().ToString("N"));

        await _connection.SendAsync(JsonHelper.SerializeToJson(chatPacket));
    }

    // Tính thời gian đã dùng từ mốc UTC đăng nhập và hiển thị theo HH:mm:ss. TotalHours
    // được ép int để số giờ vẫn tăng qua 24 thay vì quay lại 00 như TimeSpan.Hours.
    private void UpdateUsedTime()
    {
        TimeSpan elapsed = DateTime.UtcNow - _loginTimeUtc;

        // Bảo vệ trường hợp đồng hồ máy bị lệch khiến loginTime nằm trong tương lai.
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        _usedTimeTextBox.Text = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

    // Khi kết nối hoặc reconnect thành công, cập nhật UI rồi gửi lại trạng thái Online
    // để server phục hồi thông tin phiên của client.
    private async void Connection_Connected()
    {
        UpdateServerConnectionStatus("connected");
        await SendResumeStatusAsync();
    }

    // Mất kết nối chưa đóng phiên vì TcpClientConnection sẽ tự thử reconnect.
    private void Connection_Disconnected()
    {
        UpdateServerConnectionStatus("disconnected - reconnecting");
    }

    // Một lượt reconnect thất bại được phản ánh trên UI; exception không hiển thị trực tiếp.
    private void Connection_ReconnectFailed(Exception _)
    {
        UpdateServerConnectionStatus("waiting for server");
    }

    // Packet điều khiển sai được bỏ qua an toàn và chỉ cập nhật trạng thái chẩn đoán.
    private void CommandHandler_InvalidPacketIgnored()
    {
        UpdateServerConnectionStatus("connected - ignored invalid packet");
    }

    private void ApplyChatPacket(Packet<ChatPayload> packet)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyChatPacket(packet));
            return;
        }

        if (IsDisposed)
        {
            return;
        }

        ChatPayload payload = packet.TypedPayload;
        AppendClientChatLine(
            string.IsNullOrWhiteSpace(payload.Sender) ? "Server" : payload.Sender.Trim(),
            payload.Message.Trim());
        UpdateServerConnectionStatus("connected - chat received");
    }

    private void ApplyNotificationPacket(Packet<NotificationPayload> packet)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyNotificationPacket(packet));
            return;
        }

        if (IsDisposed)
        {
            return;
        }

        NotificationPayload payload = packet.TypedPayload;
        string severity = string.IsNullOrWhiteSpace(payload.Severity) ? "Info" : payload.Severity.Trim();
        AppendClientChatLine($"Thong bao {severity}", payload.Message.Trim());
        UpdateServerConnectionStatus("connected - notification received");
    }

    private void ApplyTimerPacket(Packet<TimerPayload> packet)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyTimerPacket(packet));
            return;
        }

        if (IsDisposed)
        {
            return;
        }

        TimerPayload payload = packet.TypedPayload;
        string status = string.IsNullOrWhiteSpace(payload.Status) ? "Active" : payload.Status.Trim();
        string mode = string.IsNullOrWhiteSpace(payload.RentalMode) ? "Billing" : payload.RentalMode.Trim();
        string warning = payload.IsWarning ? " - warn <=5m" : string.Empty;
        _billingModeTextBox.Text = $"{status} {mode}{warning} / {payload.RatePerHour:N0} VND/hour";

        string elapsed = FormatDuration(payload.ElapsedSeconds);
        long countdownSeconds = payload.RemainingUsageSeconds
            ?? payload.RemainingSeconds
            ?? 0;
        _billingTimeTextBox.Text = $"Da dung: {elapsed} / Còn lại: {FormatDuration(countdownSeconds)}";

        _billingBalanceTextBox.Text = payload.RemainingBalanceVnd is null
            ? "N/A"
            : $"Da nap: {payload.TotalBalanceVnd ?? 0:N0} VND / Con lai: {payload.RemainingBalanceVnd.Value:N0} VND / Thoi gian con lai: {FormatDuration(payload.RemainingUsageSeconds ?? 0)}";
        _billingAmountTextBox.Text = $"Da dung: {payload.AmountVnd:N0} VND ({payload.ChargedMinutes} min)";

        if (payload.RemainingBalanceVnd is > 0)
        {
            _hasShownEmptyBalanceWarning = false;
        }
        else if (payload.ShouldLockNow
            && payload.RemainingBalanceVnd == 0
            && !_hasShownEmptyBalanceWarning)
        {
            _hasShownEmptyBalanceWarning = true;
            ShowTopUpRequestDialog();
        }

        UpdateServerConnectionStatus(payload.ShouldLockNow
            ? "connected - billing expired"
            : "connected - billing synced");
    }

    private static string FormatDuration(long totalSeconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        return $"{(long)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }

    private void ShowTopUpRequestDialog()
    {
        using var dialog = new Form
        {
            Text = "Cần nạp tiền",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(440, 218),
            Padding = new Padding(14)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "!",
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = Color.FromArgb(210, 145, 0),
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Tài khoản đã hết tiền. Vui lòng gửi yêu cầu nạp thêm tiền để tiếp tục sử dụng máy.",
            TextAlign = ContentAlignment.MiddleLeft
        }, 1, 0);

        var amountInput = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 1_000,
            Maximum = 1_000_000_000,
            Increment = 1_000,
            ThousandsSeparator = true,
            Value = 10_000,
            Margin = new Padding(0, 2, 0, 4)
        };
        var amountLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Số tiền cần nạp (VND)",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 0, 0)
        };
        layout.Controls.Add(amountLabel, 0, 1);
        layout.SetColumnSpan(amountLabel, 2);
        layout.Controls.Add(amountInput, 0, 2);
        layout.SetColumnSpan(amountInput, 2);

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var requestButton = new Button
        {
            Text = "Yêu cầu nạp tiền",
            DialogResult = DialogResult.OK,
            Width = 132
        };
        buttonRow.Controls.Add(requestButton);
        layout.Controls.Add(buttonRow, 0, 3);
        layout.SetColumnSpan(buttonRow, 2);

        dialog.AcceptButton = requestButton;
        dialog.Controls.Add(layout);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _ = SendTopUpRequestAsync(decimal.ToInt64(amountInput.Value));
        }
    }

    private async Task SendTopUpRequestAsync(long requestedAmount)
    {
        if (requestedAmount <= 0)
        {
            UpdateServerConnectionStatus("connected - invalid top-up amount");
            return;
        }

        string message = $"{_machineId} yêu cầu nạp {requestedAmount} VND";

        try
        {
            await SendClientChatMessageAsync(message);
            AppendClientChatLine(_machineId, message);
            UpdateServerConnectionStatus("connected - top-up request sent");
        }
        catch (Exception)
        {
            UpdateServerConnectionStatus("connected - top-up request pending");
        }
    }

    private void AppendClientChatLine(string sender, string message)
    {
        if (_chatHistoryTextBox.Text == "Chua co tin nhan.")
        {
            _chatHistoryTextBox.Clear();
        }

        string line = $"[{DateTime.Now:HH:mm:ss}] {sender}: {message}";
        _chatHistoryTextBox.AppendText(_chatHistoryTextBox.TextLength == 0
            ? line
            : Environment.NewLine + line);
        _chatHistoryTextBox.SelectionStart = _chatHistoryTextBox.TextLength;
        _chatHistoryTextBox.ScrollToCaret();
    }

    // Áp dụng lệnh LOCK từ server, hiển thị LockScreenForm và gửi ACK tương ứng với
    // RequestId của packet để server đối chiếu kết quả.
    private void ApplyLockCommand(Packet<LockPayload> packet)
    {
        // Sự kiện packet có thể phát từ luồng đọc socket; mọi thao tác control phải
        // được chuyển về UI thread.
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyLockCommand(packet));
            return;
        }

        // Bỏ qua packet đến muộn sau khi form đã kết thúc vòng đời.
        if (IsDisposed)
        {
            return;
        }

        // LOCK lặp không tạo thêm form. Kích hoạt cửa sổ hiện có và vẫn ACK thành công
        // để server biết trạng thái khóa đã được bảo đảm.
        if (_lockScreen is { IsDisposed: false })
        {
            SetClientSurfaceLocked(true);
            _lockScreen.Activate();
            UpdateServerConnectionStatus("connected - locked by server");
            _ = SendCommandAckAsync(packet.Type, packet.RequestId, "Success", "Lock already applied.");
            return;
        }

        // Lệnh LOCK đầu tiên khóa các control form chính rồi mở form chặn thao tác.
        SetClientSurfaceLocked(true);
        _lockScreen = new LockScreenForm();
        _lockScreen.FormClosed += LockScreen_FormClosed;
        _lockScreen.Show(this);
        UpdateServerConnectionStatus("connected - locked by server");
        _ = SendCommandAckAsync(packet.Type, packet.RequestId, "Success", "Lock applied.");
    }

    // Áp dụng lệnh UNLOCK, đóng màn hình khóa bằng đường được server cho phép và gửi ACK.
    private void ApplyUnlockCommand(Packet<UnlockPayload> packet)
    {
        // Đồng bộ về UI thread vì handler mạng không chạy trên message loop của form.
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyUnlockCommand(packet));
            return;
        }

        // Không truy cập control nếu form chính đã bị Dispose.
        if (IsDisposed)
        {
            return;
        }

        // Nếu màn hình khóa còn sống, UnlockFromServer đặt cờ cho phép đóng. Nếu không,
        // chỉ dọn tham chiếu để trạng thái nội bộ vẫn nhất quán.
        if (_lockScreen is { IsDisposed: false })
        {
            _lockScreen.UnlockFromServer();
        }
        else
        {
            _lockScreen = null;
        }

        // Mở lại bề mặt client kể cả khi form khóa không tồn tại, giúp UNLOCK có tính idempotent.
        SetClientSurfaceLocked(false);
        UpdateServerConnectionStatus("connected - unlocked by server");
        _ = SendCommandAckAsync(packet.Type, packet.RequestId, "Success", "Unlock applied.");
    }

    private async void ApplyShutdownCommand(Packet<ShutdownPayload> packet)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyShutdownCommand(packet));
            return;
        }

        if (IsDisposed)
        {
            return;
        }

        UpdateServerConnectionStatus("connected - shutdown requested");
        await SendCommandAckAsync(packet.Type, packet.RequestId, "Success", "Shutdown accepted.");
        if (!IsDisposed)
        {
            Close();
        }
    }

    // Dọn tham chiếu khi màn hình khóa đóng và hủy handler để tránh giữ object cũ.
    private void LockScreen_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is LockScreenForm lockScreen)
        {
            lockScreen.FormClosed -= LockScreen_FormClosed;
        }

        // Form khóa đã biến mất nên luôn khôi phục khả năng tương tác của form chính.
        _lockScreen = null;
        SetClientSurfaceLocked(false);
    }

    // Ghi nhận trạng thái khóa và bật/tắt toàn bộ control cấp một của form chính.
    private void SetClientSurfaceLocked(bool locked)
    {
        DateTime now = DateTime.UtcNow;
        if (locked && !_isLockedByServer)
        {
            _lockStartedUtc = now;
        }
        else if (!locked && _isLockedByServer)
        {
            _lockStartedUtc = null;
        }

        _isLockedByServer = locked;
        UpdateUsedTime();

        // Vô hiệu hóa container gốc sẽ làm toàn bộ control con không nhận thao tác.
        foreach (Control control in Controls)
        {
            control.Enabled = !locked;
        }
    }

    // Gửi packet STATUS Online sau đăng nhập/reconnect để server gắn lại máy với phiên hiện tại.
    private async Task SendResumeStatusAsync()
    {
        try
        {
            // RequestId mới xác định riêng lần cập nhật; LastSeen dùng UTC để đồng bộ máy chủ.
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

            // Packet typed được serialize thành một dòng JSON trước khi chuyển qua TCP.
            await _connection.SendAsync(JsonHelper.SerializeToJson(statusPacket));
        }
        catch (Exception)
        {
            // Không đóng form khi status tạm thời chưa gửi được; auto reconnect có thể
            // kích hoạt lần gửi lại sau đó.
            UpdateServerConnectionStatus("connected - status pending");
        }
    }

    // Gửi ACK cho lệnh LOCK/UNLOCK, giữ nguyên RequestId của lệnh gốc để server ghép cặp.
    private async Task SendCommandAckAsync(PacketType commandType, string? requestId, string status, string message)
    {
        try
        {
            // AckFor mô tả loại lệnh, còn MachineId xác định client đã thực thi lệnh.
            var ackPacket = PacketFactory.CreateAck(
                source: _machineId,
                target: NetworkProtocol.ServerSource,
                payload: new AckPayload
                {
                    MachineId = _machineId,
                    AckFor = commandType.ToString(),
                    Status = status,
                    Message = message
                },
                requestId: requestId);

            await _connection.SendAsync(JsonHelper.SerializeToJson(ackPacket));
        }
        catch (Exception)
        {
            // ACK lỗi chỉ làm thay đổi trạng thái hiển thị; lệnh đã áp dụng cục bộ không
            // bị hoàn tác vì mất phản hồi mạng.
            UpdateServerConnectionStatus("connected - command ACK pending");
        }
    }

    // Hiển thị endpoint cùng trạng thái kết nối; helper tự marshal về UI thread để
    // mọi callback networking có thể gọi trực tiếp.
    private void UpdateServerConnectionStatus(string status)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateServerConnectionStatus(status));
            return;
        }

        // Callback có thể được xếp hàng trước lúc đóng form, nên kiểm tra lại Dispose.
        if (!IsDisposed)
        {
            _serverTextBox.Text = $"{_serverEndpoint} ({status})";
        }
    }

    // Rút gọn SessionId còn tối đa 12 ký tự cho vừa giao diện; ID đầy đủ vẫn được giữ
    // trong field _sessionId để gửi packet.
    private static string ShortSessionId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return "N/A";
        }

        return sessionId.Length <= 12 ? sessionId : sessionId[..12];
    }

    // Chuyển mốc UTC sang giờ địa phương chỉ tại bước hiển thị cho người dùng.
    private static string FormatLoginTime(DateTime loginTimeUtc)
    {
        return loginTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }

    // Giải phóng toàn bộ subscription, handler, form khóa và timer khi phiên kết thúc.
    // Thứ tự dọn dẹp ngăn callback mạng tiếp tục truy cập control đã Dispose.
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // Ngừng nhận lệnh trước rồi Dispose command handler.
        _commandHandler.LockRequested -= ApplyLockCommand;
        _commandHandler.UnlockRequested -= ApplyUnlockCommand;
        _commandHandler.ShutdownRequested -= ApplyShutdownCommand;
        _commandHandler.ChatReceived -= ApplyChatPacket;
        _commandHandler.NotificationReceived -= ApplyNotificationPacket;
        _commandHandler.TimerReceived -= ApplyTimerPacket;
        _commandHandler.InvalidPacketIgnored -= CommandHandler_InvalidPacketIgnored;
        _commandHandler.Dispose();

        // Hủy các callback trạng thái của kết nối; TcpClientConnection do ConnectForm
        // sở hữu sẽ được Dispose khi form đăng nhập đóng.
        _connection.Connected -= Connection_Connected;
        _connection.Disconnected -= Connection_Disconnected;
        _connection.ReconnectFailed -= Connection_ReconnectFailed;

        // Cho phép đóng màn hình khóa theo đường server để handler FormClosing không chặn.
        if (_lockScreen is { IsDisposed: false })
        {
            _lockScreen.FormClosed -= LockScreen_FormClosed;
            _lockScreen.UnlockFromServer();
            _lockScreen = null;
        }

        // Khôi phục control trước khi form chính hoàn tất đóng để trạng thái không bị kẹt.
        if (_isLockedByServer)
        {
            SetClientSurfaceLocked(false);
        }

        // Timer phải được dừng và Dispose để không còn Tick sau khi form đóng.
        _sessionTimer.Stop();
        _sessionTimer.Dispose();
        base.OnFormClosed(e);
    }
}
