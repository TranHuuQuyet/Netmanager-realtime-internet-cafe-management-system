namespace ServerApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        LoadMachineData();
        LoadCustomerData();
        SelectMachine("PC01");
    }

    private void LoadMachineData()
    {
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
            string machineName = $"PC{machineNumber:00}";

            dgvMachines.Rows.Add(machineNumber, machineNumber, statuses[index], machineName);
            pnlMachineCards.Controls.Add(CreateMachineCard(machineName, statuses[index]));
        }
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
            Tag = status
        };
        icon.Paint += MachineIcon_Paint;

        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"{machineName} - {status}",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold)
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
        if (sender is PictureBox pictureBox && pictureBox.Parent?.Tag is string parentMachineName)
        {
            SelectMachine(parentMachineName);
            return;
        }

        if (sender is Control control && control.Tag is string machineName)
        {
            SelectMachine(machineName);
        }
    }

    private void DgvMachines_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvMachines.CurrentRow?.Cells["MachineNameColumn"].Value is string machineName)
        {
            SelectMachine(machineName);
        }
    }

    private void SelectMachine(string machineName)
    {
        lblSelectedClient.Text = string.Format(UiStrings.ChatWithMachineTemplate, machineName);
        txtChatHistory.Text = string.Format(UiStrings.ChatHistoryTemplate, machineName);

        foreach (DataGridViewRow row in dgvMachines.Rows)
        {
            if (row.Cells["MachineNameColumn"].Value?.ToString() == machineName)
            {
                row.Selected = true;
                dgvMachines.CurrentCell = row.Cells[0];
                break;
            }
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

    private void LoadCustomerData()
    {
        dgvCustomers.Rows.Clear();

        dgvCustomers.Rows.Add(1, "Chi", "Nguyễn", "0128475621", "264493270", "16/04/1996", "Chi123", "123456", "10000");
        dgvCustomers.Rows.Add(2, "Thanh", "Nguyễn", "0902548345", "025351810", "12/12/1995", "Thanh123", "123456", "20000");
        dgvCustomers.Rows.Add(3, "Hà", "Trần", "012038950", "025351818", "03/02/1990", "Ha", "123456", "10000");
        dgvCustomers.Rows.Add(4, "Châu", "Trần", "0919512120", "025609999", "03/08/1990", "Chaubc", "123456", "5000");
        dgvCustomers.Rows.Add(5, "Linh", "Võ", "01212239011", "025607777", "30/04/1990", "PkLanh", "123456", "20000");
    }
}
