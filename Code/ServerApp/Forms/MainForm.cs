using ServerApp.Database.Contracts;
using ServerApp.Database.Entities;
using ServerApp.Database.Models;
using ServerApp.Networking;
using ServerApp.Presentation;
using Shared.Enums;
using System.Media;

namespace ServerApp;

public partial class MainForm : Form
{
    // Tiền tố chỉ dùng khi tạo danh sách máy mẫu; dữ liệu thật giữ nguyên MachineId
    // do repository hoặc client gửi lên.
    private const string SampleMachinePrefix = "PC";
    private const string ServerMachineId = "PC00";

    // Định danh nguồn phát lệnh để phía networking có thể ghi log/audit lệnh admin.
    private const string AdminCommandIssuer = "ServerApp.MainForm";
    private const string MachineCardLabelName = "lblMachineCardText";
    private const string UnreadBadgeLabelName = "lblUnreadChatBadge";

    // Repository có thể null khi chạy giao diện demo không kết nối cơ sở dữ liệu.
    private readonly IMachineRepository? _machines;
    private readonly ICustomerRepository? _customers;

    // Hai service luôn có giá trị. Nếu dependency không được truyền vào, constructor
    // dùng implementation "Unavailable" để trả lỗi có kiểm soát thay vì NullReference.
    private readonly IAdminCommandService _adminCommands;
    private readonly IAdminChatService _adminChat;
    private readonly IAdminNotificationService _adminNotification;
    private readonly IAdminBillingService? _adminBilling;
    private readonly System.Windows.Forms.Timer _billingRefreshTimer = new();
    private readonly ToolTip _incomingChatTip = new();

    // Lưu lịch sử chat riêng theo từng MachineId. So sánh không phân biệt hoa/thường
    // để PC01 và pc01 không tạo thành hai cuộc hội thoại khác nhau.
    private readonly Dictionary<string, List<AdminChatMessage>> _chatHistoryByMachine =
        new(StringComparer.OrdinalIgnoreCase);

    // Số tin nhắn client đã gửi nhưng admin chưa mở cuộc hội thoại của máy đó.
    private readonly Dictionary<string, int> _unreadChatCountByMachine =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, List<TopUpChatAction>> _topUpActionsByMachine =
        new(StringComparer.OrdinalIgnoreCase);

    // Cờ chống vòng lặp sự kiện: SelectMachine thay đổi CurrentRow, thao tác đó lại
    // phát SelectionChanged. Khi cờ bật, handler sẽ không chọn máy thêm lần nữa.
    private bool _isSelectingMachine;

    // Phân biệt màn hình dữ liệu mẫu với dữ liệu runtime. Chỉ runtime mới cho phép
    // gửi lệnh khóa/mở khóa tới client thật.
    private bool _isRuntimeMachineDataActive;

    // Máy đang được admin thao tác; đồng thời là đích nhận lệnh và tin nhắn chat.
    private string? _selectedMachineName;
    private NumericUpDown _billingRatePerHourInput = null!;
    private Button _btnSaveBillingRate = null!;
    private Label _billingRateStatusLabel = null!;
    private TextBox _txtBillingMonitor = null!;
    private TableLayoutPanel _billingTopUpRequestPanel = null!;
    private Label _billingTopUpRequestLabel = null!;
    private FlowLayoutPanel _billingTopUpButtonRow = null!;
    private Button _btnRejectBillingTopUp = null!;
    private Button _btnConfirmBillingTopUp = null!;
    private Label _billingTopUpStatusLabel = null!;
    private FlowLayoutPanel _chatHistoryPanel = null!;

    // Các constructor rút gọn đều chuyển về constructor đầy đủ để việc khởi tạo
    // dependency và đăng ký sự kiện chỉ nằm tại một nơi.
    public MainForm()
        : this(null, (IAdminCommandService?)null)
    {
    }

    public MainForm(IMachineRepository? machines)
        : this(machines, (IAdminCommandService?)null)
    {
    }

    public MainForm(IMachineRepository? machines, IAdminCommandService? adminCommands)
        : this(machines, adminCommands, null)
    {
    }

    public MainForm(
        IMachineRepository? machines,
        IAdminCommandService? adminCommands,
        IAdminChatService? adminChat)
        : this(machines, adminCommands, adminChat, null)
    {
    }

    public MainForm(
        IMachineRepository? machines,
        IAdminCommandService? adminCommands,
        IAdminChatService? adminChat,
        IAdminBillingService? adminBilling)
        : this(machines, adminCommands, adminChat, adminBilling, null, null)
    {
    }

    private MainForm(
        IMachineRepository? machines,
        IAdminCommandService? adminCommands,
        IAdminChatService? adminChat,
        IAdminBillingService? adminBilling,
        IAdminNotificationService? adminNotification,
        ICustomerRepository? customers)
    {
        // Lưu dependency và thay dependency thiếu bằng service trả lỗi có kiểm soát.
        _machines = machines;
        _customers = customers;
        _adminCommands = adminCommands ?? new UnavailableAdminCommandService();
        _adminChat = adminChat ?? new UnavailableAdminChatService();
        _adminNotification = adminNotification ?? new UnavailableAdminNotificationService();
        _adminBilling = adminBilling;

        // Dựng control trước khi cấu hình trạng thái ban đầu vì các hàm sau cần truy
        // cập label, button và textbox do Designer tạo.
        InitializeComponent();
        ConfigureResizableLayout();
        ConfigureInlineChatHistory();
        ConfigureBillingPanel();
        ConfigureR1ShellState();

        // Nhận tin nhắn đẩy từ client trong suốt vòng đời form. Sự kiện được hủy ở
        // OnFormClosed để service không giữ tham chiếu tới form đã đóng.
        _adminChat.MessageReceived += AdminChat_MessageReceived;
        if (_adminBilling is not null)
        {
            _adminBilling.BillingUpdated += AdminBilling_BillingUpdated;
            _billingRefreshTimer.Interval = 1000;
            _billingRefreshTimer.Tick += BillingRefreshTimer_Tick;
            _billingRefreshTimer.Start();
        }
    }

    /// <summary>
    /// Constructor tương thích với TcpJsonLineServer: bọc server thành service gửi
    /// lệnh để phần còn lại của form chỉ phụ thuộc vào IAdminCommandService.
    /// </summary>
    public MainForm(IMachineRepository? machines, TcpJsonLineServer? networkServer)
        : this(machines, networkServer, null)
    {
    }

    public MainForm(IMachineRepository? machines, TcpJsonLineServer? networkServer, IAdminBillingService? adminBilling)
        : this(machines, networkServer, adminBilling, null)
    {
    }

    public MainForm(
        IMachineRepository? machines,
        TcpJsonLineServer? networkServer,
        IAdminBillingService? adminBilling,
        ICustomerRepository? customers)
        : this(
            machines,
            networkServer is null
                ? null
                : new NetworkAdminCommandService(networkServer),
            networkServer is null
                ? null
                : new NetworkAdminChatService(networkServer),
            adminBilling,
            networkServer is null
                ? null
                : new NetworkAdminNotificationService(networkServer),
            customers)
    {
    }

    private void ConfigureResizableLayout()
    {
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        SizeGripStyle = SizeGripStyle.Show;
        MinimumSize = new Size(960, 640);
        ClientSize = new Size(Math.Max(ClientSize.Width, 1100), Math.Max(ClientSize.Height, 760));

        machineActions.AutoScroll = true;
        customerButtons.AutoScroll = true;

        machineLayout.RowStyles.Clear();
        machineLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        machineLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        machineLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        machineLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        machineSplit.FixedPanel = FixedPanel.None;
        machineSplit.IsSplitterFixed = false;
        machineSplit.Panel1MinSize = 320;
        machineSplit.Panel2MinSize = 320;
        if (machineSplit.Width > machineSplit.Panel1MinSize + machineSplit.Panel2MinSize + machineSplit.SplitterWidth)
        {
            machineSplit.SplitterDistance = Math.Min(
                Math.Max(560, machineSplit.Panel1MinSize),
                machineSplit.Width - machineSplit.Panel2MinSize - machineSplit.SplitterWidth);
        }

        customerLayout.RowStyles.Clear();
        customerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
        customerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        customerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
    }

    private void ConfigureInlineChatHistory()
    {
        txtChatHistory.Clear();
        txtChatHistory.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        txtChatHistory.BackColor = Color.White;
        txtChatHistory.WordWrap = true;
        txtChatHistory.ScrollBars = ScrollBars.Vertical;
    }

    private void ConfigureBillingPanel()
    {
        var rightTabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Alignment = TabAlignment.Top
        };

        var notificationTab = new TabPage
        {
            Text = "Thông báo",
            Padding = new Padding(8)
        };

        var chatTab = new TabPage
        {
            Text = "Chat",
            Padding = new Padding(8)
        };

        var settingTab = new TabPage
        {
            Text = "Setting",
            Padding = new Padding(8)
        };

