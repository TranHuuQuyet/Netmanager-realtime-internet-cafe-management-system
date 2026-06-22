using ServerApp.Database.Contracts;
using ServerApp.Database.Entities;
using ServerApp.Networking;
using ServerApp.Presentation;
using Shared.Enums;

namespace ServerApp;

public partial class MainForm : Form
{
    // Tiền tố chỉ dùng khi tạo danh sách máy mẫu; dữ liệu thật giữ nguyên MachineId
    // do repository hoặc client gửi lên.
    private const string SampleMachinePrefix = "PC";

    // Định danh nguồn phát lệnh để phía networking có thể ghi log/audit lệnh admin.
    private const string AdminCommandIssuer = "ServerApp.MainForm";

    // Repository có thể null khi chạy giao diện demo không kết nối cơ sở dữ liệu.
    private readonly IMachineRepository? _machines;

    // Hai service luôn có giá trị. Nếu dependency không được truyền vào, constructor
    // dùng implementation "Unavailable" để trả lỗi có kiểm soát thay vì NullReference.
    private readonly IAdminCommandService _adminCommands;
    private readonly IAdminChatService _adminChat;

    // Lưu lịch sử chat riêng theo từng MachineId. So sánh không phân biệt hoa/thường
    // để PC01 và pc01 không tạo thành hai cuộc hội thoại khác nhau.
    private readonly Dictionary<string, List<AdminChatMessage>> _chatHistoryByMachine =
        new(StringComparer.OrdinalIgnoreCase);

    // Cờ chống vòng lặp sự kiện: SelectMachine thay đổi CurrentRow, thao tác đó lại
    // phát SelectionChanged. Khi cờ bật, handler sẽ không chọn máy thêm lần nữa.
    private bool _isSelectingMachine;

    // Phân biệt màn hình dữ liệu mẫu với dữ liệu runtime. Chỉ runtime mới cho phép
    // gửi lệnh khóa/mở khóa tới client thật.
    private bool _isRuntimeMachineDataActive;

    // Máy đang được admin thao tác; đồng thời là đích nhận lệnh và tin nhắn chat.
    private string? _selectedMachineName;

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
    {
        // Lưu dependency và thay dependency thiếu bằng service trả lỗi có kiểm soát.
        _machines = machines;
        _adminCommands = adminCommands ?? new UnavailableAdminCommandService();
        _adminChat = adminChat ?? new UnavailableAdminChatService();

        // Dựng control trước khi cấu hình trạng thái ban đầu vì các hàm sau cần truy
        // cập label, button và textbox do Designer tạo.
        InitializeComponent();
        ConfigureR1ShellState();

        // Nhận tin nhắn đẩy từ client trong suốt vòng đời form. Sự kiện được hủy ở
        // OnFormClosed để service không giữ tham chiếu tới form đã đóng.
        _adminChat.MessageReceived += AdminChat_MessageReceived;
    }

    /// <summary>
    /// Constructor tương thích với TcpJsonLineServer: bọc server thành service gửi
    /// lệnh để phần còn lại của form chỉ phụ thuộc vào IAdminCommandService.
    /// </summary>
    public MainForm(IMachineRepository? machines, TcpJsonLineServer? networkServer)
        : this(
            machines,
            networkServer is null
                ? null
                : new NetworkAdminCommandService(networkServer))
    {
    }

    /// <summary>
    /// Nạp dữ liệu khi form mở. Dữ liệu khách hàng hiện là dữ liệu mẫu; danh sách
    /// máy ưu tiên repository thật và chỉ quay về dữ liệu mẫu khi không có dữ liệu.
    /// </summary>
    private async void MainForm_Load(object sender, EventArgs e)
    {
        LoadSampleCustomerData();

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
            dgvMachines.Rows.Add(machineNumber, machineNumber, statuses[index], machineName);
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
            Dock = DockStyle.Fill,
            Text = FormatMachineLabel(machineName, status),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Tag = machineName
        };

        // Thứ tự Add kết hợp với Dock quyết định icon ở trên, label ở dưới.
        card.Controls.Add(label);
        card.Controls.Add(icon);

        // Gắn cùng handler cho container và hai control con để click ở bất kỳ vùng
        // nào trên card cũng chọn đúng máy.
        card.Click += MachineCard_Click;
        icon.Click += MachineCard_Click;
        label.Click += MachineCard_Click;

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
            RenderSelectedChatHistory();
            SetChatActionEnabled(true);
            lblServerStatus.Text = string.Format(UiStrings.MainSelectedMachineStatusTemplate, machineName);

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

