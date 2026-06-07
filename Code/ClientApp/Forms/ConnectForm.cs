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
            Packet<LoginResultPayload> response = await SendLoginAsync(
                pendingConnection,
                username,
                password,
                machineId,
                timeout.Token);

            _connection?.Dispose();
            _connection = pendingConnection;
            pendingConnection = null;
            loginSucceeded = true;

            ShowClientMainForm(response);
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

    private async Task<Packet<LoginResultPayload>> SendLoginAsync(
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
                Packet<LoginResultPayload> successPacket when successPacket.Success == true => successPacket,
                Packet<EmptyPayload> failurePacket => throw new InvalidOperationException(CreateLoginFailureMessage(failurePacket)),
                Packet<LoginResultPayload> unexpectedPacket => throw new InvalidOperationException(unexpectedPacket.Message ?? "Máy chủ từ chối đăng nhập."),
                _ => throw new InvalidDataException("Unexpected LOGIN response packet.")
            };
        }
        finally
        {
            connection.MessageReceived -= HandleMessage;
        }
    }

    private static string CreateLoginFailureMessage(Packet<EmptyPayload> failurePacket)
    {
        return failurePacket.Error?.Code switch
        {
            "INVALID_CREDENTIALS" => "Sai tài khoản hoặc mật khẩu.",
            "ACCOUNT_MACHINE_MISMATCH" => "Tài khoản này không được gán cho máy đang chọn.",
            "MACHINE_ALREADY_ACTIVE" => "Máy này đang có phiên đăng nhập khác.",
            "INVALID_PACKET" => failurePacket.Error.Details ?? "Gói đăng nhập không hợp lệ.",
            _ => failurePacket.Error?.Details
                ?? failurePacket.Message
                ?? "Máy chủ từ chối đăng nhập."
        };
    }

    private void ShowErrorMessage(string message)
    {
        lblMessage.ForeColor = Color.FromArgb(170, 45, 45);
        lblMessage.Text = message;
    }

    private void ShowClientMainForm(Packet<LoginResultPayload> loginResponse)
    {
        Hide();
        LoginResultPayload login = loginResponse.TypedPayload;

        using var mainForm = new ClientMainForm(
            login.Username,
            login.MachineId,
            _launchOptions.ServerHost,
            _launchOptions.ServerPort,
            login.SessionId,
            loginResponse.Timestamp);

        mainForm.ShowDialog(this);
        Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _connection?.Dispose();
        base.OnFormClosed(e);
    }
}
