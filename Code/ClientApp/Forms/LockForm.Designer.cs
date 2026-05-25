namespace ClientApp;

partial class LockForm
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
    private Label summaryLabel = null!;
    private Button unlockButton = null!;

    private void InitializeComponent()
    {
        titleLabel = new Label();
        summaryLabel = new Label();
        unlockButton = new Button();
        SuspendLayout();
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
        titleLabel.Location = new Point(24, 20);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(115, 30);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Lock Shell";
        // 
        // summaryLabel
        // 
        summaryLabel.AutoSize = true;
        summaryLabel.Location = new Point(28, 74);
        summaryLabel.Name = "summaryLabel";
        summaryLabel.Size = new Size(270, 15);
        summaryLabel.TabIndex = 1;
        summaryLabel.Text = "Placeholder lock screen shell for client session.";
        // 
        // unlockButton
        // 
        unlockButton.Location = new Point(28, 116);
        unlockButton.Name = "unlockButton";
        unlockButton.Size = new Size(120, 32);
        unlockButton.TabIndex = 2;
        unlockButton.Text = "Unlock";
        unlockButton.UseVisualStyleBackColor = true;
        unlockButton.Click += UnlockButton_Click;
        // 
        // LockForm
        // 
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(320, 180);
        Controls.Add(unlockButton);
        Controls.Add(summaryLabel);
        Controls.Add(titleLabel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "LockForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "NetManager Client - Lock";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