        // Tin nhắn của máy khác vẫn được lưu nhưng không thay nội dung admin đang đọc.
        if (string.Equals(_selectedMachineName, machineId, StringComparison.OrdinalIgnoreCase))
        {
            RenderSelectedChatHistory();
        }

        lblServerStatus.Text = string.Format(
            UiStrings.MainChatReceivedTemplate,
            machineId);
    }

    // Adapter sự kiện rất mỏng: mọi quy tắc thread, chuẩn hóa và render nằm trong
    // ApplyIncomingChatMessage để cả callback và caller khác dùng chung một luồng.
    private void AdminChat_MessageReceived(AdminChatMessage message)
        => ApplyIncomingChatMessage(message);

    /// <summary>
    /// Ánh xạ nút thao tác máy sang CommandType và điều phối việc gửi lệnh. Nút tắt
    /// máy hiện chỉ hiển thị trạng thái "chờ triển khai" vì chưa có CommandType tương ứng.
    /// </summary>
    private async void MachineAction_Click(object? sender, EventArgs e)
    {
        // Chỉ Lock và Unlock được ánh xạ thành lệnh mạng ở phiên bản hiện tại.
        CommandType? command = sender switch
        {
            Button button when button == btnLockMachine => CommandType.LOCK,
            Button button when button == btnUnlockMachine => CommandType.UNLOCK,
            _ => null
        };

        // Chuỗi action dùng cho thông báo UI, độc lập với enum gửi qua mạng.
        string action = command switch
        {
            CommandType.LOCK => UiStrings.MainLockMachine,
            CommandType.UNLOCK => UiStrings.MainUnlockMachine,
            _ when sender == btnShutdownMachine => UiStrings.MainShutdownMachine,
            _ => UiStrings.MainPendingAction
        };

        // Mọi lệnh máy đều cần một đích cụ thể.
        if (string.IsNullOrWhiteSpace(_selectedMachineName))
        {
            lblServerStatus.Text = UiStrings.MainNoMachineSelectedStatus;
            return;
        }

        // Nút có action nhưng chưa có command sẽ không gọi service, tránh gửi một
        // yêu cầu không hợp lệ tới client.
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
            command == CommandType.LOCK
                ? UiStrings.MainCommandLockReason
                : UiStrings.MainCommandUnlockReason);

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

    /// <summary>
    /// Nhận sự kiện từ nhóm nút khách hàng và hiển thị chức năng dự kiến. CRUD khách
    /// hàng chưa được nối tới repository trong phiên bản hiện tại.
    /// </summary>
    private void CustomerAction_Click(object? sender, EventArgs e)
    {
        // So sánh chính instance Button để tất cả nút có thể dùng chung một handler.
        string action = sender switch
        {
            Button button when button == btnAddCustomer => UiStrings.MainAddCustomerButton,
            Button button when button == btnEditCustomer => UiStrings.MainEditCustomerButton,
            Button button when button == btnDeleteCustomer => UiStrings.MainDeleteCustomerButton,
            Button button when button == btnCancelCustomer => UiStrings.MainCancelCustomerButton,
            _ => UiStrings.MainPendingAction
        };

        lblServerStatus.Text = string.Format(UiStrings.MainCustomerActionPendingTemplate, action);
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
        SetMachineActionButtonsEnabled(true);
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
        dgvMachines.Rows.Add(machineNumber, machineNumber, status, machineName);
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
    private static void UpdateMachineCardStatus(Panel card, string machineName, string status)
    {
        foreach (Control child in card.Controls)
        {
            if (child is PictureBox icon)
            {
                icon.Tag = status;
                icon.Invalidate();
            }
            else if (child is Label label)
            {
                label.Text = FormatMachineLabel(machineName, status);
            }
        }
    }

    // Quy tắc hiển thị card được gom một chỗ để lúc tạo và cập nhật cho kết quả giống nhau.
    private static string FormatMachineLabel(string machineName, string status)
        => $"{machineName} - {status}";

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

    /// <summary>
    /// Render lịch sử của máy đang chọn vào textbox. Nếu chưa có tin nhắn thì hiển
    /// thị nội dung placeholder; nếu không có máy được chọn thì xóa hoàn toàn.
    /// </summary>
    private void RenderSelectedChatHistory()
    {
        if (string.IsNullOrWhiteSpace(_selectedMachineName))
        {
            txtChatHistory.Clear();
            return;
        }

        if (!_chatHistoryByMachine.TryGetValue(_selectedMachineName, out List<AdminChatMessage>? history)
            || history.Count == 0)
        {
            txtChatHistory.Text = string.Format(UiStrings.ChatHistoryTemplate, _selectedMachineName);
            return;
        }

        // Mỗi message thành một dòng có thời gian/người gửi; sau đó cuộn tới cuối để
        // tin mới nhất luôn nằm trong vùng nhìn thấy.
        txtChatHistory.Lines = history
            .Select(FormatChatMessage)
            .ToArray();
        txtChatHistory.SelectionStart = txtChatHistory.TextLength;
        txtChatHistory.ScrollToCaret();
    }

    // Định dạng duy nhất cho cả tin server gửi và tin client gửi vào lịch sử.
    private static string FormatChatMessage(AdminChatMessage message)
        => $"[{message.Timestamp:HH:mm:ss}] {message.Sender}: {message.Message}";

    /// <summary>Bật/tắt đồng thời các nút gửi lệnh đã được hỗ trợ.</summary>
    private void SetMachineActionButtonsEnabled(bool enabled)
    {
        btnLockMachine.Enabled = enabled;
        btnUnlockMachine.Enabled = enabled;
    }

    /// <summary>Bật/tắt đồng thời ô nhập và nút gửi chat.</summary>
    private void SetChatActionEnabled(bool enabled)
    {
        txtChatMessage.Enabled = enabled;
        btnSendChat.Enabled = enabled;
    }

    /// <summary>
    /// Hủy đăng ký sự kiện trước khi đóng form để service chat không giữ tham chiếu
    /// đến form và không gọi cập nhật control đã Dispose.
    /// </summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _adminChat.MessageReceived -= AdminChat_MessageReceived;
        base.OnFormClosed(e);
    }

    // Chuẩn hóa ID tại biên hệ thống: loại khoảng trắng và dùng UNKNOWN cho giá trị rỗng.
    private static string NormalizeMachineId(string machineId)
        => string.IsNullOrWhiteSpace(machineId) ? "UNKNOWN" : machineId.Trim();

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
            // Client protocol nhận cờ lockMachine; LOCK là true và UNLOCK là false.
            // ConfigureAwait(false) phù hợp vì phần ánh xạ sau đó không truy cập UI.
            MachineCommandSendResult commandResult = await networkServer.SendMachineCommandWithResultAsync(
                request.MachineId,
                lockMachine: request.Command == CommandType.LOCK,
                issuedBy: request.IssuedBy,
                reason: request.Reason,
                cancellationToken).ConfigureAwait(false);

            // Giữ RequestId ở cả hai nhánh để có thể đối chiếu request với phản hồi/log.
            return commandResult.Sent
                ? AdminCommandResult.Submitted(request, commandResult.Message, commandResult.RequestId)
                : AdminCommandResult.ControlledError(
                    request,
                    commandResult.ErrorCode ?? "COMMAND_SEND_FAILED",
                    commandResult.Message,
                    commandResult.RequestId);
        }
    }

    /// <summary>
    /// Nạp dữ liệu khách hàng minh họa vào bảng. Hàm luôn xóa bảng trước để việc gọi
    /// lại không tạo dòng trùng; dữ liệu này chưa được đọc từ repository.
    /// </summary>
    private void LoadSampleCustomerData()
    {
        // Xóa dữ liệu hiện có trước khi thêm lại bộ dữ liệu mẫu cố định.
        dgvCustomers.Rows.Clear();

        // Thứ tự giá trị của mỗi dòng phải khớp thứ tự cột khai báo trong Designer.
        dgvCustomers.Rows.Add(1, "Chi", "Nguyễn", "0128475621", "264493270", "16/04/1996", "Chi123", "123456", "10000");
        dgvCustomers.Rows.Add(2, "Thanh", "Nguyễn", "0902548345", "025351810", "12/12/1995", "Thanh123", "123456", "20000");
        dgvCustomers.Rows.Add(3, "Hà", "Trần", "012038950", "025351818", "03/02/1990", "Ha", "123456", "10000");
        dgvCustomers.Rows.Add(4, "Châu", "Trần", "0919512120", "025609999", "03/08/1990", "Chaubc", "123456", "5000");
        dgvCustomers.Rows.Add(5, "Linh", "Võ", "01212239011", "025607777", "30/04/1990", "PkLanh", "123456", "20000");
    }
}
