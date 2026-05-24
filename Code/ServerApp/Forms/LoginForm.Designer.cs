namespace ServerApp;

partial class LoginForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
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
        loginPanel.Location = new Point(24, 24);
        loginPanel.Name = "loginPanel";
        loginPanel.RowCount = 6;
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        loginPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        loginPanel.Size = new Size(376, 270);
        loginPanel.TabIndex = 0;
        // 
        // lblTitle
        // 
        loginPanel.SetColumnSpan(lblTitle, 2);
        lblTitle.Dock = DockStyle.Fill;
        lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        lblTitle.Location = new Point(3, 0);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(370, 62);
        lblTitle.TabIndex = 0;
        lblTitle.Text = UiStrings.LoginTitle;
        lblTitle.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // lblUsername
        // 
        lblUsername.Dock = DockStyle.Fill;
        lblUsername.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblUsername.Location = new Point(3, 62);
        lblUsername.Name = "lblUsername";
        lblUsername.Size = new Size(114, 42);
        lblUsername.TabIndex = 1;
        lblUsername.Text = UiStrings.LoginUsernameLabel;
        lblUsername.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtUsername
        // 
        txtUsername.Dock = DockStyle.Fill;
        txtUsername.Location = new Point(123, 71);
        txtUsername.Margin = new Padding(3, 9, 3, 3);
        txtUsername.Name = "txtUsername";
        txtUsername.Size = new Size(250, 23);
        txtUsername.TabIndex = 1;
        txtUsername.Text = "";
        // 
        // lblPassword
        // 
        lblPassword.Dock = DockStyle.Fill;
        lblPassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblPassword.Location = new Point(3, 104);
        lblPassword.Name = "lblPassword";
        lblPassword.Size = new Size(114, 42);
        lblPassword.TabIndex = 3;
        lblPassword.Text = UiStrings.LoginPasswordLabel;
        lblPassword.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtPassword
        // 
        txtPassword.Dock = DockStyle.Fill;
        txtPassword.Location = new Point(123, 113);
        txtPassword.Margin = new Padding(3, 9, 3, 3);
        txtPassword.Name = "txtPassword";
        txtPassword.PasswordChar = '*';
        txtPassword.Size = new Size(250, 23);
        txtPassword.TabIndex = 2;
        txtPassword.Text = "";
        // 
        // lblMachineId
        // 
        lblMachineId.Dock = DockStyle.Fill;
        lblMachineId.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblMachineId.Location = new Point(3, 146);
        lblMachineId.Name = "lblMachineId";
        lblMachineId.Size = new Size(114, 42);
        lblMachineId.TabIndex = 4;
        lblMachineId.Text = UiStrings.LoginMachineIdLabel;
        lblMachineId.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtMachineId
        // 
        txtMachineId.Dock = DockStyle.Fill;
        txtMachineId.Location = new Point(123, 155);
        txtMachineId.Margin = new Padding(3, 9, 3, 3);
        txtMachineId.Name = "txtMachineId";
        txtMachineId.Size = new Size(250, 23);
        txtMachineId.TabIndex = 3;
        txtMachineId.Text = "";
        // 
        // buttonPanel
        // 
        loginPanel.SetColumnSpan(buttonPanel, 2);
        buttonPanel.Controls.Add(btnLogin);
        buttonPanel.Controls.Add(btnExit);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Location = new Point(3, 188);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Padding = new Padding(0, 10, 0, 0);
        buttonPanel.Size = new Size(370, 48);
        buttonPanel.TabIndex = 5;
        // 
        // btnLogin
        // 
        btnLogin.Location = new Point(247, 10);
        btnLogin.Margin = new Padding(8, 0, 0, 0);
        btnLogin.Name = "btnLogin";
        btnLogin.Size = new Size(123, 32);
        btnLogin.TabIndex = 0;
        btnLogin.Text = UiStrings.LoginButtonText;
        btnLogin.UseVisualStyleBackColor = true;
        btnLogin.Click += BtnLogin_Click;
        // 
        // btnExit
        // 
        btnExit.DialogResult = DialogResult.Cancel;
        btnExit.Location = new Point(116, 10);
        btnExit.Margin = new Padding(8, 0, 0, 0);
        btnExit.Name = "btnExit";
        btnExit.Size = new Size(123, 32);
        btnExit.TabIndex = 1;
        btnExit.Text = UiStrings.ExitButtonText;
        btnExit.UseVisualStyleBackColor = true;
        // 
        // lblMessage
        // 
        loginPanel.SetColumnSpan(lblMessage, 2);
        lblMessage.Dock = DockStyle.Fill;
        lblMessage.ForeColor = Color.FromArgb(170, 45, 45);
        lblMessage.Location = new Point(3, 242);
        lblMessage.Name = "lblMessage";
        lblMessage.Size = new Size(370, 28);
        lblMessage.TabIndex = 6;
        lblMessage.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // LoginForm
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
        Name = "LoginForm";
        Padding = new Padding(24);
        ActiveControl = txtUsername;
        StartPosition = FormStartPosition.CenterScreen;
        Text = UiStrings.LoginFormTitle;
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
