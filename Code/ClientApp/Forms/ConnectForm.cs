using System.IO;
using System.Net.Sockets;
using ClientApp;
using ClientApp.Networking;
using Shared.DTOs.RequestPayloads;
using Shared.DTOs.ResponsePayloads;
using Shared.Networking;
using Shared.Packets;
using Shared.Utilities.JsonHelper;

namespace ClientApp.Forms;

public partial class ConnectForm : Form
{
    private const string MachinePrefix = "PC";

    // Cấu hình khởi chạy chứa host/port server và MachineId mặc định từ Program.
    private readonly ClientLaunchOptions _launchOptions;

    // Kết nối chỉ được gán vào field sau khi đăng nhập thành công. Trước thời điểm đó,
    // kết nối tạm được giữ trong biến local để dễ Dispose khi có lỗi.
    private TcpClientConnection? _connection;

    // Dựng control từ Designer. Mã máy để trống để người dùng tự nhập đúng PC cần chạy.
    public ConnectForm(ClientLaunchOptions launchOptions)
    {
        // Form không thể hoạt động nếu thiếu endpoint đầu vào.
        _launchOptions = launchOptions ?? throw new ArgumentNullException(nameof(launchOptions));

        InitializeComponent();
        txtMachineId.Text = MachinePrefix;
        txtMachineId.SelectionStart = txtMachineId.TextLength;
        txtMachineId.TextChanged += TxtMachineId_TextChanged;
        txtMachineId.Leave += TxtMachineId_Leave;
        txtMachineId.ReadOnly = false;
        txtMachineId.TabStop = true;
        txtMachineId.BackColor = SystemColors.Window;
        txtMachineId.PlaceholderText = "Nhap so may, VD: 01 -> PC01";
        Text = "Client login";
        lblTitle.Text = "CLIENT LOGIN";
    }

    // Kiểm tra dữ liệu đầu vào, mở TCP, gửi LOGIN và chỉ chuyển sang ClientMainForm
    // khi server trả về kết quả xác thực thành công.
    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        // Đặt lại vùng thông báo về màu lỗi mặc định và xóa nội dung của lần thử trước.
        lblMessage.ForeColor = Color.FromArgb(170, 45, 45);
        lblMessage.Text = string.Empty;

        // Username/MachineId được trim; password giữ nguyên vì khoảng trắng có thể là
        // một phần hợp lệ của mật khẩu.
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;
        string machineId = NormalizeMachineId(txtMachineId.Text);
        txtMachineId.Text = string.IsNullOrWhiteSpace(machineId) ? MachinePrefix : machineId;
        txtMachineId.SelectionStart = txtMachineId.TextLength;

