using System.Drawing.Drawing2D;

namespace ClientApp.Forms;

public sealed class ConnectForm : Form
{
    private static readonly Color AppBackground = Color.FromArgb(243, 246, 250);
    private static readonly Color CardBackground = Color.White;
    private static readonly Color BorderSoft = Color.FromArgb(220, 226, 234);
    private static readonly Color LabelInk = Color.FromArgb(39, 47, 59);
    private static readonly Color InputInk = Color.FromArgb(30, 36, 48);
    private static readonly Color FooterInk = Color.FromArgb(104, 115, 132);
    private static readonly Color AccentBlue = Color.FromArgb(40, 110, 224);
    private static readonly Color AccentBlueHover = Color.FromArgb(31, 95, 201);
    private static readonly Color SecondaryFill = Color.FromArgb(236, 241, 248);
    private static readonly Color SecondaryHover = Color.FromArgb(223, 232, 245);
    private static readonly Color StatusReady = Color.FromArgb(34, 164, 92);

    private readonly TextBox _hostTextBox;
    private readonly NumericUpDown _portInput;
    private readonly TextBox _usernameTextBox;
    private readonly TextBox _passwordTextBox;
    private readonly TextBox _machineIdTextBox;
    private readonly Label _statusLabel;
    private readonly Panel _statusDot;
    private readonly Panel _statusBadge;

