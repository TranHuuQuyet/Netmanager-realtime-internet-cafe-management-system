using ServerApp.Database.Contracts;
using ServerApp.Database.Entities;
using ServerApp.Networking;
using ServerApp.Presentation;
using Shared.Enums;

namespace ServerApp;

public partial class MainForm : Form
{
    private const string SampleMachinePrefix = "PC";
    private const string AdminCommandIssuer = "ServerApp.MainForm";

    private readonly IMachineRepository? _machines;
    private readonly IAdminCommandService _adminCommands;
    private bool _isSelectingMachine;
    private bool _isRuntimeMachineDataActive;
    private string? _selectedMachineName;

    public MainForm()
        : this(null, (IAdminCommandService?)null)
    {
    }

    public MainForm(IMachineRepository? machines)
        : this(machines, (IAdminCommandService?)null)
    {
    }

    public MainForm(IMachineRepository? machines, IAdminCommandService? adminCommands)
    {
        _machines = machines;
        _adminCommands = adminCommands ?? new UnavailableAdminCommandService();
        InitializeComponent();
        ConfigureR1ShellState();
    }

    public MainForm(IMachineRepository? machines, TcpJsonLineServer? networkServer)
        : this(
            machines,
            networkServer is null
                ? null
                : new NetworkAdminCommandService(networkServer))
    {
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        LoadSampleCustomerData();

        if (!await TryLoadRuntimeMachineDataAsync())
        {
            LoadSampleMachineData();
            SelectMachine("PC01");
            lblServerStatus.Text = UiStrings.MainServerStatus;
        }
    }

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
    }

    private void LoadSampleMachineData()
    {
        _isRuntimeMachineDataActive = false;
        dgvMachines.Rows.Clear();
        pnlMachineCards.Controls.Clear();

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

            dgvMachines.Rows.Add(machineNumber, machineNumber, statuses[index], machineName);
            pnlMachineCards.Controls.Add(CreateMachineCard(machineName, statuses[index]));
        }
    }

    private async Task<bool> TryLoadRuntimeMachineDataAsync()
    {
        if (_machines is null)
        {
            return false;
        }

        try
        {
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

    private void LoadRuntimeMachineData(IReadOnlyList<MachineEntity> machines)
    {
        EnsureRuntimeMachineDataActive();

        foreach (MachineEntity machine in machines)
        {
            string machineName = NormalizeMachineId(machine.MachineId);
            string status = NormalizeStatus(machine.Status);

            UpsertMachineRow(machineName, status);
            UpsertMachineCard(machineName, status);
        }

        string firstMachineName = NormalizeMachineId(machines[0].MachineId);
        SelectMachine(firstMachineName);
        lblServerStatus.Text = string.Format(UiStrings.MainSelectedMachineStatusTemplate, firstMachineName);
    }

    public void ApplyMachineStatusUpdate(string machineId, string status)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyMachineStatusUpdate(machineId, status));
            return;
        }

        string normalizedMachineId = NormalizeMachineId(machineId);
        string normalizedStatus = NormalizeStatus(status);

        EnsureRuntimeMachineDataActive();
        UpsertMachineRow(normalizedMachineId, normalizedStatus);
        UpsertMachineCard(normalizedMachineId, normalizedStatus);
        SelectMachine(normalizedMachineId);

        lblServerStatus.Text = string.Format(
            UiStrings.MainRuntimeStatusUpdatedTemplate,
            normalizedMachineId,
            normalizedStatus);
    }

    public void ApplyCommandResultUpdate(AdminCommandResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyCommandResultUpdate(result));
            return;
        }

        lblServerStatus.Text = FormatCommandResult(result);
    }

    private Panel CreateMachineCard(string machineName, string status)
    {
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

        var icon = new PictureBox
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.White,
            Cursor = Cursors.Hand,
            Tag = status
        };
        icon.Paint += MachineIcon_Paint;

        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = FormatMachineLabel(machineName, status),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Tag = machineName
        };

        card.Controls.Add(label);
        card.Controls.Add(icon);
        card.Click += MachineCard_Click;
        icon.Click += MachineCard_Click;
        label.Click += MachineCard_Click;

        return card;
    }

    private void MachineIcon_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not PictureBox icon)
        {
            return;
        }

        string status = icon.Tag?.ToString() ?? "AVAILABLE";
        Color statusColor = status switch
        {
            "ONLINE" => Color.FromArgb(31, 122, 58),
            "OFFLINE" => Color.FromArgb(170, 45, 45),
            "DISCONNECT" => Color.FromArgb(170, 45, 45),
            _ => Color.FromArgb(120, 120, 120)
        };

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var blackBrush = new SolidBrush(Color.Black);
        using var whiteBrush = new SolidBrush(Color.White);
        using var statusBrush = new SolidBrush(statusColor);
        using var pen = new Pen(Color.Black, 3F);

        Rectangle monitor = new(22, 8, 72, 44);
        e.Graphics.FillRectangle(blackBrush, monitor);
        e.Graphics.DrawRectangle(pen, monitor);
        e.Graphics.FillRectangle(whiteBrush, 29, 15, 58, 29);
        e.Graphics.FillEllipse(whiteBrush, 54, 50, 8, 8);
        e.Graphics.FillRectangle(blackBrush, 53, 58, 10, 8);
        e.Graphics.FillRectangle(blackBrush, 40, 66, 36, 5);
        e.Graphics.FillEllipse(statusBrush, 83, 8, 12, 12);
    }

    private void MachineCard_Click(object? sender, EventArgs e)
    {
        string? machineName = GetMachineNameFromCardSender(sender);

        if (!string.IsNullOrWhiteSpace(machineName))
        {
            SelectMachine(machineName);
        }
    }

    private void DgvMachines_SelectionChanged(object? sender, EventArgs e)
    {
        if (_isSelectingMachine)
        {
            return;
        }

        if (dgvMachines.CurrentRow?.Cells["MachineNameColumn"].Value is string machineName)
        {
            SelectMachine(machineName);
        }
    }

    private void SelectMachine(string machineName)
    {
        if (string.IsNullOrWhiteSpace(machineName))
        {
            return;
        }

        _isSelectingMachine = true;

        try
        {
            _selectedMachineName = machineName;
            lblSelectedClient.Text = string.Format(UiStrings.ChatWithMachineTemplate, machineName);
            txtChatHistory.Text = string.Format(UiStrings.ChatHistoryTemplate, machineName);
            lblServerStatus.Text = string.Format(UiStrings.MainSelectedMachineStatusTemplate, machineName);

            foreach (DataGridViewRow row in dgvMachines.Rows)
            {
                bool isSelected = row.Cells["MachineNameColumn"].Value?.ToString() == machineName;
                row.Selected = isSelected;

                if (isSelected)
                {
                    dgvMachines.CurrentCell = row.Cells[0];
                }
            }

            UpdateMachineCardSelection(machineName);
        }
        finally
        {
            _isSelectingMachine = false;
        }
    }

    private void BtnSendChat_Click(object? sender, EventArgs e)
    {
        string message = txtChatMessage.Text.Trim();

        if (message.Length == 0)
        {
            return;
        }

        txtChatHistory.AppendText($"{Environment.NewLine}{UiStrings.ServerPrefix}: {message}");
        txtChatMessage.Clear();
        txtChatMessage.Focus();
    }

    private async void MachineAction_Click(object? sender, EventArgs e)
    {
        CommandType? command = sender switch
        {
            Button button when button == btnLockMachine => CommandType.LOCK,
            Button button when button == btnUnlockMachine => CommandType.UNLOCK,
            _ => null
        };

        string action = command switch
        {
            CommandType.LOCK => UiStrings.MainLockMachine,
            CommandType.UNLOCK => UiStrings.MainUnlockMachine,
            _ when sender == btnShutdownMachine => UiStrings.MainShutdownMachine,
            _ => UiStrings.MainPendingAction
        };

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

        await SendMachineCommandAsync(command.Value, action, _selectedMachineName);
    }

    private async Task SendMachineCommandAsync(CommandType command, string action, string machineName)
    {
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
            AdminCommandResult result = await _adminCommands.SendAsync(request);
            ApplyCommandResultUpdate(result);
        }
        catch (Exception ex)
        {
            ApplyCommandResultUpdate(AdminCommandResult.ControlledError(
                request,
                "COMMAND_SERVICE_ERROR",
                ex.Message));
        }
        finally
        {
            SetMachineActionButtonsEnabled(_isRuntimeMachineDataActive);
        }
    }

    private void CustomerAction_Click(object? sender, EventArgs e)
    {
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

    private void UpdateMachineCardSelection(string selectedMachineName)
    {
        foreach (Control control in pnlMachineCards.Controls)
        {
            if (control is not Panel card || card.Tag is not string machineName)
            {
                continue;
            }

            bool isSelected = machineName == selectedMachineName;
            card.BackColor = isSelected ? Color.FromArgb(232, 244, 255) : Color.White;
            card.BorderStyle = isSelected ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;

            foreach (Control child in card.Controls)
            {
                if (child is not PictureBox)
                {
                    child.BackColor = card.BackColor;
                }
            }
        }
    }

    private void EnsureRuntimeMachineDataActive()
    {
        if (_isRuntimeMachineDataActive)
        {
            return;
        }

        _isRuntimeMachineDataActive = true;
        lblMachineTitle.Text = UiStrings.MainMachineTitle;
        _selectedMachineName = null;
        SetMachineActionButtonsEnabled(true);
        btnLockMachine.Text = UiStrings.MainLockMachine;
        btnUnlockMachine.Text = UiStrings.MainUnlockMachine;

        bool previousSelectingState = _isSelectingMachine;
        _isSelectingMachine = true;

        try
        {
            dgvMachines.Rows.Clear();
            pnlMachineCards.Controls.Clear();
        }
        finally
        {
            _isSelectingMachine = previousSelectingState;
        }
    }

    private void UpsertMachineRow(string machineName, string status)
    {
        foreach (DataGridViewRow row in dgvMachines.Rows)
        {
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

        int machineNumber = TryGetMachineNumber(machineName);
        dgvMachines.Rows.Add(machineNumber, machineNumber, status, machineName);
    }

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

        pnlMachineCards.Controls.Add(CreateMachineCard(machineName, status));
    }

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

    private static string FormatMachineLabel(string machineName, string status)
        => $"{machineName} - {status}";

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

    private void SetMachineActionButtonsEnabled(bool enabled)
    {
        btnLockMachine.Enabled = enabled;
        btnUnlockMachine.Enabled = enabled;
    }

    private static string NormalizeMachineId(string machineId)
        => string.IsNullOrWhiteSpace(machineId) ? "UNKNOWN" : machineId.Trim();

    private static string NormalizeStatus(string status)
        => string.IsNullOrWhiteSpace(status) ? "UNKNOWN" : status.Trim().ToUpperInvariant();

    private static int TryGetMachineNumber(string machineName)
    {
        string digits = new(machineName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int machineNumber) ? machineNumber : 0;
    }

    private sealed class NetworkAdminCommandService(TcpJsonLineServer networkServer) : IAdminCommandService
    {
        public async Task<AdminCommandResult> SendAsync(
            AdminCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            MachineCommandSendResult commandResult = await networkServer.SendMachineCommandWithResultAsync(
                request.MachineId,
                lockMachine: request.Command == CommandType.LOCK,
                issuedBy: request.IssuedBy,
                reason: request.Reason,
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

    private void LoadSampleCustomerData()
    {
        dgvCustomers.Rows.Clear();

        dgvCustomers.Rows.Add(1, "Chi", "Nguyễn", "0128475621", "264493270", "16/04/1996", "Chi123", "123456", "10000");
        dgvCustomers.Rows.Add(2, "Thanh", "Nguyễn", "0902548345", "025351810", "12/12/1995", "Thanh123", "123456", "20000");
        dgvCustomers.Rows.Add(3, "Hà", "Trần", "012038950", "025351818", "03/02/1990", "Ha", "123456", "10000");
        dgvCustomers.Rows.Add(4, "Châu", "Trần", "0919512120", "025609999", "03/08/1990", "Chaubc", "123456", "5000");
        dgvCustomers.Rows.Add(5, "Linh", "Võ", "01212239011", "025607777", "30/04/1990", "PkLanh", "123456", "20000");
    }
}