        var notificationGroup = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "Thông báo nạp tiền từ client",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Padding = new Padding(12, 8, 12, 12)
        };

        var notificationLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        notificationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        notificationLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _txtBillingMonitor = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            TabStop = false,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9.25F, FontStyle.Regular),
            Margin = new Padding(0, 2, 0, 0),
            Text = "Bạn sẽ nhận thông báo nạp tiền từ client tại đây."
        };

        _billingTopUpRequestPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(255, 252, 232),
            ColumnCount = 1,
            Margin = new Padding(0, 6, 0, 0),
            Padding = new Padding(10, 8, 10, 8),
            RowCount = 3,
            Visible = true
        };
        _billingTopUpRequestPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _billingTopUpRequestPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _billingTopUpRequestPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _billingTopUpRequestPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _billingTopUpRequestPanel.Resize += (_, _) => UpdateBillingTopUpRequestLabelWidth();

        _billingTopUpRequestLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 6)
        };

        _billingTopUpButtonRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            WrapContents = false
        };

        _btnRejectBillingTopUp = new Button
        {
            Text = "Từ chối",
            Width = 92,
            Margin = new Padding(0, 0, 8, 0),
            UseVisualStyleBackColor = true
        };
        _btnRejectBillingTopUp.Click += TopUpRejectButton_Click;

        _btnConfirmBillingTopUp = new Button
        {
            Text = "Xác nhận",
            Width = 92,
            Margin = new Padding(0, 0, 0, 0),
            UseVisualStyleBackColor = true
        };
        _btnConfirmBillingTopUp.Click += TopUpConfirmButton_Click;

        _billingTopUpStatusLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Italic),
            ForeColor = Color.FromArgb(60, 90, 60),
            Margin = new Padding(0, 4, 0, 0),
            Visible = false
        };

        _billingTopUpButtonRow.Controls.Add(_btnRejectBillingTopUp);
        _billingTopUpButtonRow.Controls.Add(_btnConfirmBillingTopUp);
        _billingTopUpRequestPanel.Controls.Add(_billingTopUpRequestLabel, 0, 0);
        _billingTopUpRequestPanel.Controls.Add(_billingTopUpButtonRow, 0, 1);
        _billingTopUpRequestPanel.Controls.Add(_billingTopUpStatusLabel, 0, 2);

        notificationLayout.Controls.Add(_txtBillingMonitor, 0, 0);
        notificationLayout.Controls.Add(_billingTopUpRequestPanel, 0, 1);
        notificationGroup.Controls.Add(notificationLayout);

        var settingGroup = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "Cài đặt giá tiền",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Padding = new Padding(12, 8, 12, 12)
        };

        var settingLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(0, 8, 0, 0)
        };
        settingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126F));
        settingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        settingLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        settingLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        settingLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _billingRatePerHourInput = new NumericUpDown
        {
            Minimum = 1_000,
            Maximum = 1_000_000_000,
            Increment = 1_000,
            ThousandsSeparator = true,
            Value = 10_000,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            Margin = new Padding(0, 3, 0, 3)
        };

        _btnSaveBillingRate = new Button
        {
            Text = "Cập nhật",
            Width = 104,
            Height = 30,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 4),
            UseVisualStyleBackColor = true
        };
        _btnSaveBillingRate.Click += BillingRateSave_Click;

        _billingRateStatusLabel = new Label
        {
            AutoSize = true,
            Text = $"Giá hiện tại: {FormatMoney(GetBillingRatePerHour())}/giờ. Áp dụng cho phiên client mới.",
            ForeColor = Color.FromArgb(55, 90, 55),
            Margin = new Padding(0, 4, 0, 0)
        };

        settingLayout.Controls.Add(BuildBillingFieldLabel("Giá/giờ"), 0, 0);
        settingLayout.Controls.Add(_billingRatePerHourInput, 1, 0);
        settingLayout.Controls.Add(_btnSaveBillingRate, 1, 1);
        settingLayout.Controls.Add(_billingRateStatusLabel, 0, 2);
        settingLayout.SetColumnSpan(_billingRateStatusLabel, 2);
        settingGroup.Controls.Add(settingLayout);

        machineSplit.Panel2.Controls.Remove(chatGroup);
        chatGroup.Text = string.Empty;
        notificationTab.Controls.Add(notificationGroup);
        chatTab.Controls.Add(chatGroup);
        settingTab.Controls.Add(settingGroup);
        rightTabs.TabPages.Add(notificationTab);
        rightTabs.TabPages.Add(chatTab);
        rightTabs.TabPages.Add(settingTab);
        machineSplit.Panel2.Controls.Add(rightTabs);
        RenderSelectedBillingTopUpRequest();
        SetBillingActionEnabled(false);
    }

    private static Label BuildBillingFieldLabel(string text)
        => new()
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 45, 45),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 8, 0)
        };

    /// <summary>
    /// Nạp dữ liệu khi form mở. Danh sách khách hàng đọc trực tiếp từ database;
    /// danh sách máy ưu tiên repository thật và chỉ quay về dữ liệu mẫu khi không có dữ liệu.
    /// </summary>
    private async void MainForm_Load(object sender, EventArgs e)
    {
        await LoadCustomerDataAsync();

        // TryLoad trả true khi đã có ít nhất một máy runtime và tự chọn máy đầu tiên.
        if (!await TryLoadRuntimeMachineDataAsync())
        {
            // Chế độ fallback giúp UI vẫn minh họa được bố cục khi database chưa sẵn
            // sàng, nhưng không được xem là danh sách client thật.
            LoadSampleMachineData();
            SelectMachine("PC01");
            lblServerStatus.Text = UiStrings.MainServerStatus;
        }
    }

    /// <summary>
    /// Thiết lập trạng thái giao diện ban đầu trước khi biết có dữ liệu runtime hay
    /// không: tiêu đề mẫu, nhãn nút và khu vực chat chưa có máy đích.
    /// </summary>
    private void ConfigureR1ShellState()
    {
        lblMachineTitle.Text = UiStrings.MainMachineTitleSample;
        lblServerStatus.Text = UiStrings.MainServerStatus;

        btnLockMachine.Text = UiStrings.MainLockMachine;
        btnLockMachine.Width = 120;
        btnLockMachine.Enabled = true;

        btnUnlockMachine.Text = UiStrings.MainUnlockMachine;
        btnUnlockMachine.Width = 120;
        btnUnlockMachine.Enabled = true;

        SetChatActionEnabled(false);
    }

    /// <summary>
    /// Tạo 10 máy giả cho chế độ trình diễn. Mỗi máy được thể hiện đồng thời bằng
    /// một dòng trong bảng và một card trong FlowLayoutPanel.
    /// </summary>
    private void LoadSampleMachineData()
    {
        // Đánh dấu rõ đây không phải dữ liệu kết nối thật và xóa nội dung cũ để tránh
        // nhân đôi nếu hàm được gọi lại.
        _isRuntimeMachineDataActive = false;
        dgvMachines.Rows.Clear();
        pnlMachineCards.Controls.Clear();

        // Mảng trạng thái và vị trí index quyết định trạng thái của PC01..PC10.
        string[] statuses =
        [
            "AVAILABLE",
            "DISCONNECT",
            "ONLINE",
            "ONLINE",
            "ONLINE",
            "AVAILABLE",
            "DISCONNECT",
            "DISCONNECT",
            "AVAILABLE",
            "AVAILABLE"
        ];

        for (int index = 0; index < statuses.Length; index++)
        {
            int machineNumber = index + 1;
            string machineName = $"{SampleMachinePrefix}{machineNumber:00}";

            // Cập nhật hai cách hiển thị từ cùng một nguồn để bảng và card nhất quán.
            dgvMachines.Rows.Add(machineNumber, machineNumber, statuses[index], FormatMoney(0), machineName);
            pnlMachineCards.Controls.Add(CreateMachineCard(machineName, statuses[index]));
        }
    }

    /// <summary>
    /// Thử lấy danh sách máy thật từ repository. Trả false khi không có repository,
    /// danh sách rỗng hoặc phát sinh lỗi để caller chủ động dùng dữ liệu mẫu.
    /// </summary>
    private async Task<bool> TryLoadRuntimeMachineDataAsync()
    {
        if (_machines is null)
        {
            return false;
        }

        try
        {
            // Repository chịu trách nhiệm truy cập database; UI chỉ nhận danh sách
            // entity đã được materialize.
            IReadOnlyList<MachineEntity> machines = await _machines.ListAsync();
            if (machines.Count == 0)
            {
                return false;
            }

            LoadRuntimeMachineData(machines);
            return true;
        }
        catch (Exception ex)
        {
            lblServerStatus.Text = $"Không thể tải dữ liệu máy thật: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Đưa toàn bộ entity từ repository lên bảng và card, sau đó chọn máy đầu tiên
    /// làm ngữ cảnh thao tác mặc định.
    /// </summary>
    private void LoadRuntimeMachineData(IReadOnlyList<MachineEntity> machines)
    {
        // Lần chuyển đầu tiên sẽ xóa dữ liệu mẫu và bật các thao tác runtime.
        EnsureRuntimeMachineDataActive();

        foreach (MachineEntity machine in machines)
        {
            // Chuẩn hóa tại biên UI để MachineId rỗng và status khác kiểu chữ không
            // làm hỏng việc tìm/cập nhật control.
            string machineName = NormalizeMachineId(machine.MachineId);
            string status = NormalizeStatus(machine.Status);

            // Upsert cho phép cùng code xử lý cả lần tải đầu và lần cập nhật sau.
            UpsertMachineRow(machineName, status);
            UpsertMachineCard(machineName, status);
        }

        string firstMachineName = NormalizeMachineId(machines[0].MachineId);
        SelectMachine(firstMachineName);
        lblServerStatus.Text = string.Format(UiStrings.MainSelectedMachineStatusTemplate, firstMachineName);
    }

    /// <summary>
    /// Nhận trạng thái máy theo thời gian thực từ tầng networking và đồng bộ cả bảng
    /// lẫn card. Hàm public để thành phần tiếp nhận sự kiện mạng gọi vào form.
    /// </summary>
    public void ApplyMachineStatusUpdate(string machineId, string status)
    {
        // Sự kiện mạng có thể chạy trên worker thread. Mọi thay đổi WinForms control
        // phải được chuyển về UI thread thông qua BeginInvoke.
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyMachineStatusUpdate(machineId, status));
            return;
        }

        // Chuẩn hóa trước khi so sánh hoặc hiển thị để dữ liệu từ nhiều client có
        // cùng quy ước định danh và trạng thái.
        string normalizedMachineId = NormalizeMachineId(machineId);
        string normalizedStatus = NormalizeStatus(status);

        // Status runtime đầu tiên cũng là tín hiệu thay dữ liệu mẫu bằng dữ liệu thật.
        EnsureRuntimeMachineDataActive();
        UpsertMachineRow(normalizedMachineId, normalizedStatus);
        UpsertMachineCard(normalizedMachineId, normalizedStatus);

        // Sự kiện của client khác không được giành mất lựa chọn hiện tại của admin.
        // Chỉ tự chọn khi danh sách runtime chưa có máy nào đang được chọn, ví dụ
        // client đầu tiên vừa kết nối.
        if (string.IsNullOrWhiteSpace(_selectedMachineName))
        {
            SelectMachine(normalizedMachineId);
        }

        lblServerStatus.Text = string.Format(
            UiStrings.MainRuntimeStatusUpdatedTemplate,
            normalizedMachineId,
            normalizedStatus);

        if (string.Equals(normalizedStatus, "ONLINE", StringComparison.OrdinalIgnoreCase)
            && !IsServerMachine(normalizedMachineId))
        {
            _ = EnsureBillingForMachineAsync(normalizedMachineId);
        }
        else if (string.Equals(normalizedStatus, "OFFLINE", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedStatus, "DISCONNECT", StringComparison.OrdinalIgnoreCase))
        {
            _ = CloseBillingForMachineAsync(normalizedMachineId);
        }
    }

    /// <summary>
    /// Hiển thị kết quả thực thi lệnh admin. Có thể được gọi từ callback networking
    /// nên cũng bảo đảm việc cập nhật label diễn ra trên UI thread.
    /// </summary>
    public void ApplyCommandResultUpdate(AdminCommandResult result)
    {
        // Fail-fast khi caller vi phạm contract thay vì phát sinh lỗi khó hiểu phía sau.
        ArgumentNullException.ThrowIfNull(result);

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyCommandResultUpdate(result));
            return;
        }

        lblServerStatus.Text = FormatCommandResult(result);
    }

    /// <summary>
    /// Tạo một card máy hoàn chỉnh gồm Panel, biểu tượng màn hình tự vẽ và nhãn tên/
    /// trạng thái. machineName được lưu trong Tag để handler click dùng lại.
    /// </summary>
    private Panel CreateMachineCard(string machineName, string status)
    {
        // Panel là container và cũng giữ MachineId đại diện cho toàn bộ card.
        var card = new Panel
        {
            Width = 118,
            Height = 104,
            Margin = new Padding(24, 12, 24, 12),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand,
            Tag = machineName
        };

        // PictureBox không tải file ảnh; sự kiện Paint sẽ vẽ icon theo status trong Tag.
        var icon = new PictureBox
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.White,
            Cursor = Cursors.Hand,
            Tag = status
        };
        icon.Paint += MachineIcon_Paint;

        // Label lấp phần diện tích còn lại và hiển thị cùng lúc tên máy, trạng thái.
        var label = new Label
        {
            Name = MachineCardLabelName,
            Dock = DockStyle.Fill,
            Text = FormatMachineLabel(machineName, status),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Tag = machineName
        };

        var unreadBadge = new Label
        {
            Name = UnreadBadgeLabelName,
            AutoSize = false,
            Width = 24,
            Height = 20,
            Location = new Point(card.Width - 30, 4),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.FromArgb(220, 45, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Tag = machineName,
            Visible = false
        };

        // Thứ tự Add kết hợp với Dock quyết định icon ở trên, label ở dưới.
        card.Controls.Add(label);
        card.Controls.Add(icon);
        card.Controls.Add(unreadBadge);
        unreadBadge.BringToFront();

        // Gắn cùng handler cho container và hai control con để click ở bất kỳ vùng
        // nào trên card cũng chọn đúng máy.
        card.Click += MachineCard_Click;
        icon.Click += MachineCard_Click;
        label.Click += MachineCard_Click;
        unreadBadge.Click += MachineCard_Click;

        UpdateUnreadChatBadge(card, GetUnreadChatCount(machineName));

        return card;
    }

    /// <summary>
    /// Tự vẽ biểu tượng máy tính và chấm trạng thái. Chấm xanh biểu thị ONLINE, đỏ
    /// biểu thị OFFLINE/DISCONNECT, các trạng thái khác dùng màu xám.
    /// </summary>
    private void MachineIcon_Paint(object? sender, PaintEventArgs e)
    {
        // Handler chỉ hợp lệ với PictureBox; bỏ qua sender sai kiểu để tránh cast lỗi.
        if (sender is not PictureBox icon)
        {
            return;
        }

        // Status được gắn vào Tag lúc tạo/cập nhật card.
        string status = icon.Tag?.ToString() ?? "AVAILABLE";
        Color statusColor = status switch
        {
            "ONLINE" => Color.FromArgb(31, 122, 58),
            "OFFLINE" => Color.FromArgb(170, 45, 45),
            "DISCONNECT" => Color.FromArgb(170, 45, 45),
            _ => Color.FromArgb(120, 120, 120)
        };

        // AntiAlias làm các cạnh tròn mượt hơn. Các brush/pen dùng using để giải
        // phóng GDI resource ngay sau mỗi lượt Paint.
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var blackBrush = new SolidBrush(Color.Black);
        using var whiteBrush = new SolidBrush(Color.White);
        using var statusBrush = new SolidBrush(statusColor);
        using var pen = new Pen(Color.Black, 3F);

        // Các hình chữ nhật/elip dưới đây ghép thành màn hình, chân đế và đèn trạng thái.
        Rectangle monitor = new(22, 8, 72, 44);
        e.Graphics.FillRectangle(blackBrush, monitor);
        e.Graphics.DrawRectangle(pen, monitor);
        e.Graphics.FillRectangle(whiteBrush, 29, 15, 58, 29);
        e.Graphics.FillEllipse(whiteBrush, 54, 50, 8, 8);
        e.Graphics.FillRectangle(blackBrush, 53, 58, 10, 8);
        e.Graphics.FillRectangle(blackBrush, 40, 66, 36, 5);
        e.Graphics.FillEllipse(statusBrush, 83, 8, 12, 12);
    }

    /// <summary>
    /// Xử lý click từ Panel/PictureBox/Label của card và quy về một MachineId.
    /// </summary>
    private void MachineCard_Click(object? sender, EventArgs e)
    {
        string? machineName = GetMachineNameFromCardSender(sender);

        if (!string.IsNullOrWhiteSpace(machineName))
        {
            SelectMachine(machineName);
        }
    }

    /// <summary>
    /// Đồng bộ lựa chọn từ DataGridView sang toàn bộ giao diện khi admin đổi dòng.
    /// </summary>
    private void DgvMachines_SelectionChanged(object? sender, EventArgs e)
    {
        // SelectMachine tự đổi CurrentCell nên sự kiện này sẽ phát lại. Cờ bảo vệ
        // ngăn vòng gọi lặp giữa SelectionChanged và SelectMachine.
        if (_isSelectingMachine)
        {
            return;
        }

        // MachineNameColumn là cột định danh nội bộ, dùng để tìm đúng card và lịch sử chat.
        if (dgvMachines.CurrentRow?.Cells["MachineNameColumn"].Value is string machineName)
        {
            SelectMachine(machineName);
        }
    }

    /// <summary>
    /// Đặt một máy làm ngữ cảnh thao tác hiện tại và đồng bộ bảng, card, khu vực chat
    /// cùng dòng trạng thái. Đây là điểm chọn máy dùng chung cho mọi nguồn sự kiện.
    /// </summary>
    private void SelectMachine(string machineName)
    {
        if (string.IsNullOrWhiteSpace(machineName))
        {
            return;
        }

        // Bật cờ trước khi thay đổi DataGridView để handler SelectionChanged không
        // gọi ngược lại hàm này.
        _isSelectingMachine = true;

        try
        {
            // Cập nhật ngữ cảnh nghiệp vụ trước, sau đó render chat và trạng thái nút
            // dựa trên MachineId vừa chọn.
            _selectedMachineName = machineName;
            lblSelectedClient.Text = string.Format(UiStrings.ChatWithMachineTemplate, machineName);
            MarkMachineChatAsRead(machineName);
            RenderSelectedChatHistory();
            RenderSelectedBillingTopUpRequest();
            SetChatActionEnabled(true);
            SetBillingActionEnabled(_adminBilling is not null
                && _isRuntimeMachineDataActive
                && !IsServerMachine(machineName));
            lblServerStatus.Text = string.Format(UiStrings.MainSelectedMachineStatusTemplate, machineName);
            _ = SyncBillingForMachineAsync(machineName);

            // DataGridView không biết lựa chọn từ card, vì vậy duyệt các dòng để chọn
            // đúng dòng có MachineName tương ứng (không phân biệt hoa/thường).
            foreach (DataGridViewRow row in dgvMachines.Rows)
            {
                bool isSelected = string.Equals(
                    row.Cells["MachineNameColumn"].Value?.ToString(),
                    machineName,
                    StringComparison.OrdinalIgnoreCase);
                row.Selected = isSelected;

                if (isSelected)
                {
                    // CurrentCell giúp DataGridView cuộn/đặt dòng hiện hành đúng máy.
                    dgvMachines.CurrentCell = row.Cells[0];
                }
            }

            // Cập nhật màu nền và border để card được chọn có phản hồi trực quan.
            UpdateMachineCardSelection(machineName);
        }
        finally
        {
            // Luôn trả cờ về false kể cả khi việc cập nhật control phát sinh lỗi.
            _isSelectingMachine = false;
        }
    }

    /// <summary>
    /// Gửi nội dung chat từ server tới máy đang chọn, lưu tin nhắn thành công vào
    /// lịch sử cục bộ và khôi phục trạng thái nhập sau khi hoàn tất.
    /// </summary>
    private async void BtnSendChat_Click(object? sender, EventArgs e)
        => await SendAdminChatAsync();

    private void TxtChatMessage_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            _ = SendAdminChatAsync();
        }
    }

    private async Task SendAdminChatAsync()
    {
        // Trim loại bỏ khoảng trắng đầu/cuối và coi chuỗi toàn khoảng trắng là rỗng.
        string message = txtChatMessage.Text.Trim();

        if (message.Length == 0 || string.IsNullOrWhiteSpace(_selectedMachineName))
        {
            // Tin nhắn rỗng được bỏ qua; riêng trường hợp chưa chọn máy thì báo rõ lý do.
            if (string.IsNullOrWhiteSpace(_selectedMachineName))
            {
                lblServerStatus.Text = UiStrings.MainNoMachineSelectedStatus;
            }

            return;
        }

        // Chụp lại MachineId trước await. Nếu admin đổi lựa chọn lúc đang gửi, kết quả
        // vẫn gắn với đúng máy là đích của request ban đầu.
        string targetMachineId = _selectedMachineName;
        var request = new AdminChatRequest(targetMachineId, message);

        lblServerStatus.Text = string.Format(UiStrings.MainChatSendingTemplate, targetMachineId);
        // Khóa ô nhập và nút gửi để tránh gửi trùng trong khi request chưa kết thúc.
        SetChatActionEnabled(false);

        try
        {
            AdminChatResult result = await _adminChat.SendAsync(request);
            lblServerStatus.Text = FormatChatResult(result);

            // Chỉ ghi lịch sử phía server khi service xác nhận không có lỗi.
            if (!result.IsError)
            {
                AppendChatMessage(new AdminChatMessage(
                    targetMachineId,
                    UiStrings.ServerPrefix,
                    message,
                    DateTimeOffset.Now));

                txtChatMessage.Clear();
                RenderSelectedChatHistory();
            }
        }
        catch (Exception ex)
        {
            // Chuyển exception ngoài dự kiến thành kết quả lỗi thống nhất để UI dùng
            // chung quy tắc format với lỗi do service trả về.
            AdminChatResult error = AdminChatResult.ControlledError(
                request,
                "CHAT_SERVICE_ERROR",
                ex.Message);
            lblServerStatus.Text = FormatChatResult(error);
        }
        finally
        {
            // Chỉ mở lại chat khi vẫn còn máy được chọn và trả focus về ô nhập để
            // admin có thể tiếp tục gõ.
            SetChatActionEnabled(!string.IsNullOrWhiteSpace(_selectedMachineName));
            txtChatMessage.Focus();
        }
    }

    private async void BtnBroadcastNotification_Click(object? sender, EventArgs e)
    {
        string message = txtChatMessage.Text.Trim();
        if (message.Length == 0)
        {
            return;
        }

        var request = new AdminNotificationRequest("ALL", message, "Info", "Broadcast");

        lblServerStatus.Text = "Dang broadcast thong bao...";
        SetChatActionEnabled(false);

        try
        {
            AdminNotificationResult result = await _adminNotification.BroadcastAsync(request);
            lblServerStatus.Text = FormatNotificationResult(result);

            if (!result.IsError)
            {
                foreach (string machineId in _chatHistoryByMachine.Keys.ToArray())
                {
                    AppendChatMessage(new AdminChatMessage(
                        machineId,
                        "Broadcast",
                        message,
                        DateTimeOffset.Now));
                }

                txtChatMessage.Clear();
                RenderSelectedChatHistory();
            }
        }
        catch (Exception ex)
        {
            AdminNotificationResult error = AdminNotificationResult.ControlledError(
                request,
                "NOTIFICATION_BROADCAST_SERVICE_ERROR",
                ex.Message);
            lblServerStatus.Text = FormatNotificationResult(error);
        }
        finally
        {
            SetChatActionEnabled(!string.IsNullOrWhiteSpace(_selectedMachineName));
            txtChatMessage.Focus();
        }
    }

    /// <summary>
    /// Nhận tin nhắn client gửi lên, chuẩn hóa MachineId, lưu đúng cuộc hội thoại và
    /// chỉ render lại ngay nếu admin đang xem máy đó.
    /// </summary>
    public void ApplyIncomingChatMessage(AdminChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Callback nhận mạng không bảo đảm chạy trên UI thread.
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyIncomingChatMessage(message));
            return;
        }

        // Record expression tạo bản sao với MachineId đã chuẩn hóa, không sửa object
        // message do tầng networking sở hữu.
        string machineId = NormalizeMachineId(message.MachineId);
        AdminChatMessage normalizedMessage = message with { MachineId = machineId };
        AppendChatMessage(normalizedMessage);
        if (TryParseTopUpRequest(normalizedMessage, out long requestedAmount))
        {
            AddTopUpChatAction(normalizedMessage, requestedAmount);
            _ = EnsureMachineLockedForTopUpRequestAsync(machineId);
        }

        // Tin nhắn của máy khác vẫn được lưu nhưng không thay nội dung admin đang đọc.
        if (string.Equals(_selectedMachineName, machineId, StringComparison.OrdinalIgnoreCase))
        {
            MarkMachineChatAsRead(machineId);
            RenderSelectedChatHistory();
            RenderSelectedBillingTopUpRequest();
        }
        else
        {
            IncrementUnreadChatCount(machineId);
        }

        lblServerStatus.Text = string.Format(
            UiStrings.MainChatReceivedTemplate,
            machineId);
        ShowIncomingChatNotification(normalizedMessage);
    }

    private async Task EnsureMachineLockedForTopUpRequestAsync(string machineId)
    {
        AdminBillingResult? syncResult = null;

        try
        {
            if (_adminBilling is not null)
            {
                syncResult = await _adminBilling.SyncMachineAsync(machineId).ConfigureAwait(true);
                if (syncResult is not null)
                {
                    ApplyBillingUpdate(syncResult);
                }
            }

            if (!ShouldLockAfterRejectedTopUp(syncResult))
            {
                return;
            }

            var request = new AdminCommandRequest(
                machineId,
                CommandType.LOCK,
                AdminCommandIssuer,
                "Client requested top-up while account balance is depleted.");
            AdminCommandResult result = await _adminCommands.SendAsync(request).ConfigureAwait(true);
            if (string.Equals(_selectedMachineName, machineId, StringComparison.OrdinalIgnoreCase))
            {
                lblServerStatus.Text = result.IsError
                    ? $"Yeu cau nap tien nhung khoa {machineId} loi: {result.Message}"
                    : $"Da khoa {machineId} trong luc cho admin xac nhan nap tien.";
            }
        }
        catch (Exception ex)
        {
            if (string.Equals(_selectedMachineName, machineId, StringComparison.OrdinalIgnoreCase))
            {
                lblServerStatus.Text = $"Khong the khoa {machineId} sau yeu cau nap tien: {ex.Message}";
            }
        }
    }

    private void ShowIncomingChatNotification(AdminChatMessage message)
    {
        SystemSounds.Asterisk.Play();

        string notification = $"Tin nhan moi tu {message.MachineId}: {message.Message}";
        Control anchor = FindMachineCard(message.MachineId) is Control card
            ? card
            : chatGroup;
        _incomingChatTip.Show(notification, anchor, 16, 16, 5000);
    }

    public async Task HandleTopUpRequestDecisionAsync(
        string machineId,
        long requestedAmount,
        DialogResult decision,
        CancellationToken cancellationToken = default)
    {
        string targetMachineId = NormalizeMachineId(machineId);
        if (string.IsNullOrWhiteSpace(targetMachineId)
            || string.Equals(targetMachineId, "UNKNOWN", StringComparison.OrdinalIgnoreCase)
            || IsServerMachine(targetMachineId))
        {
            lblServerStatus.Text = "Yeu cau nap tien khong co may hop le.";
            return;
        }

        if (requestedAmount <= 0)
        {
            lblServerStatus.Text = $"Yeu cau nap tien cua {targetMachineId} khong hop le.";
            return;
        }

        if (decision == DialogResult.Yes)
        {
            if (_adminBilling is null)
            {
                lblServerStatus.Text = "Dịch vụ tính tiền chưa sẵn sàng.";
                return;
            }

            AdminBillingResult result = await _adminBilling.TopUpMachineAsync(
                targetMachineId,
                requestedAmount,
                cancellationToken);
            ApplyBillingUpdate(result);
            await LoadCustomerDataAsync().ConfigureAwait(true);
            lblServerStatus.Text = result.IsError
                ? $"Khong the nap tien cho {targetMachineId}: {result.Message}"
                : $"Da xac nhan nap {FormatMoney(requestedAmount)} cho {targetMachineId}.";
            return;
        }

        if (decision == DialogResult.No)
        {
            AdminBillingResult? syncResult = _adminBilling is null
                ? null
                : await _adminBilling.SyncMachineAsync(targetMachineId, cancellationToken);
            if (syncResult is not null)
            {
                ApplyBillingUpdate(syncResult);
            }

            if (!ShouldLockAfterRejectedTopUp(syncResult))
            {
                long remainingSeconds = syncResult?.Timer?.RemainingUsageSeconds
                    ?? syncResult?.Timer?.RemainingSeconds
                    ?? 0;
                lblServerStatus.Text =
                    $"Da tu choi nap tien cho {targetMachineId}; may van con {FormatDuration(remainingSeconds)} theo so du nen khong khoa.";
                return;
            }

            var request = new AdminCommandRequest(
                targetMachineId,
                CommandType.LOCK,
                AdminCommandIssuer,
                "Top-up request rejected and balance is depleted.");
            AdminCommandResult result = await _adminCommands.SendAsync(request, cancellationToken);
            lblServerStatus.Text = result.IsError
                ? $"Tu choi nap tien nhung khoa {targetMachineId} loi: {result.Message}"
                : $"Da tu choi nap tien va giu {targetMachineId} o trang thai khoa.";
        }
    }

    private static bool ShouldLockAfterRejectedTopUp(AdminBillingResult? syncResult)
    {
        Shared.DTOs.CommandPayloads.TimerPayload? timer = syncResult?.Timer;
        if (timer is null)
        {
            return true;
        }

        if (timer.ShouldLockNow)
        {
            return true;
        }

        if (timer.RemainingUsageSeconds is not null)
        {
            return timer.RemainingUsageSeconds.Value <= 0;
        }

        return timer.RemainingSeconds is <= 0;
    }

    private DialogResult ShowTopUpDecisionDialog(string machineId, long requestedAmount)
    {
        using var dialog = new Form
        {
            Text = "Yêu cầu nạp tiền",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(420, 148),
            Padding = new Padding(14)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"{machineId} yêu cầu nạp {FormatMoney(requestedAmount)}.{Environment.NewLine}Bạn muốn xử lý yêu cầu này như thế nào?",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular)
        }, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var confirmButton = new Button
        {
            Text = "Xác nhận",
            Width = 92,
            DialogResult = DialogResult.Yes
        };
        var rejectButton = new Button
        {
            Text = "Từ chối",
            Width = 92,
            DialogResult = DialogResult.No
        };
        buttons.Controls.Add(confirmButton);
        buttons.Controls.Add(rejectButton);
        layout.Controls.Add(buttons, 0, 1);

        dialog.AcceptButton = confirmButton;
        dialog.CancelButton = rejectButton;
        dialog.Controls.Add(layout);
        return dialog.ShowDialog(this);
    }

    public static bool TryParseTopUpRequest(AdminChatMessage message, out long requestedAmount)
    {
        requestedAmount = 0;
        ArgumentNullException.ThrowIfNull(message);

        string machineId = NormalizeMachineId(message.MachineId);
        if (string.Equals(machineId, "UNKNOWN", StringComparison.OrdinalIgnoreCase)
            || IsServerMachine(machineId))
        {
            return false;
        }

        string text = message.Message.Trim();
        if (text.Length == 0)
        {
            return false;
        }

        const string pattern = @"^\s*(?<machine>PC\d+)\s+(?:yêu\s+cầu\s+nạp|yeu\s+cau\s+nap)\s+(?<amount>[\d.,]+)\s*VND\s*$";
        var match = System.Text.RegularExpressions.Regex.Match(
            text,
            pattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return false;
        }

        string requestedMachineId = NormalizeMachineId(match.Groups["machine"].Value);
        if (!string.Equals(requestedMachineId, machineId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string digits = new(match.Groups["amount"].Value.Where(char.IsDigit).ToArray());
        return long.TryParse(
            digits,
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out requestedAmount)
            && requestedAmount > 0;
    }

    // Adapter sự kiện rất mỏng: mọi quy tắc thread, chuẩn hóa và render nằm trong
    // ApplyIncomingChatMessage để cả callback và caller khác dùng chung một luồng.
    private void AdminChat_MessageReceived(AdminChatMessage message)
        => ApplyIncomingChatMessage(message);

    private async void BillingRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (_adminBilling is not null)
        {
            await _adminBilling.RefreshActiveSessionsAsync();
        }
    }

    private void AdminBilling_BillingUpdated(AdminBillingResult result)
        => ApplyBillingUpdate(result);

    private void BillingRateSave_Click(object? sender, EventArgs e)
    {
        _billingRateStatusLabel.Text =
            $"Giá hiện tại: {FormatMoney(GetBillingRatePerHour())}/giờ. Áp dụng cho phiên client mới.";
        lblServerStatus.Text = $"Đã cập nhật giá tiền/giờ: {FormatMoney(GetBillingRatePerHour())}/giờ.";
    }

    public async Task SyncBillingForMachineAsync(string machineId)
    {
        if (_adminBilling is null)
        {
            return;
        }

        if (IsServerMachine(machineId))
        {
            if (string.Equals(_selectedMachineName, machineId, StringComparison.OrdinalIgnoreCase))
            {
                _txtBillingMonitor.Text = "Bạn sẽ nhận thông báo nạp tiền từ client tại đây.";
            }

            return;
        }

        AdminBillingResult? result = await _adminBilling.SyncMachineAsync(machineId);
        if (result is not null)
        {
            ApplyBillingUpdate(result);
        }
        else if (string.Equals(_selectedMachineName, machineId, StringComparison.OrdinalIgnoreCase))
        {
            _txtBillingMonitor.Text = "Bạn sẽ nhận thông báo nạp tiền từ client tại đây.";
        }
    }

    public async Task EnsureBillingForMachineAsync(string machineId)
    {
        if (_adminBilling is null)
        {
            return;
        }

        if (IsServerMachine(machineId))
        {
            if (string.Equals(_selectedMachineName, machineId, StringComparison.OrdinalIgnoreCase))
            {
                _txtBillingMonitor.Text = "Bạn sẽ nhận thông báo nạp tiền từ client tại đây.";
            }

            return;
        }

        AdminBillingResult result = await _adminBilling.EnsureOpenEndedAsync(machineId, GetBillingRatePerHour());
        ApplyBillingUpdate(result);
    }

    private async Task CloseBillingForMachineAsync(string machineId)
    {
        if (_adminBilling is null)
        {
            return;
        }

        if (IsServerMachine(machineId))
        {
            return;
        }

        AdminBillingResult result = await _adminBilling.CloseAsync(machineId);
        if (result.IsSuccess || !string.Equals(result.ErrorCode, "BILLING_SESSION_NOT_FOUND", StringComparison.OrdinalIgnoreCase))
        {
            ApplyBillingUpdate(result);
        }
    }

    public async Task RefreshBillingSessionsAsync()
    {
        if (_adminBilling is null)
        {
            return;
        }

        IReadOnlyList<AdminBillingResult> results = await _adminBilling.RefreshActiveSessionsAsync();
        AdminBillingResult? selectedResult = results.FirstOrDefault(result =>
            string.Equals(result.MachineId, _selectedMachineName, StringComparison.OrdinalIgnoreCase));
        if (selectedResult is not null)
        {
            ApplyBillingUpdate(selectedResult);
        }
    }

    private void ApplyBillingUpdate(AdminBillingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyBillingUpdate(result));
            return;
        }

        lblServerStatus.Text = result.IsError
            ? $"Cập nhật sử dụng {result.MachineId}: {result.ErrorCode ?? "Lỗi"} - {result.Message}"
            : $"Cập nhật sử dụng {result.MachineId}: {result.Message}";

        if (result.Timer is not null)
        {
            UpdateMachineBillingAmount(result.Timer.MachineId, result.Timer.AmountVnd);
        }

        if (!string.Equals(result.MachineId, _selectedMachineName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _txtBillingMonitor.Text = "Bạn sẽ nhận thông báo nạp tiền từ client tại đây.";
    }

    private long GetBillingRatePerHour()
        => _billingRatePerHourInput.Value <= 0
            ? 10_000
            : decimal.ToInt64(_billingRatePerHourInput.Value);

    private static string FormatDuration(long totalSeconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        return $"{(long)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }

    /// <summary>
    /// Ánh xạ nút thao tác máy sang CommandType và điều phối việc gửi lệnh.
    /// </summary>
    private async void MachineAction_Click(object? sender, EventArgs e)
    {
        CommandType? command = sender switch
        {
            Button button when button == btnLockMachine => CommandType.LOCK,
            Button button when button == btnUnlockMachine => CommandType.UNLOCK,
            Button button when button == btnShutdownMachine => CommandType.SHUTDOWN,
            _ => null
        };

        // Chuỗi action dùng cho thông báo UI, độc lập với enum gửi qua mạng.
        string action = command switch
        {
            CommandType.LOCK => UiStrings.MainLockMachine,
            CommandType.UNLOCK => UiStrings.MainUnlockMachine,
            CommandType.SHUTDOWN => UiStrings.MainShutdownMachine,
            _ when sender == btnShutdownMachine => UiStrings.MainShutdownMachine,
            _ => UiStrings.MainPendingAction
        };

        // Mọi lệnh máy đều cần một đích cụ thể.
        if (string.IsNullOrWhiteSpace(_selectedMachineName))
        {
            lblServerStatus.Text = UiStrings.MainNoMachineSelectedStatus;
            return;
        }

        if (command is null)
        {
            lblServerStatus.Text = string.Format(UiStrings.MainActionPendingTemplate, action, _selectedMachineName);
            return;
        }

        // Tại đây command và máy đích đều hợp lệ.
        await SendMachineCommandAsync(command.Value, action, _selectedMachineName);
    }

    /// <summary>
    /// Đóng gói và gửi lệnh khóa/mở khóa tới máy đích. Trong thời gian chờ, hai nút
    /// lệnh bị khóa để không tạo request trùng.
    /// </summary>
    private async Task SendMachineCommandAsync(CommandType command, string action, string machineName)
    {
        // Reason phụ thuộc loại lệnh và IssuedBy giúp tầng nhận biết nguồn phát lệnh.
        var request = new AdminCommandRequest(
            machineName,
            command,
            AdminCommandIssuer,
            command switch
            {
                CommandType.LOCK => UiStrings.MainCommandLockReason,
                CommandType.UNLOCK => UiStrings.MainCommandUnlockReason,
                CommandType.SHUTDOWN => "Admin requested client shutdown.",
                _ => "Admin requested machine command."
            });

        lblServerStatus.Text = string.Format(UiStrings.MainCommandSubmittingTemplate, action, machineName);
        SetMachineActionButtonsEnabled(false);

        try
        {
            // Service có thể là implementation mạng thật hoặc implementation
            // Unavailable trả lỗi có kiểm soát do constructor cung cấp.
            AdminCommandResult result = await _adminCommands.SendAsync(request);
            ApplyCommandResultUpdate(result);
        }
        catch (Exception ex)
        {
            // Chuẩn hóa exception thành AdminCommandResult để chỉ có một đường hiển
            // thị kết quả trên giao diện.
            ApplyCommandResultUpdate(AdminCommandResult.ControlledError(
                request,
                "COMMAND_SERVICE_ERROR",
                ex.Message));
        }
        finally
        {
            // Chỉ cho thao tác lại khi form đang hiển thị dữ liệu máy runtime thật.
            SetMachineActionButtonsEnabled(_isRuntimeMachineDataActive);
        }
    }

    private async void CustomerAction_Click(object? sender, EventArgs e)
    {
        if (sender == btnCancelCustomer)
        {
            ClearCustomerInputs();
            dgvCustomers.ClearSelection();
            lblServerStatus.Text = "Đã hủy thao tác khách hàng.";
            return;
        }

        if (_customers is null)
        {
            ShowCustomerMessage("Chưa kết nối database khách hàng.", MessageBoxIcon.Warning);
            return;
        }

        try
        {
            if (sender == btnDeleteCustomer)
            {
                await DeleteSelectedCustomerAsync();
                return;
            }

            if (sender == btnTopUpCustomer)
            {
                await TopUpSelectedCustomerAsync();
                return;
            }

            if (!TryBuildCustomerRecord(out CustomerRecord? customer) || customer is null)
            {
                return;
            }

            if (sender == btnAddCustomer)
            {
                await _customers.AddAsync(customer).ConfigureAwait(true);
                await LoadCustomerDataAsync(customer.CustomerId);
                lblServerStatus.Text = $"Đã thêm khách hàng {customer.CustomerId}.";
                return;
            }

            if (sender == btnEditCustomer)
            {
                var existing = await _customers.GetByIdAsync(customer.CustomerId).ConfigureAwait(true);
                if (existing is null)
                {
                    ShowCustomerMessage($"Không tìm thấy khách hàng {customer.CustomerId} để sửa.", MessageBoxIcon.Warning);
                    return;
                }

                await _customers.UpdateAsync(customer).ConfigureAwait(true);
                await LoadCustomerDataAsync(customer.CustomerId);
                lblServerStatus.Text = $"Đã cập nhật khách hàng {customer.CustomerId}.";
            }
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            ShowCustomerMessage("Mã KH hoặc tên đăng nhập đã tồn tại trong database.", MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            ShowCustomerMessage($"Không thể lưu dữ liệu khách hàng: {ex.Message}", MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Tìm MachineId từ control phát sinh click. Panel/Label lưu ID trực tiếp trong
    /// Tag; PictureBox lấy ID từ Tag của Panel cha vì Tag của nó đang chứa status.
    /// </summary>
    private static string? GetMachineNameFromCardSender(object? sender)
    {
        return sender switch
        {
            PictureBox pictureBox => pictureBox.Parent?.Tag as string,
            Control { Tag: string machineName } => machineName,
            Control control => control.Parent?.Tag as string,
            _ => null
        };
    }

    /// <summary>
    /// Đánh dấu card đang chọn bằng nền xanh nhạt và border nổi, đồng thời trả các
    /// card còn lại về giao diện mặc định.
    /// </summary>
    private void UpdateMachineCardSelection(string selectedMachineName)
    {
        foreach (Control control in pnlMachineCards.Controls)
        {
            // FlowLayoutPanel có thể chứa control khác; chỉ xử lý Panel có MachineId.
            if (control is not Panel card || card.Tag is not string machineName)
            {
                continue;
            }

            bool isSelected = string.Equals(
                machineName,
                selectedMachineName,
                StringComparison.OrdinalIgnoreCase);
            card.BackColor = isSelected ? Color.FromArgb(232, 244, 255) : Color.White;
            card.BorderStyle = isSelected ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;

            // Đồng bộ nền Label với Panel. PictureBox giữ nền riêng để icon được vẽ ổn định.
            foreach (Control child in card.Controls)
            {
                if (child is Label { Name: UnreadBadgeLabelName })
                {
                    continue;
                }

                if (child is not PictureBox)
                {
                    child.BackColor = card.BackColor;
                }
            }
        }
    }

    /// <summary>
    /// Chuyển giao diện từ dữ liệu mẫu sang dữ liệu runtime đúng một lần. Việc chuyển
    /// chế độ xóa danh sách/card mẫu, lựa chọn và lịch sử chat cũ.
    /// </summary>
    private void EnsureRuntimeMachineDataActive()
    {
        // Các status update sau chỉ cần upsert, không được xóa lại dữ liệu đang có.
        if (_isRuntimeMachineDataActive)
        {
            return;
        }

        _isRuntimeMachineDataActive = true;
        lblMachineTitle.Text = UiStrings.MainMachineTitle;
        _selectedMachineName = null;
        _chatHistoryByMachine.Clear();
        _unreadChatCountByMachine.Clear();
        _topUpActionsByMachine.Clear();
        SetMachineActionButtonsEnabled(true);
        SetBillingActionEnabled(_adminBilling is not null);
        SetChatActionEnabled(false);
        btnLockMachine.Text = UiStrings.MainLockMachine;
        btnUnlockMachine.Text = UiStrings.MainUnlockMachine;

        // Ghi nhớ trạng thái cờ trước đó để hàm không phá ngữ cảnh của caller.
        bool previousSelectingState = _isSelectingMachine;
        _isSelectingMachine = true;

        try
        {
            // Xóa cả hai biểu diễn của bộ dữ liệu mẫu trong lúc chặn SelectionChanged.
            dgvMachines.Rows.Clear();
            pnlMachineCards.Controls.Clear();
        }
        finally
        {
            // Khôi phục chính xác trạng thái cũ kể cả khi Clear phát sinh lỗi.
            _isSelectingMachine = previousSelectingState;
        }
    }

    /// <summary>
    /// Cập nhật trạng thái của dòng máy đã tồn tại; nếu chưa có thì thêm dòng mới.
    /// Nhờ vậy status event có thể đến trước lần tải repository mà UI vẫn hoạt động.
    /// </summary>
    private void UpsertMachineRow(string machineName, string status)
    {
        foreach (DataGridViewRow row in dgvMachines.Rows)
        {
            // MachineId là khóa logic; so sánh không phân biệt hoa/thường.
            if (!string.Equals(
                    row.Cells["MachineNameColumn"].Value?.ToString(),
                    machineName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            row.Cells["TinhTrangColumn"].Value = status;
            return;
        }

        // Bảng cần hai cột số. Nếu ID không chứa chữ số, helper trả 0 thay vì ném lỗi.
        int machineNumber = TryGetMachineNumber(machineName);
        dgvMachines.Rows.Add(machineNumber, machineNumber, status, FormatMoney(0), machineName);
    }

    private void UpdateMachineBillingAmount(string machineName, long amountVnd)
    {
        foreach (DataGridViewRow row in dgvMachines.Rows)
        {
            if (string.Equals(
                    row.Cells["MachineNameColumn"].Value?.ToString(),
                    machineName,
                    StringComparison.OrdinalIgnoreCase))
            {
                row.Cells["TienSuDungColumn"].Value = FormatMoney(amountVnd);
                return;
            }
        }
    }

    /// <summary>
    /// Cập nhật card theo MachineId hoặc tạo card mới nếu máy vừa xuất hiện.
    /// </summary>
    private void UpsertMachineCard(string machineName, string status)
    {
        foreach (Control control in pnlMachineCards.Controls)
        {
            if (control is Panel card
                && card.Tag is string existingMachineName
                && string.Equals(existingMachineName, machineName, StringComparison.OrdinalIgnoreCase))
            {
                UpdateMachineCardStatus(card, machineName, status);
                return;
            }
        }

        // Máy chưa có card: tạo mới với đầy đủ Paint/Click handler.
        pnlMachineCards.Controls.Add(CreateMachineCard(machineName, status));
    }

    /// <summary>
    /// Ghi status mới vào icon và label của card. Invalidate yêu cầu WinForms phát
    /// lại Paint để chấm màu trạng thái đổi ngay trên màn hình.
    /// </summary>
    private void UpdateMachineCardStatus(Panel card, string machineName, string status)
    {
        foreach (Control child in card.Controls)
        {
            if (child is PictureBox icon)
            {
                icon.Tag = status;
                icon.Invalidate();
            }
            else if (child is Label { Name: MachineCardLabelName } label)
            {
                label.Text = FormatMachineLabel(machineName, status);
            }
        }

        UpdateUnreadChatBadge(card, GetUnreadChatCount(machineName));
    }

    // Quy tắc hiển thị card được gom một chỗ để lúc tạo và cập nhật cho kết quả giống nhau.
    private static string FormatMachineLabel(string machineName, string status)
        => $"{machineName} - {status}";

    private static string FormatMoney(long amountVnd)
        => $"{amountVnd:N0} VND";

    /// <summary>
    /// Chuyển kết quả lệnh thành chuỗi UI. Với lỗi có ErrorCode, mã lỗi được ưu tiên
    /// hơn Status để admin thấy nguyên nhân kỹ thuật cụ thể.
    /// </summary>
    private static string FormatCommandResult(AdminCommandResult result)
    {
        string template = result.IsError
            ? UiStrings.MainCommandErrorTemplate
            : UiStrings.MainCommandResultTemplate;
        string status = result.IsError && !string.IsNullOrWhiteSpace(result.ErrorCode)
            ? result.ErrorCode
            : result.Status;

        return string.Format(
            template,
            result.Command,
            result.MachineId,
            status,
            result.Message);
    }

    /// <summary>
    /// Chuyển kết quả gửi chat thành thông báo trạng thái theo cùng quy ước lỗi của
    /// kết quả lệnh máy.
    /// </summary>
    private static string FormatChatResult(AdminChatResult result)
    {
        string template = result.IsError
            ? UiStrings.MainChatErrorTemplate
            : UiStrings.MainChatResultTemplate;
        string status = result.IsError && !string.IsNullOrWhiteSpace(result.ErrorCode)
            ? result.ErrorCode
            : result.Status;

        return string.Format(template, result.MachineId, status, result.Message);
    }

    private static string FormatNotificationResult(AdminNotificationResult result)
    {
        if (result.IsError)
        {
            string errorCode = string.IsNullOrWhiteSpace(result.ErrorCode)
                ? "NOTIFICATION_ERROR"
                : result.ErrorCode;
            return $"Thong bao toi {result.MachineId} that bai ({errorCode}): {result.Message}";
        }

        return string.IsNullOrWhiteSpace(result.RequestId)
            ? $"Da gui thong bao toi {result.MachineId}."
            : $"Da gui thong bao toi {result.MachineId} ({result.RequestId}).";
    }

    /// <summary>
    /// Thêm tin nhắn vào lịch sử của đúng máy, đồng thời tạo danh sách hội thoại ở
    /// lần nhắn đầu tiên.
    /// </summary>
    private void AppendChatMessage(AdminChatMessage message)
    {
        if (!_chatHistoryByMachine.TryGetValue(message.MachineId, out List<AdminChatMessage>? history))
        {
            // Collection expression [] tạo List rỗng theo kiểu đích đã suy luận.
            history = [];
            _chatHistoryByMachine[message.MachineId] = history;
        }

        history.Add(message);
    }

    private void AddTopUpChatAction(AdminChatMessage message, long requestedAmount)
    {
        if (!_topUpActionsByMachine.TryGetValue(message.MachineId, out List<TopUpChatAction>? actions))
        {
            actions = [];
            _topUpActionsByMachine[message.MachineId] = actions;
        }

        actions.Add(new TopUpChatAction(
            Guid.NewGuid().ToString("N"),
            message,
            requestedAmount));
    }

    private int GetUnreadChatCount(string machineId)
        => _unreadChatCountByMachine.TryGetValue(machineId, out int count) ? count : 0;

    private void IncrementUnreadChatCount(string machineId)
    {
        int count = GetUnreadChatCount(machineId) + 1;
        _unreadChatCountByMachine[machineId] = count;
        UpdateUnreadChatBadge(machineId, count);
    }

    private void MarkMachineChatAsRead(string machineId)
    {
        if (!_unreadChatCountByMachine.Remove(machineId))
        {
            return;
        }

        UpdateUnreadChatBadge(machineId, 0);
    }

    private void UpdateUnreadChatBadge(string machineId, int unreadCount)
    {
        Panel? card = FindMachineCard(machineId);
        if (card is not null)
        {
            UpdateUnreadChatBadge(card, unreadCount);
        }
    }

    private Panel? FindMachineCard(string machineId)
    {
        foreach (Control control in pnlMachineCards.Controls)
        {
            if (control is Panel card
                && card.Tag is string existingMachineName
                && string.Equals(existingMachineName, machineId, StringComparison.OrdinalIgnoreCase))
            {
                return card;
            }
        }

        return null;
    }

    private static void UpdateUnreadChatBadge(Panel card, int unreadCount)
    {
        Label? badge = card.Controls
            .OfType<Label>()
            .FirstOrDefault(label => string.Equals(label.Name, UnreadBadgeLabelName, StringComparison.Ordinal));

        if (badge is null)
        {
            return;
        }

        badge.Visible = unreadCount > 0;
        badge.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
        badge.Width = unreadCount > 99 ? 32 : 24;
        badge.Location = new Point(card.Width - badge.Width - 6, 4);
        badge.BringToFront();
    }

    /// <summary>
    /// Render lịch sử của máy đang chọn vào textbox. Nếu chưa có tin nhắn thì hiển
    /// thị nội dung placeholder; nếu không có máy được chọn thì xóa hoàn toàn.
    /// </summary>
    private void RenderSelectedChatHistory()
    {
        txtChatHistory.Clear();

        if (string.IsNullOrWhiteSpace(_selectedMachineName))
        {
            return;
        }

        if (!_chatHistoryByMachine.TryGetValue(_selectedMachineName, out List<AdminChatMessage>? history)
            || history.Count == 0)
        {
            txtChatHistory.Text = string.Format(UiStrings.ChatHistoryTemplate, _selectedMachineName);
            return;
        }

        txtChatHistory.Text = string.Join(
            Environment.NewLine,
            history.Select(FormatChatMessage));
        txtChatHistory.SelectionStart = txtChatHistory.TextLength;
        txtChatHistory.ScrollToCaret();
    }

    // Định dạng duy nhất cho cả tin server gửi và tin client gửi vào lịch sử.
    private static string FormatChatMessage(AdminChatMessage message)
        => $"[{message.Timestamp:HH:mm:ss}] {message.Sender}: {message.Message}";

    /// <summary>Bật/tắt đồng thời các nút gửi lệnh đã được hỗ trợ.</summary>
    private Control CreateChatMessagePanel(AdminChatMessage message)
    {
        var panel = new Panel
        {
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
            Padding = Padding.Empty,
            BackColor = Color.White,
            Tag = "chat-item"
        };

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 1,
            Dock = DockStyle.Top,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(GetChatItemWidth() - 8, 0),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Text = FormatChatMessage(message),
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);

        panel.Controls.Add(layout);
        panel.Width = GetChatItemWidth();
        return panel;
    }

    private void RenderSelectedBillingTopUpRequest()
    {
        if (_billingTopUpRequestPanel is null)
        {
            return;
        }

        _billingTopUpRequestPanel.Visible = true;
        _billingTopUpRequestLabel.Text = "Bạn sẽ nhận thông báo nạp tiền từ client tại đây.";
        _billingTopUpStatusLabel.Text = string.Empty;
        _billingTopUpStatusLabel.Visible = false;
        _billingTopUpButtonRow.Visible = false;
        _btnRejectBillingTopUp.Tag = null;
        _btnConfirmBillingTopUp.Tag = null;

        if (string.IsNullOrWhiteSpace(_selectedMachineName))
        {
            _billingTopUpRequestLabel.Text = "Chọn một máy để xem thông báo nạp tiền từ client.";
            UpdateBillingTopUpRequestLabelWidth();
            return;
        }

        TopUpChatAction? action = FindVisibleTopUpActionForMachine(_selectedMachineName);
        if (action is null)
        {
            _billingTopUpRequestLabel.Text = $"Chưa có yêu cầu nạp tiền từ {_selectedMachineName}.";
            UpdateBillingTopUpRequestLabelWidth();
            return;
        }

        _billingTopUpRequestLabel.Text = FormatChatMessage(action.Message);
        UpdateBillingTopUpRequestLabelWidth();

        bool isPending = action.Status == TopUpChatActionStatus.Pending;
        _billingTopUpButtonRow.Visible = isPending;
        _btnRejectBillingTopUp.Enabled = isPending && !action.IsBusy;
        _btnConfirmBillingTopUp.Enabled = isPending && !action.IsBusy;
        _btnRejectBillingTopUp.Tag = action;
        _btnConfirmBillingTopUp.Tag = action;

        if (!isPending)
        {
            _billingTopUpStatusLabel.Text = action.Status switch
            {
                TopUpChatActionStatus.Confirmed => $"Da xac nhan nap {FormatMoney(action.RequestedAmount)}.",
                TopUpChatActionStatus.Rejected => "Da tu choi yeu cau nap tien.",
                _ => "Xu ly yeu cau nap tien bi loi."
            };
            _billingTopUpStatusLabel.ForeColor = action.Status == TopUpChatActionStatus.Error
                ? Color.FromArgb(180, 45, 45)
                : Color.FromArgb(55, 110, 55);
            _billingTopUpStatusLabel.Visible = true;
        }

        _billingTopUpRequestPanel.Visible = true;
    }

    private void UpdateBillingTopUpRequestLabelWidth()
    {
        if (_billingTopUpRequestLabel is null || _billingTopUpRequestPanel is null)
        {
            return;
        }

        _billingTopUpRequestLabel.MaximumSize = new Size(
            Math.Max(120, _billingTopUpRequestPanel.ClientSize.Width - 20),
            0);
    }

    private Label CreateChatPlaceholderLabel(string text)
        => new()
        {
            AutoSize = true,
            MaximumSize = new Size(GetChatItemWidth() - 8, 0),
            Text = text,
            ForeColor = Color.Gray,
            Margin = new Padding(0, 0, 0, 4)
        };

    private TopUpChatAction? FindVisibleTopUpActionForMachine(string machineId)
    {
        if (!_topUpActionsByMachine.TryGetValue(machineId, out List<TopUpChatAction>? actions)
            || actions.Count == 0)
        {
            return null;
        }

        return actions.LastOrDefault(action => action.Status == TopUpChatActionStatus.Pending)
            ?? actions.LastOrDefault();
    }

    private async void TopUpConfirmButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button { Tag: TopUpChatAction action })
        {
            await ResolveTopUpActionAsync(action, DialogResult.Yes);
        }
    }

    private async void TopUpRejectButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button { Tag: TopUpChatAction action })
        {
            await ResolveTopUpActionAsync(action, DialogResult.No);
        }
    }

    private async Task ResolveTopUpActionAsync(TopUpChatAction action, DialogResult decision)
    {
        if (action.Status != TopUpChatActionStatus.Pending || action.IsBusy)
        {
            return;
        }

        action.IsBusy = true;
        RenderSelectedBillingTopUpRequest();

        await HandleTopUpRequestDecisionAsync(
            action.Message.MachineId,
            action.RequestedAmount,
            decision);

        action.Status = decision == DialogResult.Yes
            ? TopUpChatActionStatus.Confirmed
            : TopUpChatActionStatus.Rejected;
        action.IsBusy = false;
        RenderSelectedBillingTopUpRequest();
    }

    private void ResizeChatHistoryItems()
    {
        if (_chatHistoryPanel is null)
        {
            return;
        }

        int width = GetChatItemWidth();
        foreach (Control control in _chatHistoryPanel.Controls)
        {
            control.Width = width;
        }
    }

    private int GetChatItemWidth()
        => Math.Max(120, _chatHistoryPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 16);

    private void SetMachineActionButtonsEnabled(bool enabled)
    {
        btnLockMachine.Enabled = enabled;
        btnUnlockMachine.Enabled = enabled;
        btnShutdownMachine.Enabled = enabled;
    }

    /// <summary>Bật/tắt đồng thời ô nhập và nút gửi chat.</summary>
    private void SetChatActionEnabled(bool enabled)
    {
        txtChatMessage.Enabled = enabled;
        btnSendChat.Enabled = enabled;
        btnBroadcastNotification.Enabled = enabled;
    }

    private void SetBillingActionEnabled(bool enabled)
    {
        bool canUseSettings = _adminBilling is not null;
        _billingRatePerHourInput.Enabled = canUseSettings;
        _btnSaveBillingRate.Enabled = canUseSettings;
    }

    /// <summary>
    /// Hủy đăng ký sự kiện trước khi đóng form để service chat không giữ tham chiếu
    /// đến form và không gọi cập nhật control đã Dispose.
    /// </summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _adminChat.MessageReceived -= AdminChat_MessageReceived;
        if (_adminBilling is not null)
        {
            _adminBilling.BillingUpdated -= AdminBilling_BillingUpdated;
        }

        _billingRefreshTimer.Stop();
        _billingRefreshTimer.Tick -= BillingRefreshTimer_Tick;
        _billingRefreshTimer.Dispose();
        _incomingChatTip.Dispose();
        base.OnFormClosed(e);
    }

    // Chuẩn hóa ID tại biên hệ thống: loại khoảng trắng và dùng UNKNOWN cho giá trị rỗng.
    private static string NormalizeMachineId(string machineId)
        => string.IsNullOrWhiteSpace(machineId) ? "UNKNOWN" : machineId.Trim();

    private static bool IsServerMachine(string? machineId)
        => string.Equals(machineId?.Trim(), ServerMachineId, StringComparison.OrdinalIgnoreCase);

    // Chuẩn hóa status thành chữ hoa để switch màu và nội dung hiển thị nhất quán.
    private static string NormalizeStatus(string status)
        => string.IsNullOrWhiteSpace(status) ? "UNKNOWN" : status.Trim().ToUpperInvariant();

    /// <summary>
    /// Trích toàn bộ chữ số trong MachineId để tạo số thứ tự cho DataGridView.
    /// Ví dụ PC01 thành 1; ID không có số trả về 0.
    /// </summary>
    private static int TryGetMachineNumber(string machineName)
    {
        string digits = new(machineName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int machineNumber) ? machineNumber : 0;
    }

    private enum TopUpChatActionStatus
    {
        Pending,
        Confirmed,
        Rejected,
        Error
    }

    private sealed class TopUpChatAction(string id, AdminChatMessage message, long requestedAmount)
    {
        public string Id { get; } = id;

        public AdminChatMessage Message { get; } = message;

        public long RequestedAmount { get; } = requestedAmount;

        public TopUpChatActionStatus Status { get; set; } = TopUpChatActionStatus.Pending;

        public bool IsBusy { get; set; }
    }

    /// <summary>
    /// Adapter nội bộ biến TcpJsonLineServer thành IAdminCommandService. Nhờ lớp này,
    /// MainForm không phụ thuộc trực tiếp vào chi tiết gửi JSON line khi phát lệnh.
    /// </summary>
    private sealed class NetworkAdminCommandService(TcpJsonLineServer networkServer) : IAdminCommandService
    {
        /// <summary>
        /// Chuyển AdminCommandRequest sang lời gọi networking và ánh xạ kết quả tầng
        /// mạng về AdminCommandResult dùng thống nhất ở tầng trình bày.
        /// </summary>
        public async Task<AdminCommandResult> SendAsync(
            AdminCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            // ConfigureAwait(false) phù hợp vì phần ánh xạ sau đó không truy cập UI.
            MachineCommandSendResult commandResult = await networkServer.SendMachineCommandWithResultAsync(
                request.MachineId,
                request.Command,
                request.IssuedBy,
                request.Reason,
                cancellationToken).ConfigureAwait(false);

            return commandResult.Sent
                ? AdminCommandResult.Submitted(request, commandResult.Message, commandResult.RequestId)
                : AdminCommandResult.ControlledError(
                    request,
                    commandResult.ErrorCode ?? "COMMAND_SEND_FAILED",
                    commandResult.Message,
                    commandResult.RequestId);
        }
    }

    private sealed class NetworkAdminChatService : IAdminChatService
    {
        private readonly TcpJsonLineServer _networkServer;

        public NetworkAdminChatService(TcpJsonLineServer networkServer)
        {
            _networkServer = networkServer ?? throw new ArgumentNullException(nameof(networkServer));
            _networkServer.ChatReceived += NetworkServer_ChatReceived;
        }

        public event Action<AdminChatMessage>? MessageReceived;

        public async Task<AdminChatResult> SendAsync(
            AdminChatRequest request,
            CancellationToken cancellationToken = default)
        {
            MachineChatSendResult result = await _networkServer.SendChatAsync(
                request.MachineId,
                UiStrings.ServerPrefix,
                request.Message,
                cancellationToken).ConfigureAwait(false);

            return result.Sent
                ? AdminChatResult.Sent(request, result.Message, result.RequestId)
                : AdminChatResult.ControlledError(
                    request,
                    result.ErrorCode ?? "CHAT_SEND_FAILED",
                    result.Message,
                    result.RequestId);
        }

        private void NetworkServer_ChatReceived(string machineId, Shared.DTOs.Bidrectional.ChatPayload payload)
        {
            MessageReceived?.Invoke(new AdminChatMessage(
                machineId,
                string.IsNullOrWhiteSpace(payload.Sender) ? machineId : payload.Sender.Trim(),
                payload.Message.Trim(),
                DateTimeOffset.Now));
        }
    }

    private sealed class NetworkAdminNotificationService : IAdminNotificationService
    {
        private readonly TcpJsonLineServer _networkServer;

        public NetworkAdminNotificationService(TcpJsonLineServer networkServer)
        {
            _networkServer = networkServer ?? throw new ArgumentNullException(nameof(networkServer));
        }

        public async Task<AdminNotificationResult> SendAsync(
            AdminNotificationRequest request,
            CancellationToken cancellationToken = default)
        {
            MachineNotificationSendResult result = await _networkServer.SendNotificationAsync(
                request.MachineId,
                request.Message,
                request.Severity,
                request.Scope,
                cancellationToken).ConfigureAwait(false);

            return result.Sent
                ? AdminNotificationResult.Sent(request, result.Message, result.RequestId)
                : AdminNotificationResult.ControlledError(
                    request,
                    result.ErrorCode ?? "NOTIFICATION_SEND_FAILED",
                    result.Message,
                    result.RequestId);
        }

        public async Task<AdminNotificationResult> BroadcastAsync(
            AdminNotificationRequest request,
            CancellationToken cancellationToken = default)
        {
            MachineNotificationBroadcastResult result = await _networkServer.BroadcastNotificationAsync(
                request.Message,
                request.Severity,
                cancellationToken).ConfigureAwait(false);

            string message = $"{result.Message} Targets={result.TargetCount}, Sent={result.SentCount}.";
            return result.Sent
                ? AdminNotificationResult.Sent(request, message)
                : AdminNotificationResult.ControlledError(
                    request,
                    result.ErrorCode ?? "NOTIFICATION_BROADCAST_FAILED",
                    message);
        }
    }

    private async Task LoadCustomerDataAsync(string? selectedCustomerId = null)
    {
        dgvCustomers.Rows.Clear();

        if (_customers is null)
        {
            ClearCustomerInputs();
            lblServerStatus.Text = "Chưa kết nối database khách hàng.";
            return;
        }

        IReadOnlyList<CustomerRecord> customers = await _customers.ListAsync().ConfigureAwait(true);
        foreach (CustomerRecord customer in customers)
        {
            dgvCustomers.Rows.Add(
                customer.CustomerId,
                customer.FirstName,
                customer.LastName,
                customer.Phone,
                customer.IdentityNumber,
                customer.Birthday,
                customer.Username,
                customer.Password,
                customer.AccountBalance.ToString());
        }

        if (!string.IsNullOrWhiteSpace(selectedCustomerId) && SelectCustomerRow(selectedCustomerId))
        {
            return;
        }

        if (dgvCustomers.Rows.Count == 0)
        {
            ClearCustomerInputs();
            lblServerStatus.Text = "Danh sách khách hàng đang trống.";
        }
    }

    private async Task DeleteSelectedCustomerAsync()
    {
        if (_customers is null)
        {
            ShowCustomerMessage("Chưa kết nối database khách hàng.", MessageBoxIcon.Warning);
            return;
        }

        string customerId = GetSelectedCustomerId();
        if (string.IsNullOrWhiteSpace(customerId))
        {
            ShowCustomerMessage("Vui lòng chọn khách hàng cần xóa.", MessageBoxIcon.Warning);
            return;
        }

        DialogResult confirm = MessageBox.Show(
            this,
            $"Bạn có chắc muốn xóa khách hàng {customerId}?",
            "Quản lý khách hàng",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await _customers.DeleteAsync(customerId).ConfigureAwait(true);
        await LoadCustomerDataAsync().ConfigureAwait(true);
        ClearCustomerInputs();
        lblServerStatus.Text = $"Đã xóa khách hàng {customerId}.";
    }

    private async Task TopUpSelectedCustomerAsync()
    {
        if (_customers is null)
        {
            ShowCustomerMessage("Chưa kết nối database khách hàng.", MessageBoxIcon.Warning);
            return;
        }

        string customerId = GetSelectedCustomerId();
        if (string.IsNullOrWhiteSpace(customerId))
        {
            ShowCustomerMessage("Vui lòng chọn khách hàng cần nạp tiền.", MessageBoxIcon.Warning);
            return;
        }

        CustomerRecord? customer = await _customers.GetByIdAsync(customerId).ConfigureAwait(true);
        if (customer is null)
        {
            ShowCustomerMessage($"Không tìm thấy khách hàng {customerId}.", MessageBoxIcon.Warning);
            return;
        }

        if (!TryPromptTopUpAmount(out long topUpAmount))
        {
            return;
        }

        CustomerRecord updatedCustomer = customer with
        {
            AccountBalance = customer.AccountBalance + topUpAmount
        };

        await _customers.UpdateAsync(updatedCustomer).ConfigureAwait(true);
        await LoadCustomerDataAsync(updatedCustomer.CustomerId).ConfigureAwait(true);
        await RefreshBillingSessionsAsync().ConfigureAwait(true);
        lblServerStatus.Text =
            $"Đã nạp {FormatMoney(topUpAmount)} cho {updatedCustomer.CustomerId}. Số dư mới: {FormatMoney(updatedCustomer.AccountBalance)}.";
    }

    private bool TryPromptTopUpAmount(out long amount)
    {
        amount = 0;
        using var dialog = new Form
        {
            Text = "Nạp tiền khách hàng",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(320, 132),
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Số tiền nạp thêm (VND)",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var amountInput = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 1_000,
            Maximum = 1_000_000_000,
            Increment = 1_000,
            ThousandsSeparator = true,
            Value = 10_000
        };
        layout.Controls.Add(amountInput, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var okButton = new Button
        {
            Text = "Nạp tiền",
            DialogResult = DialogResult.OK,
            Width = 92
        };
        var cancelButton = new Button
        {
            Text = "Hủy",
            DialogResult = DialogResult.Cancel,
            Width = 76
        };
        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);
        layout.Controls.Add(buttons, 0, 2);

        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;
        dialog.Controls.Add(layout);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        amount = decimal.ToInt64(amountInput.Value);
        return amount > 0;
    }

    private bool TryBuildCustomerRecord(out CustomerRecord? customer)
    {
        customer = null;

        if (!TryReadRequiredText(txtCustomerId, "Mã KH", out string customerId) ||
            !TryReadRequiredText(txtFirstName, "Tên", out string firstName) ||
            !TryReadRequiredText(txtLastName, "Họ", out string lastName) ||
            !TryReadRequiredText(txtPhone, "SĐT", out string phone) ||
            !TryReadRequiredText(txtIdentity, "Số CMND", out string identity) ||
            !TryReadRequiredText(txtBirthday, "Ngày sinh", out string birthday) ||
            !TryReadRequiredText(txtUsername, "Tên đăng nhập", out string username) ||
            !TryReadRequiredText(txtPassword, "Mật khẩu", out string password) ||
            !TryReadRequiredText(txtAccountBalance, "Tài khoản", out string accountBalanceText))
        {
            return false;
        }

        if (!long.TryParse(accountBalanceText, out long accountBalance) || accountBalance < 0)
        {
            ShowCustomerMessage("Tài khoản phải là số không âm.", MessageBoxIcon.Warning, txtAccountBalance);
            return false;
        }

        customer = new CustomerRecord(
            customerId,
            firstName,
            lastName,
            phone,
            identity,
            birthday,
            username,
            password,
            accountBalance);
        return true;
    }

    private bool TryReadRequiredText(TextBox textBox, string fieldName, out string value)
    {
        value = textBox.Text.Trim();
        if (value.Length > 0)
        {
            return true;
        }

        ShowCustomerMessage($"Vui lòng nhập {fieldName}.", MessageBoxIcon.Warning, textBox);
        return false;
    }

    private bool SelectCustomerRow(string customerId)
    {
        foreach (DataGridViewRow row in dgvCustomers.Rows)
        {
            if (!string.Equals(
                    GetCustomerCellValue(row, CustomerIdColumn.Name),
                    customerId,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            row.Selected = true;
            dgvCustomers.CurrentCell = row.Cells[0];
            FillCustomerInputs(row);
            return true;
        }

        return false;
    }

    private void DgvCustomers_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvCustomers.CurrentRow is null || dgvCustomers.CurrentRow.IsNewRow)
        {
            return;
        }

        FillCustomerInputs(dgvCustomers.CurrentRow);
    }

    private void FillCustomerInputs(DataGridViewRow row)
    {
        txtCustomerId.Text = GetCustomerCellValue(row, CustomerIdColumn.Name);
        txtFirstName.Text = GetCustomerCellValue(row, FirstNameColumn.Name);
        txtLastName.Text = GetCustomerCellValue(row, LastNameColumn.Name);
        txtPhone.Text = GetCustomerCellValue(row, PhoneColumn.Name);
        txtIdentity.Text = GetCustomerCellValue(row, IdentityColumn.Name);
        txtBirthday.Text = GetCustomerCellValue(row, BirthdayColumn.Name);
        txtUsername.Text = GetCustomerCellValue(row, UsernameColumn.Name);
        txtPassword.Text = GetCustomerCellValue(row, PasswordColumn.Name);
        txtAccountBalance.Text = GetCustomerCellValue(row, AccountBalanceColumn.Name);
    }

    private void ClearCustomerInputs()
    {
        txtCustomerId.Clear();
        txtFirstName.Clear();
        txtLastName.Clear();
        txtPhone.Clear();
        txtIdentity.Clear();
        txtBirthday.Clear();
        txtUsername.Clear();
        txtPassword.Clear();
        txtAccountBalance.Clear();
    }

    private string GetSelectedCustomerId()
    {
        if (dgvCustomers.CurrentRow is not null && !dgvCustomers.CurrentRow.IsNewRow)
        {
            return GetCustomerCellValue(dgvCustomers.CurrentRow, CustomerIdColumn.Name);
        }

        return txtCustomerId.Text.Trim();
    }

    private static string GetCustomerCellValue(DataGridViewRow row, string columnName)
        => row.Cells[columnName].Value?.ToString() ?? string.Empty;

    private void ShowCustomerMessage(string message, MessageBoxIcon icon, Control? focusControl = null)
    {
        lblServerStatus.Text = message;
        MessageBox.Show(this, message, "Quản lý khách hàng", MessageBoxButtons.OK, icon);
        focusControl?.Focus();
    }
}