    public ConnectForm()
    {
        Text = "NetManager Client";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        ClientSize = new Size(760, 620);
        MinimumSize = new Size(760, 620);
        BackColor = AppBackground;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        AutoScaleMode = AutoScaleMode.Dpi;
        Icon = SystemIcons.Application;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20, 16, 20, 16),
            BackColor = AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildHeader(), 0, 0);

        var cardPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBackground,
            Padding = new Padding(18, 16, 18, 16),
            Margin = new Padding(0, 14, 0, 6)
        };
        cardPanel.Paint += CardPanel_Paint;

        var formLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6
        };
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F));
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var endpointLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 6, 0, 6),
            Padding = new Padding(0)
        };
        endpointLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        endpointLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));

        _hostTextBox = BuildInput("127.0.0.1");
        _portInput = BuildPortInput(5000);
        _hostTextBox.Margin = new Padding(0);
        _portInput.Margin = new Padding(0);
        _usernameTextBox = BuildInput("client01");
        _passwordTextBox = BuildInput("123");
        _passwordTextBox.UseSystemPasswordChar = true;
        _machineIdTextBox = BuildInput("PC-01");

        endpointLayout.Controls.Add(_hostTextBox, 0, 0);
        endpointLayout.Controls.Add(_portInput, 1, 0);

        AddInputRow(formLayout, 0, "Endpoint", endpointLayout);
        AddInputRow(formLayout, 1, "Username", _usernameTextBox);
        AddInputRow(formLayout, 2, "Password", _passwordTextBox);
        AddInputRow(formLayout, 3, "Machine ID", _machineIdTextBox);

        var statusLabel = BuildLabel("Status");
        statusLabel.Margin = new Padding(0, 12, 10, 0);
        formLayout.Controls.Add(statusLabel, 0, 4);

        var statusBadge = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(239, 251, 244),
            BorderStyle = BorderStyle.None,
            Margin = new Padding(0, 12, 0, 0),
            Padding = new Padding(10, 0, 10, 0)
        };
        statusBadge.Paint += StatusBadge_Paint;
        _statusBadge = statusBadge;

        _statusDot = new Panel
        {
            Size = new Size(8, 8),
            BackColor = StatusReady
        };
        _statusDot.Paint += StatusDot_Paint;

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Ready",
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = StatusReady,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        var statusInner = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        statusInner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14F));
        statusInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        statusInner.Controls.Add(_statusDot, 0, 0);
        statusInner.Controls.Add(_statusLabel, 1, 0);
        statusBadge.Controls.Add(statusInner);
        formLayout.Controls.Add(statusBadge, 1, 4);
        var spacer = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        formLayout.Controls.Add(spacer, 0, 5);
        formLayout.SetColumnSpan(spacer, 2);

        cardPanel.Controls.Add(formLayout);
        root.Controls.Add(cardPanel, 0, 1);
        root.Controls.Add(BuildButtons(), 0, 2);
        root.Controls.Add(BuildFooter(), 0, 3);

        Controls.Add(root);
        Shown += (_, _) => _statusDot.Region = BuildRoundedRegion(_statusDot.ClientRectangle, 4);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(0),
            Height = 88
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var iconHolder = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(232, 240, 255),
            Margin = new Padding(0, 2, 8, 2),
            Padding = new Padding(6)
        };
        iconHolder.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(Color.FromArgb(65, 127, 230));
            using var pen = new Pen(Color.FromArgb(31, 96, 204), 1.8F);
            e.Graphics.FillEllipse(fill, 8, 8, 20, 20);
            e.Graphics.DrawEllipse(pen, 8, 8, 20, 20);
            e.Graphics.DrawLine(pen, 13, 18, 23, 18);
            e.Graphics.DrawLine(pen, 18, 13, 18, 23);
        };

        var titleBlock = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        titleBlock.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        titleBlock.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        titleBlock.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Cyber Cafe Client",
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            ForeColor = LabelInk,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        titleBlock.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Session gateway and machine access",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = FooterInk,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        header.Controls.Add(iconHolder, 0, 0);
        header.Controls.Add(titleBlock, 1, 0);
        return header;
    }

    private Control BuildButtons()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 6)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        panel.Height = 60;

        var connectButton = BuildRoundedButton("Connect", AccentBlue, AccentBlueHover, Color.White);
        connectButton.Click += PreviewButton_Click;
        var lockButton = BuildRoundedButton("Lock", SecondaryFill, SecondaryHover, LabelInk);
        lockButton.Click += LockPreview_Click;

        panel.Controls.Add(connectButton, 0, 0);
        panel.Controls.Add(lockButton, 1, 0);
        return panel;
    }

    private Control BuildFooter()
    {
        return new Label
        {
            Dock = DockStyle.Top,
            Text = "NetManager Client 0.2",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = FooterInk,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0),
            Height = 24
        };
    }

    private static Label BuildLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            AutoSize = false,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = LabelInk,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 6, 10, 6)
        };
    }

    private static TextBox BuildInput(string text)
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = InputInk,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 6, 0, 6)
        };
    }

    private static NumericUpDown BuildPortInput(decimal value)
    {
        return new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 65535,
            Value = value,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = InputInk,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 6, 0, 6)
        };
    }

    private static void AddInputRow(TableLayoutPanel layout, int row, string label, Control input)
    {
        layout.Controls.Add(BuildLabel(label), 0, row);
        layout.Controls.Add(input, 1, row);
    }

    private static Button BuildRoundedButton(string text, Color backColor, Color hoverColor, Color foreColor)
    {
        var button = new Button
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            BackColor = backColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(4, 0, 4, 0),
            Padding = new Padding(0),
            MinimumSize = new Size(0, 46),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;

        button.MouseEnter += (_, _) => button.BackColor = hoverColor;
        button.MouseLeave += (_, _) => button.BackColor = backColor;
        button.Resize += (_, _) => button.Region = BuildRoundedRegion(button.ClientRectangle, 8);

        return button;
    }

    private void CardPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel)
        {
            return;
        }

        using var border = new Pen(BorderSoft, 1F);
        e.Graphics.DrawRectangle(border, 0, 0, panel.Width - 1, panel.Height - 1);
    }

    private static void StatusBadge_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var border = new Pen(Color.FromArgb(204, 236, 218), 1F);
        var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
        e.Graphics.DrawPath(border, RoundedPath(rect, 7));
    }

    private static void StatusDot_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel dot)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(StatusReady);
        e.Graphics.FillEllipse(fill, 0, 0, dot.Width - 1, dot.Height - 1);
    }

    private static Region BuildRoundedRegion(Rectangle rectangle, int radius)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return new Region();
        }

        return new Region(RoundedPath(new Rectangle(0, 0, rectangle.Width - 1, rectangle.Height - 1), radius));
    }

    private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        radius = Math.Max(1, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.StartFigure();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void PreviewButton_Click(object? sender, EventArgs e)
    {
        if (!ValidateInput())
        {
            return;
        }

        using var mainForm = new ClientMainForm(
            _usernameTextBox.Text.Trim(),
            _machineIdTextBox.Text.Trim(),
            _hostTextBox.Text.Trim(),
            (int)_portInput.Value);
        Hide();
        mainForm.ShowDialog(this);
        Show();
    }

    private void LockPreview_Click(object? sender, EventArgs e)
    {
        using var lockForm = new LockScreenForm();
        lockForm.ShowDialog(this);
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(_hostTextBox.Text))
        {
            SetStatus("Server endpoint is required.", Color.FromArgb(181, 59, 59), Color.FromArgb(255, 240, 240));
            _hostTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(_usernameTextBox.Text))
        {
            SetStatus("Username is required.", Color.FromArgb(181, 59, 59), Color.FromArgb(255, 240, 240));
            _usernameTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(_passwordTextBox.Text))
        {
            SetStatus("Password is required.", Color.FromArgb(181, 59, 59), Color.FromArgb(255, 240, 240));
            _passwordTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(_machineIdTextBox.Text))
        {
            SetStatus("Machine ID is required.", Color.FromArgb(181, 59, 59), Color.FromArgb(255, 240, 240));
            _machineIdTextBox.Focus();
            return false;
        }

        SetStatus("Ready", StatusReady, Color.FromArgb(239, 251, 244));
        return true;
    }

    private void SetStatus(string message, Color textColor, Color badgeColor)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = textColor;
        _statusDot.BackColor = textColor;
        _statusBadge.BackColor = badgeColor;
        _statusBadge.Invalidate();
    }
}
