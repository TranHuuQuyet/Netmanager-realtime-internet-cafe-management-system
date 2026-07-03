using ServerApp.Auth.Contracts;
using ServerApp.Auth.Models;

namespace ServerApp;

public partial class LoginForm : Form
{
    private const string MachinePrefix = "PC";

    // Dịch vụ xác thực được khởi tạo bất đồng bộ từ Program. Form giữ lại Task
    // thay vì chặn luồng giao diện, nhờ đó cửa sổ vẫn có thể được dựng ngay.
    private readonly Task<IAuthService> _authServiceTask;

    /// <summary>
    /// Nhận tiến trình khởi tạo dịch vụ xác thực và cấu hình các sự kiện của form.
    /// </summary>
    public LoginForm(Task<IAuthService> authServiceTask)
    {
        // Không cho phép tạo form nếu không có nguồn cung cấp dịch vụ xác thực.
        _authServiceTask = authServiceTask ?? throw new ArgumentNullException(nameof(authServiceTask));

        // Tạo toàn bộ control được khai báo trong LoginForm.Designer.cs.
        InitializeComponent();
        txtMachineId.Text = MachinePrefix;
        txtMachineId.SelectionStart = txtMachineId.TextLength;
        txtMachineId.TextChanged += TxtMachineId_TextChanged;
        txtMachineId.Leave += TxtMachineId_Leave;
        txtMachineId.PlaceholderText = "Nhap so may, VD: 00 -> PC00";

        // Nút đăng nhập không tự đóng form. Chỉ nhánh xác thực thành công bên dưới
        // mới gán DialogResult.OK và đóng cửa sổ.
        btnLogin.DialogResult = DialogResult.None;

        // Khi form đã hiển thị, kiểm tra việc chuẩn bị dữ liệu xác thực mà không
        // làm chậm constructor hoặc khóa UI thread.
        Shown += LoginForm_Shown;
    }

    /// <summary>
    /// Chờ dịch vụ xác thực khởi tạo xong ngay sau khi form xuất hiện.
    /// Nếu khởi tạo thất bại thì khóa nút đăng nhập vì mọi lần đăng nhập sau đó
    /// đều không thể thực hiện được.
    /// </summary>
    private async void LoginForm_Shown(object? sender, EventArgs e)
    {
        try
        {
            await _authServiceTask;
        }
        catch (Exception)
        {
            lblMessage.Text = "Authentication data could not be initialized.";
            btnLogin.Enabled = false;
        }
    }

    /// <summary>
    /// Xử lý toàn bộ quy trình khi người dùng bấm Đăng nhập: kiểm tra dữ liệu,
    /// gọi dịch vụ xác thực và trả kết quả về form đã mở LoginForm.
    /// </summary>
    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        // Xóa lỗi cũ trước khi bắt đầu một lần kiểm tra mới.
        lblMessage.Text = string.Empty;

        // Kiểm tra lần lượt để người dùng chỉ phải sửa trường đầu tiên đang sai.
        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            ShowValidationMessage(UiStrings.LoginUsernameRequired, txtUsername);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            ShowValidationMessage(UiStrings.LoginPasswordRequired, txtPassword);
            return;
        }

        string machineId = NormalizeMachineId(txtMachineId.Text);
        txtMachineId.Text = string.IsNullOrWhiteSpace(machineId) ? MachinePrefix : machineId;
        txtMachineId.SelectionStart = txtMachineId.TextLength;

        if (string.IsNullOrWhiteSpace(machineId))
        {
            ShowValidationMessage(UiStrings.LoginMachineIdRequired, txtMachineId);
            return;
        }

        // Ngăn double-click tạo nhiều yêu cầu xác thực chạy song song.
        btnLogin.Enabled = false;

        try
        {
            // Task có thể đã hoàn tất từ sự kiện Shown; await vẫn trả dịch vụ ngay
            // và không khóa luồng giao diện.
            IAuthService authService = await _authServiceTask;

            // Máy chủ luôn đăng nhập với vai trò Admin. Tên đăng nhập, mật khẩu và
            // mã máy được đóng gói thành AuthRequest để tầng giao diện không tự xử
            // lý quy tắc xác thực.
            AuthResult result = await authService.AuthenticateAsync(
                new AuthRequest(
                    txtUsername.Text,
                    txtPassword.Text,
                    machineId,
                    UserRole.Admin));

            // Dịch vụ trả về lỗi nghiệp vụ (sai tài khoản, sai mật khẩu...) dưới
            // dạng AuthResult, vì vậy chỉ hiển thị thông báo và giữ form mở.
            if (!result.IsSuccess)
            {
                lblMessage.Text = CreateLoginFailureMessage(result);
                return;
            }

            // DialogResult.OK báo cho Program biết đăng nhập thành công để tiếp tục
            // mở MainForm. Close kết thúc vòng đời của hộp thoại đăng nhập.
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception)
        {
            // Đây là lỗi kỹ thuật ngoài kết quả nghiệp vụ, ví dụ không đọc được dữ
            // liệu xác thực. Không đưa chi tiết exception nhạy cảm lên giao diện.
            lblMessage.Text = "Authentication could not be completed.";
        }
        finally
        {
            // Luôn mở lại nút sau khi yêu cầu kết thúc. Nếu form đã Close thì thao
            // tác này không ảnh hưởng tới kết quả DialogResult.OK.
            btnLogin.Enabled = true;
        }
    }

    /// <summary>
    /// Hiển thị lỗi kiểm tra đầu vào và đưa con trỏ về đúng control cần sửa.
    /// </summary>
    private void ShowValidationMessage(string message, Control focusTarget)
    {
        lblMessage.Text = message;
        focusTarget.Focus();
    }

    private static string CreateLoginFailureMessage(AuthResult result)
    {
        return result.ErrorCode switch
        {
            "INVALID_MACHINE_ID" => "Mã máy không tồn tại trong hệ thống.",
            "INVALID_CREDENTIALS" => "Sai tài khoản, mật khẩu hoặc quyền đăng nhập.",
            "ACCOUNT_MACHINE_MISMATCH" => "Tài khoản này không được gán cho mã máy đang chọn.",
            "ACCOUNT_DISABLED" => "Tài khoản hoặc máy đã bị vô hiệu hóa.",
            "MACHINE_ALREADY_ACTIVE" => "Máy này đang có phiên đăng nhập khác.",
            _ => result.Message
        };
    }

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

        string digits = new(suffix.Where(char.IsDigit).ToArray());
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
}
