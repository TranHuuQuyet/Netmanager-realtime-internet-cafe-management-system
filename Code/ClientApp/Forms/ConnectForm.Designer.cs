namespace ClientApp.Forms;

partial class ConnectForm
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        loginPanel = new TableLayoutPanel();
        lblTitle = new Label();
        lblUsername = new Label();
        txtUsername = new TextBox();
        lblPassword = new Label();
        txtPassword = new TextBox();
        lblMachineId = new Label();
        txtMachineId = new TextBox();
        buttonPanel = new FlowLayoutPanel();
        btnLogin = new Button();
        btnExit = new Button();
        lblMessage = new Label();
        loginPanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // loginPanel
        // 
        loginPanel.ColumnCount = 2;
        loginPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        loginPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        loginPanel.Controls.Add(lblTitle, 0, 0);
        loginPanel.Controls.Add(lblUsername, 0, 1);
        loginPanel.Controls.Add(txtUsername, 1, 1);
        loginPanel.Controls.Add(lblPassword, 0, 2);
        loginPanel.Controls.Add(txtPassword, 1, 2);
        loginPanel.Controls.Add(lblMachineId, 0, 3);
        loginPanel.Controls.Add(txtMachineId, 1, 3);
        loginPanel.Controls.Add(buttonPanel, 0, 4);
        loginPanel.Controls.Add(lblMessage, 0, 5);
        loginPanel.Dock = DockStyle.Fill;
        loginPanel.Name = "loginPanel";
        loginPanel.RowCount = 6;
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        loginPanel.TabIndex = 0;
        // 
        // lblTitle
        // 
        loginPanel.SetColumnSpan(lblTitle, 2);
        lblTitle.Dock = DockStyle.Fill;
        lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        lblTitle.Name = "lblTitle";
        lblTitle.TabIndex = 0;
        lblTitle.Text = "ĐĂNG NHẬP MÁY TRẠM";
        lblTitle.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // lblUsername
        // 
        lblUsername.Dock = DockStyle.Fill;
        lblUsername.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblUsername.Name = "lblUsername";
        lblUsername.TabIndex = 1;
        lblUsername.Text = "Tài khoản";
        lblUsername.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtUsername
        // 
        txtUsername.Dock = DockStyle.Fill;
        txtUsername.Margin = new Padding(3, 9, 3, 3);
        txtUsername.Name = "txtUsername";
        txtUsername.TabIndex = 1;
        // 
        // lblPassword
        // 
        lblPassword.Dock = DockStyle.Fill;
        lblPassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblPassword.Name = "lblPassword";
        lblPassword.TabIndex = 2;
        lblPassword.Text = "Mật khẩu";
        lblPassword.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtPassword
        // 
        txtPassword.Dock = DockStyle.Fill;
        txtPassword.Margin = new Padding(3, 9, 3, 3);
        txtPassword.Name = "txtPassword";
        txtPassword.PasswordChar = '*';
        txtPassword.TabIndex = 2;
        // 
        // lblMachineId
        // 
        lblMachineId.Dock = DockStyle.Fill;
        lblMachineId.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblMachineId.Name = "lblMachineId";
        lblMachineId.TabIndex = 3;
        lblMachineId.Text = "Mã máy";
        lblMachineId.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtMachineId
        // 
        txtMachineId.Dock = DockStyle.Fill;
        txtMachineId.Margin = new Padding(3, 9, 3, 3);
        txtMachineId.Name = "txtMachineId";
        txtMachineId.ReadOnly = true;
        txtMachineId.TabIndex = 3;
        txtMachineId.TabStop = false;
        // 
        // buttonPanel
        // 
        loginPanel.SetColumnSpan(buttonPanel, 2);
        buttonPanel.Controls.Add(btnLogin);
        buttonPanel.Controls.Add(btnExit);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Padding = new Padding(0, 10, 0, 0);
        buttonPanel.TabIndex = 4;
        // 
        // btnLogin
        // 
        btnLogin.Margin = new Padding(8, 0, 0, 0);
        btnLogin.Name = "btnLogin";
        btnLogin.Size = new Size(123, 32);
        btnLogin.TabIndex = 0;
        btnLogin.Text = "Đăng nhập";
        btnLogin.UseVisualStyleBackColor = true;
        btnLogin.Click += BtnLogin_Click;
        // 
        // btnExit
        // 
        btnExit.DialogResult = DialogResult.Cancel;
        btnExit.Margin = new Padding(8, 0, 0, 0);
        btnExit.Name = "btnExit";
        btnExit.Size = new Size(123, 32);
        btnExit.TabIndex = 1;
        btnExit.Text = "Thoát";
        btnExit.UseVisualStyleBackColor = true;
        btnExit.Click += BtnExit_Click;
        // 
        // lblMessage
        // 
        loginPanel.SetColumnSpan(lblMessage, 2);
        lblMessage.Dock = DockStyle.Fill;
        lblMessage.ForeColor = Color.FromArgb(170, 45, 45);
        lblMessage.Name = "lblMessage";
        lblMessage.TabIndex = 5;
        lblMessage.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // ConnectForm
        // 
        AcceptButton = btnLogin;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnExit;
        ClientSize = new Size(424, 318);
        Controls.Add(loginPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ConnectForm";
        Padding = new Padding(24);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Đăng nhập máy trạm";
        loginPanel.ResumeLayout(false);
        loginPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel loginPanel;
    private Label lblTitle;
    private Label lblUsername;
    private TextBox txtUsername;
    private Label lblPassword;
    private TextBox txtPassword;
    private Label lblMachineId;
    private TextBox txtMachineId;
    private FlowLayoutPanel buttonPanel;
    private Button btnLogin;
    private Button btnExit;
    private Label lblMessage;
}
