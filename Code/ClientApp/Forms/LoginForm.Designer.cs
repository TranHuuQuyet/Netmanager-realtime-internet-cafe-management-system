namespace ClientApp;

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

    private Label titleLabel = null!;
    private Label usernameLabel = null!;
    private Label passwordLabel = null!;
    private Label machineIdLabel = null!;
    private TextBox usernameTextBox = null!;
    private TextBox passwordTextBox = null!;
    private TextBox machineIdTextBox = null!;
    private Button openMainButton = null!;
    private Button backButton = null!;

    private void InitializeComponent()
    {
        titleLabel = new Label();
        usernameLabel = new Label();
        passwordLabel = new Label();
        machineIdLabel = new Label();
        usernameTextBox = new TextBox();
        passwordTextBox = new TextBox();
        machineIdTextBox = new TextBox();
        openMainButton = new Button();
        backButton = new Button();
        SuspendLayout();
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
        titleLabel.Location = new Point(24, 20);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(130, 30);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Client Login";
        // 
        // usernameLabel
        // 
        usernameLabel.AutoSize = true;
        usernameLabel.Location = new Point(28, 82);
        usernameLabel.Name = "usernameLabel";
        usernameLabel.Size = new Size(60, 15);
        usernameLabel.TabIndex = 1;
        usernameLabel.Text = "Username";
        // 
        // passwordLabel
        // 
        passwordLabel.AutoSize = true;
        passwordLabel.Location = new Point(28, 126);
        passwordLabel.Name = "passwordLabel";
        passwordLabel.Size = new Size(57, 15);
        passwordLabel.TabIndex = 2;
        passwordLabel.Text = "Password";
        // 
        // machineIdLabel
        // 
        machineIdLabel.AutoSize = true;
        machineIdLabel.Location = new Point(28, 170);
        machineIdLabel.Name = "machineIdLabel";
        machineIdLabel.Size = new Size(62, 15);
        machineIdLabel.TabIndex = 3;
        machineIdLabel.Text = "MachineId";
        // 
        // usernameTextBox
        // 
        usernameTextBox.Location = new Point(110, 78);
        usernameTextBox.Name = "usernameTextBox";
        usernameTextBox.Size = new Size(220, 23);
        usernameTextBox.TabIndex = 4;
        usernameTextBox.Text = "client01";
        // 
        // passwordTextBox
        // 
        passwordTextBox.Location = new Point(110, 122);
        passwordTextBox.Name = "passwordTextBox";
        passwordTextBox.Size = new Size(220, 23);
        passwordTextBox.TabIndex = 5;
        passwordTextBox.Text = "123456";
        passwordTextBox.UseSystemPasswordChar = true;
        // 
        // machineIdTextBox
        // 
        machineIdTextBox.Location = new Point(110, 166);
        machineIdTextBox.Name = "machineIdTextBox";
        machineIdTextBox.Size = new Size(220, 23);
        machineIdTextBox.TabIndex = 6;
        machineIdTextBox.Text = "PC-01";
        // 
        // openMainButton
        // 
        openMainButton.Location = new Point(110, 220);
        openMainButton.Name = "openMainButton";
        openMainButton.Size = new Size(120, 32);
        openMainButton.TabIndex = 7;
        openMainButton.Text = "Open Main";
        openMainButton.UseVisualStyleBackColor = true;
        openMainButton.Click += OpenMainFormButton_Click;
        // 
        // backButton
        // 
        backButton.Location = new Point(236, 220);
        backButton.Name = "backButton";
        backButton.Size = new Size(94, 32);
        backButton.TabIndex = 8;
        backButton.Text = "Back";
        backButton.UseVisualStyleBackColor = true;
        backButton.Click += BackButton_Click;
        // 
        // LoginForm
        // 
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(380, 280);
        Controls.Add(backButton);
        Controls.Add(openMainButton);
        Controls.Add(machineIdTextBox);
        Controls.Add(passwordTextBox);
        Controls.Add(usernameTextBox);
        Controls.Add(machineIdLabel);
        Controls.Add(passwordLabel);
        Controls.Add(usernameLabel);
        Controls.Add(titleLabel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "LoginForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "NetManager Client - Login";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
