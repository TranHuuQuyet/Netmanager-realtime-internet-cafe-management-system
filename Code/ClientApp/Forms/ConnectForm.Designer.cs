namespace ClientApp;

partial class ConnectForm
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
    private Label hostLabel = null!;
    private Label portLabel = null!;
    private TextBox hostTextBox = null!;
    private TextBox portTextBox = null!;
    private Button openLoginButton = null!;
    private Button exitButton = null!;

    private void InitializeComponent()
    {
        titleLabel = new Label();
        hostLabel = new Label();
        portLabel = new Label();
        hostTextBox = new TextBox();
        portTextBox = new TextBox();
        openLoginButton = new Button();
        exitButton = new Button();
        SuspendLayout();
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
        titleLabel.Location = new Point(24, 20);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(183, 30);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Client Connect";
        // 
        // hostLabel
        // 
        hostLabel.AutoSize = true;
        hostLabel.Location = new Point(28, 82);
        hostLabel.Name = "hostLabel";
        hostLabel.Size = new Size(32, 15);
        hostLabel.TabIndex = 1;
        hostLabel.Text = "Host";
        // 
        // portLabel
        // 
        portLabel.AutoSize = true;
        portLabel.Location = new Point(28, 126);
        portLabel.Name = "portLabel";
        portLabel.Size = new Size(29, 15);
        portLabel.TabIndex = 2;
        portLabel.Text = "Port";
        // 
        // hostTextBox
        // 
        hostTextBox.Location = new Point(94, 78);
        hostTextBox.Name = "hostTextBox";
        hostTextBox.Size = new Size(220, 23);
        hostTextBox.TabIndex = 3;
        hostTextBox.Text = "127.0.0.1";
        // 
        // portTextBox
        // 
        portTextBox.Location = new Point(94, 122);
        portTextBox.Name = "portTextBox";
        portTextBox.Size = new Size(100, 23);
        portTextBox.TabIndex = 4;
        portTextBox.Text = "9000";
        // 
        // openLoginButton
        // 
        openLoginButton.Location = new Point(94, 176);
        openLoginButton.Name = "openLoginButton";
        openLoginButton.Size = new Size(120, 32);
        openLoginButton.TabIndex = 5;
        openLoginButton.Text = "Open Login";
        openLoginButton.UseVisualStyleBackColor = true;
        openLoginButton.Click += OpenLoginFormButton_Click;
        // 
        // exitButton
        // 
        exitButton.Location = new Point(220, 176);
        exitButton.Name = "exitButton";
        exitButton.Size = new Size(94, 32);
        exitButton.TabIndex = 6;
        exitButton.Text = "Exit";
        exitButton.UseVisualStyleBackColor = true;
        exitButton.Click += ExitButton_Click;
        // 
        // ConnectForm
        // 
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(360, 242);
        Controls.Add(exitButton);
        Controls.Add(openLoginButton);
        Controls.Add(portTextBox);
        Controls.Add(hostTextBox);
        Controls.Add(portLabel);
        Controls.Add(hostLabel);
        Controls.Add(titleLabel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ConnectForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "NetManager Client - Connect";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
