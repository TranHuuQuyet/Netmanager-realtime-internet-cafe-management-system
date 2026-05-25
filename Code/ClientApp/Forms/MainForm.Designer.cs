namespace ClientApp;

partial class MainForm
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

    private Panel headerPanel = null!;
    private Label machineTitleLabel = null!;
    private Label machineNameLabel = null!;
    private GroupBox sessionGroupBox = null!;
    private Label totalTimeLabel = null!;
    private Label usedTimeLabel = null!;
    private Label remainingTimeLabel = null!;
    private Label waitCostLabel = null!;
    private Label loginTimeLabel = null!;
    private Label totalTimeValueLabel = null!;
    private Label usedTimeValueLabel = null!;
    private Label remainingTimeValueLabel = null!;
    private Label waitCostValueLabel = null!;
    private Label loginTimeValueLabel = null!;
    private FlowLayoutPanel actionPanel = null!;
    private Button unlockButton = null!;
    private Button logoutButton = null!;
    private Button chatButton = null!;

    private void InitializeComponent()
    {
        headerPanel = new Panel();
        machineTitleLabel = new Label();
        machineNameLabel = new Label();
        sessionGroupBox = new GroupBox();
        totalTimeLabel = new Label();
        usedTimeLabel = new Label();
        remainingTimeLabel = new Label();
        waitCostLabel = new Label();
        loginTimeLabel = new Label();
        totalTimeValueLabel = new Label();
        usedTimeValueLabel = new Label();
        remainingTimeValueLabel = new Label();
        waitCostValueLabel = new Label();
        loginTimeValueLabel = new Label();
        actionPanel = new FlowLayoutPanel();
        unlockButton = new Button();
        logoutButton = new Button();
        chatButton = new Button();
        headerPanel.SuspendLayout();
        sessionGroupBox.SuspendLayout();
        actionPanel.SuspendLayout();
        SuspendLayout();
        // 
        // headerPanel
        // 
        headerPanel.BackColor = Color.Gainsboro;
        headerPanel.Controls.Add(machineNameLabel);
        headerPanel.Controls.Add(machineTitleLabel);
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(464, 72);
        headerPanel.TabIndex = 0;
        // 
        // machineTitleLabel
        // 
        machineTitleLabel.Dock = DockStyle.Top;
        machineTitleLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
        machineTitleLabel.Location = new Point(0, 0);
        machineTitleLabel.Name = "machineTitleLabel";
        machineTitleLabel.Size = new Size(464, 28);
        machineTitleLabel.TabIndex = 0;
        machineTitleLabel.Text = "MÁY TRẠM";
        machineTitleLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // machineNameLabel
        // 
        machineNameLabel.Dock = DockStyle.Fill;
        machineNameLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
        machineNameLabel.Location = new Point(0, 0);
        machineNameLabel.Name = "machineNameLabel";
        machineNameLabel.Padding = new Padding(0, 24, 0, 0);
        machineNameLabel.Size = new Size(464, 72);
        machineNameLabel.TabIndex = 1;
        machineNameLabel.Text = "Máy 1";
        machineNameLabel.TextAlign = ContentAlignment.TopCenter;
        // 
        // sessionGroupBox
        // 
        sessionGroupBox.Controls.Add(loginTimeValueLabel);
        sessionGroupBox.Controls.Add(waitCostValueLabel);
        sessionGroupBox.Controls.Add(remainingTimeValueLabel);
        sessionGroupBox.Controls.Add(usedTimeValueLabel);
        sessionGroupBox.Controls.Add(totalTimeValueLabel);
        sessionGroupBox.Controls.Add(loginTimeLabel);
        sessionGroupBox.Controls.Add(waitCostLabel);
        sessionGroupBox.Controls.Add(remainingTimeLabel);
        sessionGroupBox.Controls.Add(usedTimeLabel);
        sessionGroupBox.Controls.Add(totalTimeLabel);
        sessionGroupBox.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        sessionGroupBox.Location = new Point(16, 88);
        sessionGroupBox.Name = "sessionGroupBox";
        sessionGroupBox.Size = new Size(432, 178);
        sessionGroupBox.TabIndex = 1;
        sessionGroupBox.TabStop = false;
        // 
        // totalTimeLabel
        // 
        totalTimeLabel.AutoSize = true;
        totalTimeLabel.Location = new Point(16, 28);
        totalTimeLabel.Name = "totalTimeLabel";
        totalTimeLabel.Size = new Size(88, 15);
        totalTimeLabel.TabIndex = 0;
        totalTimeLabel.Text = "Tổng thời gian:";
        // 
        // usedTimeLabel
        // 
        usedTimeLabel.AutoSize = true;
        usedTimeLabel.Location = new Point(16, 59);
        usedTimeLabel.Name = "usedTimeLabel";
        usedTimeLabel.Size = new Size(103, 15);
        usedTimeLabel.TabIndex = 1;
        usedTimeLabel.Text = "Thời gian sử dụng:";
        // 
        // remainingTimeLabel
        // 
        remainingTimeLabel.AutoSize = true;
        remainingTimeLabel.Location = new Point(16, 90);
        remainingTimeLabel.Name = "remainingTimeLabel";
        remainingTimeLabel.Size = new Size(104, 15);
        remainingTimeLabel.TabIndex = 2;
        remainingTimeLabel.Text = "Thời gian còn lại:";
        // 
        // waitCostLabel
        // 
        waitCostLabel.AutoSize = true;
        waitCostLabel.Location = new Point(16, 121);
        waitCostLabel.Name = "waitCostLabel";
        waitCostLabel.Size = new Size(99, 15);
        waitCostLabel.TabIndex = 3;
        waitCostLabel.Text = "Chi phí giờ chờ:";
        // 
        // loginTimeLabel
        // 
        loginTimeLabel.AutoSize = true;
        loginTimeLabel.Location = new Point(16, 152);
        loginTimeLabel.Name = "loginTimeLabel";
        loginTimeLabel.Size = new Size(95, 15);
        loginTimeLabel.TabIndex = 4;
        loginTimeLabel.Text = "Giờ Đăng Nhập:";
        // 
        // totalTimeValueLabel
        // 
        totalTimeValueLabel.BorderStyle = BorderStyle.FixedSingle;
        totalTimeValueLabel.Location = new Point(244, 24);
        totalTimeValueLabel.Name = "totalTimeValueLabel";
        totalTimeValueLabel.Size = new Size(170, 23);
        totalTimeValueLabel.TabIndex = 5;
        totalTimeValueLabel.Text = "01:59:01";
        totalTimeValueLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // usedTimeValueLabel
        // 
        usedTimeValueLabel.BorderStyle = BorderStyle.FixedSingle;
        usedTimeValueLabel.Location = new Point(244, 55);
        usedTimeValueLabel.Name = "usedTimeValueLabel";
        usedTimeValueLabel.Size = new Size(170, 23);
        usedTimeValueLabel.TabIndex = 6;
        usedTimeValueLabel.Text = "00:00:10";
        usedTimeValueLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // remainingTimeValueLabel
        // 
        remainingTimeValueLabel.BorderStyle = BorderStyle.FixedSingle;
        remainingTimeValueLabel.Location = new Point(244, 86);
        remainingTimeValueLabel.Name = "remainingTimeValueLabel";
        remainingTimeValueLabel.Size = new Size(170, 23);
        remainingTimeValueLabel.TabIndex = 7;
        remainingTimeValueLabel.Text = "01:58:51";
        remainingTimeValueLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // waitCostValueLabel
        // 
        waitCostValueLabel.BorderStyle = BorderStyle.FixedSingle;
        waitCostValueLabel.Location = new Point(244, 117);
        waitCostValueLabel.Name = "waitCostValueLabel";
        waitCostValueLabel.Size = new Size(170, 23);
        waitCostValueLabel.TabIndex = 8;
        waitCostValueLabel.Text = "14(VND)";
        waitCostValueLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // loginTimeValueLabel
        // 
        loginTimeValueLabel.BorderStyle = BorderStyle.FixedSingle;
        loginTimeValueLabel.Location = new Point(244, 148);
        loginTimeValueLabel.Name = "loginTimeValueLabel";
        loginTimeValueLabel.Size = new Size(170, 23);
        loginTimeValueLabel.TabIndex = 9;
        loginTimeValueLabel.Text = "1/17/2017 1:41:39 PM";
        loginTimeValueLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // actionPanel
        // 
        actionPanel.Controls.Add(unlockButton);
        actionPanel.Controls.Add(logoutButton);
        actionPanel.Controls.Add(chatButton);
        actionPanel.Location = new Point(16, 280);
        actionPanel.Name = "actionPanel";
        actionPanel.Size = new Size(432, 64);
        actionPanel.TabIndex = 2;
        // 
        // unlockButton
        // 
        unlockButton.Location = new Point(3, 3);
        unlockButton.Name = "unlockButton";
        unlockButton.Size = new Size(104, 56);
        unlockButton.TabIndex = 0;
        unlockButton.Text = "Mở khóa";
        unlockButton.UseVisualStyleBackColor = true;
        unlockButton.Click += UnlockButton_Click;
        // 
        // logoutButton
        // 
        logoutButton.Location = new Point(113, 3);
        logoutButton.Name = "logoutButton";
        logoutButton.Size = new Size(104, 56);
        logoutButton.TabIndex = 1;
        logoutButton.Text = "Đăng xuất";
        logoutButton.UseVisualStyleBackColor = true;
        logoutButton.Click += LogoutButton_Click;
        // 
        // chatButton
        // 
        chatButton.Location = new Point(223, 3);
        chatButton.Name = "chatButton";
        chatButton.Size = new Size(104, 56);
        chatButton.TabIndex = 2;
        chatButton.Text = "Giao tiếp";
        chatButton.UseVisualStyleBackColor = true;
        chatButton.Click += ChatButton_Click;
        // 
        // MainForm
        // 
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(464, 364);
        Controls.Add(actionPanel);
        Controls.Add(sessionGroupBox);
        Controls.Add(headerPanel);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Máy trạm";
        headerPanel.ResumeLayout(false);
        sessionGroupBox.ResumeLayout(false);
        sessionGroupBox.PerformLayout();
        actionPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
