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
    private readonly ClientLaunchOptions _launchOptions;
    private TcpClientConnection? _connection;

    public ConnectForm(ClientLaunchOptions launchOptions)
    {
        _launchOptions = launchOptions ?? throw new ArgumentNullException(nameof(launchOptions));

        InitializeComponent();
        txtMachineId.Text = _launchOptions.MachineId;
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblMessage.ForeColor = Color.FromArgb(170, 45, 45);
        lblMessage.Text = string.Empty;

        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;
        string machineId = txtMachineId.Text.Trim();

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
            ShowValidationMessage("Vui lòng nhập mã máy.", txtMachineId);
            return;
        }

        lblMessage.ForeColor = SystemColors.ControlText;
        lblMessage.Text = $"Đang kết nối {_launchOptions.ServerHost}:{_launchOptions.ServerPort}...";
        btnLogin.Enabled = false;
        btnExit.Enabled = false;

        TcpClientConnection? pendingConnection = new();
        bool loginSucceeded = false;

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            ClientLoginAuthResult authResult = await SendLoginAsync(
                pendingConnection,
                username,
                password,
                machineId,
                timeout.Token);

            if (!authResult.IsSuccess)
            {
                ShowErrorMessage(CreateLoginFailureMessage(authResult));
                return;
            }

            _connection?.Dispose();
            _connection = pendingConnection;
            pendingConnection = null;
            loginSucceeded = true;

            ShowClientMainForm(authResult);
        }
        catch (OperationCanceledException)
        {
            ShowErrorMessage("Không nhận được phản hồi từ máy chủ. Vui lòng thử lại.");
        }
        catch (SocketException)
        {
            ShowErrorMessage("Không thể kết nối TCP. Hãy mở ServerApp trước và kiểm tra host/port.");
        }
        catch (IOException)
        {
            ShowErrorMessage("Kết nối TCP bị ngắt khi đăng nhập.");
        }
        catch (InvalidDataException ex)
        {
            ShowErrorMessage($"Phản hồi máy chủ không hợp lệ: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            pendingConnection?.Dispose();

            if (!loginSucceeded && !IsDisposed)
            {
                btnLogin.Enabled = true;
                btnExit.Enabled = true;
            }
        }
    }

    private void BtnExit_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void ShowValidationMessage(string message, Control focusTarget)
    {
        lblMessage.Text = message;
        focusTarget.Focus();
    }

    private async Task<ClientLoginAuthResult> SendLoginAsync(
        TcpClientConnection connection,
        string username,
        string password,
        string machineId,
        CancellationToken cancellationToken)
    {
        string requestId = Guid.NewGuid().ToString("N");
        var responseSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        void HandleMessage(string message)
        {
            try
            {
                object response = JsonHelper.DeserializePacket(message);

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
                responseSource.TrySetException(ex);
            }
        }

        connection.MessageReceived += HandleMessage;

        try
        {
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

            await connection.ConnectAsync(_launchOptions.ServerHost, _launchOptions.ServerPort, cancellationToken);
            await connection.SendAsync(JsonHelper.SerializeToJson(loginPacket), cancellationToken);

            object response = await responseSource.Task.WaitAsync(cancellationToken);

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
            connection.MessageReceived -= HandleMessage;
        }
    }

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

    private void ShowErrorMessage(string message)
    {
        lblMessage.ForeColor = Color.FromArgb(170, 45, 45);
        lblMessage.Text = message;
    }

    private void ShowClientMainForm(ClientLoginAuthResult authResult)
    {
        Hide();

        using var mainForm = new ClientMainForm(
            authResult.Username,
            authResult.MachineId,
            _launchOptions.ServerHost,
            _launchOptions.ServerPort,
            authResult.SessionId ?? string.Empty,
            authResult.LoginTimeUtc);

        mainForm.ShowDialog(this);
        Close();
    }

    private sealed class ClientLoginAuthResult
    {
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

        public bool IsSuccess { get; }

        public string? SessionId { get; }

        public string Username { get; }

        public string MachineId { get; }

        public DateTime LoginTimeUtc { get; }

        public string? ErrorCode { get; }

        public string? ErrorMessage { get; }

        public static ClientLoginAuthResult FromSuccess(
            Packet<LoginResultPayload> packet,
            string requestedUsername,
            string requestedMachineId)
        {
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

        private static string UseServerValueOrFallback(string? serverValue, string fallback)
        {
            return string.IsNullOrWhiteSpace(serverValue)
                ? fallback.Trim()
                : serverValue.Trim();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _connection?.Dispose();
        base.OnFormClosed(e);
    }
}