        // Dừng tại trường sai đầu tiên và đưa focus về đúng control để người dùng sửa.
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowValidationMessage("Vui lòng nhập tên tài khoản.", txtUsername);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowValidationMessage("Vui lòng nhập mật khẩu.", txtPassword);
            return;
        }

        if (string.IsNullOrWhiteSpace(machineId))
        {
            ShowValidationMessage("Vui lòng nhập mã máy theo dạng số sau PC, ví dụ PC01.", txtMachineId);
            return;
        }

        // Chuyển sang trạng thái đang xử lý và khóa nút để tránh nhiều request LOGIN
        // chạy song song trên các kết nối khác nhau.
        lblMessage.ForeColor = SystemColors.ControlText;
        lblMessage.Text = $"Đang kết nối {_launchOptions.ServerHost}:{_launchOptions.ServerPort}...";
        btnLogin.Enabled = false;
        btnExit.Enabled = false;

        // pendingConnection chỉ trở thành _connection khi xác thực thành công. Cờ
        // loginSucceeded quyết định có mở lại nút ở finally hay không.
        TcpClientConnection? pendingConnection = new();
        bool loginSucceeded = false;

        try
        {
            // Toàn bộ connect/send/wait response có giới hạn 10 giây để UI không chờ vô hạn.
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            ClientLoginAuthResult authResult = await SendLoginAsync(
                pendingConnection,
                username,
                password,
                machineId,
                timeout.Token);

            // Lỗi nghiệp vụ được server trả về dưới dạng result, không phải exception.
            if (!authResult.IsSuccess)
            {
                ShowErrorMessage(CreateLoginFailureMessage(authResult));
                return;
            }

            // Thay kết nối cũ nếu có, chuyển quyền sở hữu kết nối tạm sang field và bật
            // tự reconnect cho giai đoạn chạy ClientMainForm.
            _connection?.Dispose();
            _connection = pendingConnection;
            _connection.EnableAutoReconnect();
            pendingConnection = null;
            loginSucceeded = true;

            // Mở form phiên làm việc bằng thông tin server đã xác nhận.
            ShowClientMainForm(authResult);
        }
        // Timeout hoặc cancellation đều được diễn giải thành server không phản hồi.
        catch (OperationCanceledException)
        {
            ShowErrorMessage("Không nhận được phản hồi từ máy chủ. Vui lòng thử lại.");
        }
        // Lỗi ở bước tạo/kết nối socket thường do server chưa chạy hoặc sai endpoint.
        catch (SocketException)
        {
            ShowErrorMessage("Không thể kết nối TCP. Hãy mở ServerApp trước và kiểm tra host/port.");
        }
        // Socket đã mở nhưng luồng dữ liệu bị ngắt trong quá trình đăng nhập.
        catch (IOException)
        {
            ShowErrorMessage("Kết nối TCP bị ngắt khi đăng nhập.");
        }
        // Packet nhận được nhưng cấu trúc/nội dung không đúng contract LOGIN.
        catch (InvalidDataException ex)
        {
            ShowErrorMessage($"Phản hồi máy chủ không hợp lệ: {ex.Message}");
        }
        // Trạng thái/protocol hợp lệ về cú pháp nhưng không phù hợp với luồng LOGIN.
        catch (InvalidOperationException ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            // Sau thành công pendingConnection đã được đặt null, nên chỉ kết nối thất
            // bại mới bị Dispose tại đây.
            pendingConnection?.Dispose();

            // Nếu chưa chuyển sang form chính và form đăng nhập vẫn sống, mở lại nút.
            if (!loginSucceeded && !IsDisposed)
            {
                btnLogin.Enabled = true;
                btnExit.Enabled = true;
            }
        }
    }

    // Đóng form đăng nhập; OnFormClosed chịu trách nhiệm dừng và Dispose kết nối nếu có.
    private void BtnExit_Click(object? sender, EventArgs e)
    {
        Close();
    }

    // Hiển thị lỗi nhập liệu và chuyển focus tới trường cần sửa.
    private void ShowValidationMessage(string message, Control focusTarget)
    {
        lblMessage.Text = message;
        focusTarget.Focus();
    }

    // Giữ phần đầu "PC" cố định khi người dùng đang gõ, nhưng chưa padding số
    // ngay lập tức để caret không bị nhảy khó chịu.
    private void TxtMachineId_TextChanged(object? sender, EventArgs e)
    {
        if (sender is not TextBox textBox || textBox != txtMachineId)
        {
            return;
        }

        int digitsBeforeCaret = textBox.Text
            .Take(Math.Min(textBox.SelectionStart, textBox.TextLength))
            .Count(char.IsDigit);

        string sanitized = SanitizeMachineIdInput(textBox.Text);
        if (string.Equals(textBox.Text, sanitized, StringComparison.Ordinal))
        {
            return;
        }

        textBox.TextChanged -= TxtMachineId_TextChanged;
        textBox.Text = sanitized;
        textBox.SelectionStart = Math.Min(MachinePrefix.Length + digitsBeforeCaret, textBox.TextLength);
        textBox.TextChanged += TxtMachineId_TextChanged;
    }

    private void TxtMachineId_Leave(object? sender, EventArgs e)
    {
        string normalized = NormalizeMachineId(txtMachineId.Text);
        txtMachineId.Text = string.IsNullOrWhiteSpace(normalized) ? MachinePrefix : normalized;
        txtMachineId.SelectionStart = txtMachineId.TextLength;
    }

    // Trong lúc nhập chỉ cho phép dạng PC + chữ số. Người dùng có thể gõ "1",
    // "01", "PC1" hoặc paste "PC01"; control sẽ hiển thị phần hợp lệ.
    private static string SanitizeMachineIdInput(string? rawMachineId)
    {
        if (string.IsNullOrWhiteSpace(rawMachineId))
        {
            return MachinePrefix;
        }

        string trimmed = rawMachineId.Trim();
        string suffix = trimmed.StartsWith(MachinePrefix, StringComparison.OrdinalIgnoreCase)
            ? trimmed[MachinePrefix.Length..]
            : trimmed;

        string digits = new(suffix.Where(char.IsDigit).ToArray());
        return $"{MachinePrefix}{digits}";
    }

    // Chấp nhận input dạng "01", "1", "PC1" hoặc "PC01" và luôn trả ra "PC01".
    private static string NormalizeMachineId(string? rawMachineId)
    {
        if (string.IsNullOrWhiteSpace(rawMachineId))
        {
            return string.Empty;
        }

        string trimmed = rawMachineId.Trim();
        string suffix = trimmed.StartsWith(MachinePrefix, StringComparison.OrdinalIgnoreCase)
            ? trimmed[MachinePrefix.Length..]
            : trimmed;

        string digits = new string(suffix.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return string.Empty;
        }

        if (!int.TryParse(digits, out int machineNumber))
        {
            return string.Empty;
        }

        return $"{MachinePrefix}{machineNumber:D2}";
    }

    // Thực hiện một request/response LOGIN có correlation bằng RequestId. Handler tạm
    // chỉ nhận hai loại packet phản hồi LOGIN và được gỡ ngay khi request kết thúc.
    private async Task<ClientLoginAuthResult> SendLoginAsync(
        TcpClientConnection connection,
        string username,
        string password,
        string machineId,
        CancellationToken cancellationToken)
    {
        // RequestId duy nhất ghép phản hồi đúng với lần đăng nhập này. TCS chuyển callback
        // MessageReceived thành Task có thể await trong luồng xử lý hiện tại.
        string requestId = Guid.NewGuid().ToString("N");
        var responseSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Local handler deserialize từng JSON line và chỉ hoàn tất TCS khi RequestId khớp.
        void HandleMessage(string message)
        {
            try
            {
                object response = JsonHelper.DeserializePacket(message);

                // LOGIN thành công có payload phiên; LOGIN thất bại dùng EmptyPayload
                // kèm Error. Packet không liên quan được bỏ qua để handler khác xử lý.
                if (response is Packet<LoginResultPayload> successPacket
                    && string.Equals(successPacket.RequestId, requestId, StringComparison.Ordinal))
                {
                    responseSource.TrySetResult(successPacket);
                }
                else if (response is Packet<EmptyPayload> failurePacket
                    && string.Equals(failurePacket.RequestId, requestId, StringComparison.Ordinal))
                {
                    responseSource.TrySetResult(failurePacket);
                }
            }
            catch (Exception ex)
            {
                // Lỗi deserialize/protocol được chuyển sang Task chờ phía dưới.
                responseSource.TrySetException(ex);
            }
        }

        // Đăng ký trước khi gửi để không bỏ lỡ phản hồi rất nhanh từ server.
        connection.MessageReceived += HandleMessage;

        try
        {
            // Packet LOGIN mang role Client, thông tin xác thực và cùng RequestId đã
            // dùng để lọc phản hồi.
            var loginPacket = PacketFactory.CreateLogin(
                source: machineId,
                target: NetworkProtocol.ServerSource,
                payload: new LoginPayload
                {
                    Username = username,
                    Password = password,
                    Role = "Client",
                    MachineId = machineId
                },
                requestId: requestId);

            // Kết nối trước, gửi JSON line sau, rồi chờ TCS hoặc cancellation timeout.
            await connection.ConnectAsync(_launchOptions.ServerHost, _launchOptions.ServerPort, cancellationToken);
            await connection.SendAsync(JsonHelper.SerializeToJson(loginPacket), cancellationToken);

            object response = await responseSource.Task.WaitAsync(cancellationToken);

            // Chuyển packet wire-level thành model kết quả đơn giản cho tầng UI.
            return response switch
            {
                Packet<LoginResultPayload> successPacket when successPacket.Success == true
                    => ClientLoginAuthResult.FromSuccess(successPacket, username, machineId),
                Packet<EmptyPayload> failurePacket
                    => ClientLoginAuthResult.FromFailure(failurePacket),
                Packet<LoginResultPayload> unexpectedPacket => throw new InvalidOperationException(unexpectedPacket.Message ?? "Máy chủ từ chối đăng nhập."),
                _ => throw new InvalidDataException("Unexpected LOGIN response packet.")
            };
        }
        finally
        {
            // Luôn gỡ local handler để các lần LOGIN sau không tích lũy subscriber và
            // packet runtime không bị handler đăng nhập cũ xử lý.
            connection.MessageReceived -= HandleMessage;
        }
    }

    // Ánh xạ mã lỗi ổn định từ server sang thông báo thân thiện. Khi không nhận diện
    // mã lỗi, ưu tiên chi tiết server rồi mới dùng thông báo mặc định.
    private static string CreateLoginFailureMessage(ClientLoginAuthResult authResult)
    {
        return authResult.ErrorCode switch
        {
            "INVALID_CREDENTIALS" => "Sai tài khoản hoặc mật khẩu.",
            "INVALID_MACHINE_ID" => "Mã máy không hợp lệ hoặc máy chưa được đăng ký.",
            "ACCOUNT_MACHINE_MISMATCH" => "Tài khoản này không được gán cho máy đang chọn.",
            "ACCOUNT_DISABLED" => "Tài khoản hoặc máy trạm đã bị vô hiệu hóa.",
            "MACHINE_ALREADY_ACTIVE" => "Máy này đang có phiên đăng nhập khác.",
            "INVALID_PACKET" => authResult.ErrorMessage ?? "Gói đăng nhập không hợp lệ.",
            "SERVER_ERROR" => "Máy chủ không thể xác thực lúc này. Vui lòng thử lại.",
            _ => authResult.ErrorMessage
                ?? "Máy chủ từ chối đăng nhập."
        };
    }

    // Chuẩn hóa cách hiển thị lỗi nghiệp vụ và lỗi kỹ thuật trên cùng label.
    private void ShowErrorMessage(string message)
    {
        lblMessage.ForeColor = Color.FromArgb(170, 45, 45);
        lblMessage.Text = message;
    }

    // Ẩn form kết nối, chạy ClientMainForm theo modal và chỉ đóng ConnectForm sau khi
    // phiên client kết thúc. Kết nối TCP được truyền tiếp, không tạo socket mới.
    private void ShowClientMainForm(ClientLoginAuthResult authResult)
    {
        Hide();

        // using bảo đảm ClientMainForm được Dispose sau ShowDialog.
        using var mainForm = new ClientMainForm(
            authResult.Username,
            authResult.MachineId,
            _launchOptions.ServerHost,
            _launchOptions.ServerPort,
            authResult.SessionId ?? string.Empty,
            authResult.LoginTimeUtc,
            _connection ?? throw new InvalidOperationException("Client connection is not available."));

        mainForm.ShowDialog(this);
        Close();
    }

    // Model nội bộ gom dữ liệu của cả phản hồi thành công và thất bại, giúp code UI
    // không phụ thuộc trực tiếp vào nhiều kiểu Packet khác nhau.
    private sealed class ClientLoginAuthResult
    {
        // Constructor private buộc caller tạo model qua FromSuccess/FromFailure để
        // mọi invariant của kết quả được kiểm tra tập trung.
        private ClientLoginAuthResult(
            bool isSuccess,
            string? sessionId,
            string username,
            string machineId,
            DateTime loginTimeUtc,
            string? errorCode,
            string? errorMessage)
        {
            IsSuccess = isSuccess;
            SessionId = sessionId;
            Username = username;
            MachineId = machineId;
            LoginTimeUtc = loginTimeUtc;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        // Các property chỉ đọc vì kết quả xác thực không thay đổi sau khi được tạo.
        public bool IsSuccess { get; }

        public string? SessionId { get; }

        public string Username { get; }

        public string MachineId { get; }

        public DateTime LoginTimeUtc { get; }

        public string? ErrorCode { get; }

        public string? ErrorMessage { get; }

        // Tạo kết quả thành công từ payload server và kiểm tra SessionId bắt buộc.
        public static ClientLoginAuthResult FromSuccess(
            Packet<LoginResultPayload> packet,
            string requestedUsername,
            string requestedMachineId)
        {
            // Success packet thiếu payload/session không đủ dữ liệu mở phiên nên được
            // xem là lỗi protocol thay vì đăng nhập thành công một phần.
            LoginResultPayload payload = packet.TypedPayload
                ?? throw new InvalidDataException("LOGIN success response is missing payload.");

            if (string.IsNullOrWhiteSpace(payload.SessionId))
            {
                throw new InvalidDataException("LOGIN success response is missing session id.");
            }

            return new ClientLoginAuthResult(
                isSuccess: true,
                sessionId: payload.SessionId.Trim(),
                username: UseServerValueOrFallback(payload.Username, requestedUsername),
                machineId: UseServerValueOrFallback(payload.MachineId, requestedMachineId),
                loginTimeUtc: packet.Timestamp,
                errorCode: null,
                errorMessage: null);
        }

        // Tạo kết quả thất bại từ Error metadata; các trường phiên để rỗng vì chưa
        // có session hợp lệ.
        public static ClientLoginAuthResult FromFailure(Packet<EmptyPayload> packet)
        {
            return new ClientLoginAuthResult(
                isSuccess: false,
                sessionId: null,
                username: string.Empty,
                machineId: string.Empty,
                loginTimeUtc: DateTime.MinValue,
                errorCode: packet.Error?.Code,
                errorMessage: packet.Error?.Details ?? packet.Message);
        }

        // Tin giá trị đã chuẩn hóa từ server khi có; fallback về dữ liệu request nếu
        // server không gửi lại username hoặc MachineId.
        private static string UseServerValueOrFallback(string? serverValue, string fallback)
        {
            return string.IsNullOrWhiteSpace(serverValue)
                ? fallback.Trim()
                : serverValue.Trim();
        }
    }

    // Dừng auto reconnect và giải phóng socket khi form kết nối kết thúc vòng đời.
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _connection?.DisableAutoReconnect();
        _connection?.Dispose();
        base.OnFormClosed(e);
    }
}
