namespace ServerApp;

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

    private void InitializeComponent()
    {
        mainTabs = new TabControl();
        tabMachines = new TabPage();
        machineLayout = new TableLayoutPanel();
        lblMachineTitle = new Label();
        pnlMachineCards = new FlowLayoutPanel();
        machineActions = new FlowLayoutPanel();
        btnLockMachine = new Button();
        btnUnlockMachine = new Button();
        btnShutdownMachine = new Button();
        machineSplit = new SplitContainer();
        dgvMachines = new DataGridView();
        MaMayColumn = new DataGridViewTextBoxColumn();
        SoMayColumn = new DataGridViewTextBoxColumn();
        TinhTrangColumn = new DataGridViewTextBoxColumn();
        MachineNameColumn = new DataGridViewTextBoxColumn();
        chatGroup = new GroupBox();
        chatLayout = new TableLayoutPanel();
        lblSelectedClient = new Label();
        txtChatHistory = new TextBox();
        chatInputLayout = new TableLayoutPanel();
        txtChatMessage = new TextBox();
        btnSendChat = new Button();
        tabCustomers = new TabPage();
        customerLayout = new TableLayoutPanel();
        customerTopLayout = new TableLayoutPanel();
        customerInfoLayout = new TableLayoutPanel();
        lblCustomerId = new Label();
        txtCustomerId = new TextBox();
        lblFirstName = new Label();
        txtFirstName = new TextBox();
        lblLastName = new Label();
        txtLastName = new TextBox();
        lblPhone = new Label();
        txtPhone = new TextBox();
        lblIdentity = new Label();
        txtIdentity = new TextBox();
        lblBirthday = new Label();
        txtBirthday = new TextBox();
        loginInfoLayout = new TableLayoutPanel();
        lblLoginTitle = new Label();
        lblUsername = new Label();
        txtUsername = new TextBox();
        lblPassword = new Label();
        txtPassword = new TextBox();
        lblAccountBalance = new Label();
        txtAccountBalance = new TextBox();
        dgvCustomers = new DataGridView();
        CustomerIdColumn = new DataGridViewTextBoxColumn();
        FirstNameColumn = new DataGridViewTextBoxColumn();
        LastNameColumn = new DataGridViewTextBoxColumn();
        PhoneColumn = new DataGridViewTextBoxColumn();
        IdentityColumn = new DataGridViewTextBoxColumn();
        BirthdayColumn = new DataGridViewTextBoxColumn();
        UsernameColumn = new DataGridViewTextBoxColumn();
        PasswordColumn = new DataGridViewTextBoxColumn();
        AccountBalanceColumn = new DataGridViewTextBoxColumn();
        customerButtons = new FlowLayoutPanel();
        btnAddCustomer = new Button();
        btnEditCustomer = new Button();
        btnDeleteCustomer = new Button();
        btnCancelCustomer = new Button();
        statusStrip = new StatusStrip();
        lblServerStatus = new ToolStripStatusLabel();
        mainTabs.SuspendLayout();
        tabMachines.SuspendLayout();
        machineLayout.SuspendLayout();
        machineActions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)machineSplit).BeginInit();
        machineSplit.Panel1.SuspendLayout();
        machineSplit.Panel2.SuspendLayout();
        machineSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvMachines).BeginInit();
        chatGroup.SuspendLayout();
        chatLayout.SuspendLayout();
        chatInputLayout.SuspendLayout();
        tabCustomers.SuspendLayout();
        customerLayout.SuspendLayout();
        customerTopLayout.SuspendLayout();
        customerInfoLayout.SuspendLayout();
        loginInfoLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvCustomers).BeginInit();
        customerButtons.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // mainTabs
        // 
        mainTabs.Controls.Add(tabMachines);
        mainTabs.Controls.Add(tabCustomers);
        mainTabs.Dock = DockStyle.Fill;
        mainTabs.Location = new Point(0, 0);
        mainTabs.Name = "mainTabs";
        mainTabs.SelectedIndex = 0;
        mainTabs.Size = new Size(884, 559);
        mainTabs.TabIndex = 0;
        // 
        // tabMachines
        // 
        tabMachines.Controls.Add(machineLayout);
        tabMachines.Location = new Point(4, 24);
        tabMachines.Name = "tabMachines";
        tabMachines.Padding = new Padding(8);
        tabMachines.Size = new Size(876, 531);
        tabMachines.TabIndex = 0;
        tabMachines.Text = "Quản lý máy";
        tabMachines.UseVisualStyleBackColor = true;
        // 
        // machineLayout
        // 
        machineLayout.ColumnCount = 1;
        machineLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        machineLayout.Controls.Add(lblMachineTitle, 0, 0);
        machineLayout.Controls.Add(pnlMachineCards, 0, 1);
        machineLayout.Controls.Add(machineActions, 0, 2);
        machineLayout.Controls.Add(machineSplit, 0, 3);
        machineLayout.Dock = DockStyle.Fill;
        machineLayout.Location = new Point(8, 8);
        machineLayout.Name = "machineLayout";
        machineLayout.RowCount = 4;
        machineLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        machineLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
        machineLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        machineLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        machineLayout.Size = new Size(860, 515);
        machineLayout.TabIndex = 0;
        // 
        // lblMachineTitle
        // 
        lblMachineTitle.Dock = DockStyle.Fill;
        lblMachineTitle.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
        lblMachineTitle.Location = new Point(3, 0);
        lblMachineTitle.Name = "lblMachineTitle";
        lblMachineTitle.Size = new Size(854, 60);
        lblMachineTitle.TabIndex = 0;
        lblMachineTitle.Text = "DANH SÁCH MÁY";
        lblMachineTitle.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // pnlMachineCards
        // 
        pnlMachineCards.AutoScroll = true;
        pnlMachineCards.BackColor = Color.FromArgb(245, 245, 245);
        pnlMachineCards.Dock = DockStyle.Fill;
        pnlMachineCards.Location = new Point(3, 63);
        pnlMachineCards.Name = "pnlMachineCards";
        pnlMachineCards.Padding = new Padding(18, 10, 18, 10);
        pnlMachineCards.Size = new Size(854, 244);
        pnlMachineCards.TabIndex = 1;
        // 
        // machineActions
        // 
        machineActions.Controls.Add(btnLockMachine);
        machineActions.Controls.Add(btnUnlockMachine);
        machineActions.Controls.Add(btnShutdownMachine);
        machineActions.Dock = DockStyle.Fill;
        machineActions.Location = new Point(3, 313);
        machineActions.Name = "machineActions";
        machineActions.Padding = new Padding(0, 7, 0, 0);
        machineActions.Size = new Size(854, 42);
        machineActions.TabIndex = 2;
        // 
        // btnLockMachine
        // 
        btnLockMachine.Location = new Point(3, 10);
        btnLockMachine.Name = "btnLockMachine";
        btnLockMachine.Size = new Size(120, 28);
        btnLockMachine.TabIndex = 0;
        btnLockMachine.Text = "Khóa máy";
        btnLockMachine.UseVisualStyleBackColor = true;
        btnLockMachine.Click += MachineAction_Click;
        // 
        // btnUnlockMachine
        // 
        btnUnlockMachine.Location = new Point(129, 10);
        btnUnlockMachine.Name = "btnUnlockMachine";
        btnUnlockMachine.Size = new Size(120, 28);
        btnUnlockMachine.TabIndex = 1;
        btnUnlockMachine.Text = "Mở khóa";
        btnUnlockMachine.UseVisualStyleBackColor = true;
        btnUnlockMachine.Click += MachineAction_Click;
        // 
        // btnShutdownMachine
        // 
        btnShutdownMachine.Location = new Point(255, 10);
        btnShutdownMachine.Name = "btnShutdownMachine";
        btnShutdownMachine.Size = new Size(120, 28);
        btnShutdownMachine.TabIndex = 2;
        btnShutdownMachine.Text = "Tắt máy";
        btnShutdownMachine.UseVisualStyleBackColor = true;
        btnShutdownMachine.Click += MachineAction_Click;
        // 
        // machineSplit
        // 
        machineSplit.Dock = DockStyle.Fill;
        machineSplit.Location = new Point(3, 361);
        machineSplit.Name = "machineSplit";
        // 
        // machineSplit.Panel1
        // 
        machineSplit.Panel1.Controls.Add(dgvMachines);
        // 
        // machineSplit.Panel2
        // 
        machineSplit.Panel2.Controls.Add(chatGroup);
        machineSplit.Size = new Size(854, 151);
        machineSplit.SplitterDistance = 544;
        machineSplit.TabIndex = 3;
        // 
        // dgvMachines
        // 
        dgvMachines.AllowUserToAddRows = false;
        dgvMachines.AllowUserToDeleteRows = false;
        dgvMachines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvMachines.BackgroundColor = Color.White;
        dgvMachines.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvMachines.Columns.AddRange(new DataGridViewColumn[] { MaMayColumn, SoMayColumn, TinhTrangColumn, MachineNameColumn });
        dgvMachines.Dock = DockStyle.Fill;
        dgvMachines.Location = new Point(0, 0);
        dgvMachines.MultiSelect = false;
        dgvMachines.Name = "dgvMachines";
        dgvMachines.ReadOnly = true;
        dgvMachines.RowHeadersWidth = 32;
        dgvMachines.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvMachines.Size = new Size(544, 151);
        dgvMachines.TabIndex = 0;
        dgvMachines.SelectionChanged += DgvMachines_SelectionChanged;
        // 
        // MaMayColumn
        // 
        MaMayColumn.HeaderText = "Mã máy";
        MaMayColumn.Name = "MaMayColumn";
        MaMayColumn.ReadOnly = true;
        // 
        // SoMayColumn
        // 
        SoMayColumn.HeaderText = "Số máy";
        SoMayColumn.Name = "SoMayColumn";
        SoMayColumn.ReadOnly = true;
        // 
        // TinhTrangColumn
        // 
        TinhTrangColumn.HeaderText = "Tình trạng";
        TinhTrangColumn.Name = "TinhTrangColumn";
        TinhTrangColumn.ReadOnly = true;
        // 
        // MachineNameColumn
        // 
        MachineNameColumn.HeaderText = "Mã máy";
        MachineNameColumn.Name = "MachineNameColumn";
        MachineNameColumn.ReadOnly = true;
        MachineNameColumn.Visible = false;
        // 
        // chatGroup
        // 
        chatGroup.Controls.Add(chatLayout);
        chatGroup.Dock = DockStyle.Fill;
        chatGroup.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        chatGroup.Location = new Point(0, 0);
        chatGroup.Name = "chatGroup";
        chatGroup.Padding = new Padding(10);
        chatGroup.Size = new Size(306, 151);
        chatGroup.TabIndex = 0;
        chatGroup.TabStop = false;
        chatGroup.Text = "Chat máy";
        // 
        // chatLayout
        // 
        chatLayout.ColumnCount = 1;
        chatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        chatLayout.Controls.Add(lblSelectedClient, 0, 0);
        chatLayout.Controls.Add(txtChatHistory, 0, 1);
        chatLayout.Controls.Add(chatInputLayout, 0, 2);
        chatLayout.Dock = DockStyle.Fill;
        chatLayout.Location = new Point(10, 26);
        chatLayout.Name = "chatLayout";
        chatLayout.RowCount = 3;
        chatLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        chatLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        chatLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        chatLayout.Size = new Size(286, 115);
        chatLayout.TabIndex = 0;
        // 
        // lblSelectedClient
        // 
        lblSelectedClient.Dock = DockStyle.Fill;
        lblSelectedClient.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblSelectedClient.Location = new Point(3, 0);
        lblSelectedClient.Name = "lblSelectedClient";
        lblSelectedClient.Size = new Size(280, 28);
        lblSelectedClient.TabIndex = 0;
        lblSelectedClient.Text = "Chat với PC01";
        lblSelectedClient.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtChatHistory
        // 
        txtChatHistory.Dock = DockStyle.Fill;
        txtChatHistory.Location = new Point(3, 31);
        txtChatHistory.Multiline = true;
        txtChatHistory.Name = "txtChatHistory";
        txtChatHistory.ReadOnly = true;
        txtChatHistory.ScrollBars = ScrollBars.Vertical;
        txtChatHistory.Size = new Size(280, 41);
        txtChatHistory.TabIndex = 1;
        // 
        // chatInputLayout
        // 
        chatInputLayout.ColumnCount = 2;
        chatInputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        chatInputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
        chatInputLayout.Controls.Add(txtChatMessage, 0, 0);
        chatInputLayout.Controls.Add(btnSendChat, 1, 0);
        chatInputLayout.Dock = DockStyle.Fill;
        chatInputLayout.Location = new Point(3, 78);
        chatInputLayout.Name = "chatInputLayout";
        chatInputLayout.RowCount = 1;
        chatInputLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        chatInputLayout.Size = new Size(280, 34);
        chatInputLayout.TabIndex = 2;
        // 
        // txtChatMessage
        // 
        txtChatMessage.Dock = DockStyle.Fill;
        txtChatMessage.Location = new Point(3, 3);
        txtChatMessage.Name = "txtChatMessage";
        txtChatMessage.PlaceholderText = "Nhập tin nhắn...";
        txtChatMessage.Size = new Size(192, 23);
        txtChatMessage.TabIndex = 0;
        // 
        // btnSendChat
        // 
        btnSendChat.Dock = DockStyle.Fill;
        btnSendChat.Location = new Point(201, 3);
        btnSendChat.Name = "btnSendChat";
        btnSendChat.Size = new Size(76, 28);
        btnSendChat.TabIndex = 1;
        btnSendChat.Text = "Gửi";
        btnSendChat.UseVisualStyleBackColor = true;
        btnSendChat.Click += BtnSendChat_Click;
        // 
        // tabCustomers
        // 
        tabCustomers.Controls.Add(customerLayout);
        tabCustomers.Location = new Point(4, 24);
        tabCustomers.Name = "tabCustomers";
        tabCustomers.Padding = new Padding(14);
        tabCustomers.Size = new Size(876, 531);
        tabCustomers.TabIndex = 1;
        tabCustomers.Text = "Quản lý khách hàng";
        tabCustomers.UseVisualStyleBackColor = true;
        // 
        // customerLayout
        // 
        customerLayout.ColumnCount = 1;
        customerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        customerLayout.Controls.Add(customerTopLayout, 0, 0);
        customerLayout.Controls.Add(dgvCustomers, 0, 1);
        customerLayout.Controls.Add(customerButtons, 0, 2);
        customerLayout.Dock = DockStyle.Fill;
        customerLayout.Location = new Point(14, 14);
        customerLayout.Name = "customerLayout";
        customerLayout.RowCount = 3;
        customerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
        customerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        customerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        customerLayout.Size = new Size(848, 503);
        customerLayout.TabIndex = 0;
        // 
        // customerTopLayout
        // 
        customerTopLayout.ColumnCount = 2;
        customerTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        customerTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        customerTopLayout.Controls.Add(customerInfoLayout, 0, 0);
        customerTopLayout.Controls.Add(loginInfoLayout, 1, 0);
        customerTopLayout.Dock = DockStyle.Fill;
        customerTopLayout.Location = new Point(3, 3);
        customerTopLayout.Name = "customerTopLayout";
        customerTopLayout.RowCount = 1;
        customerTopLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        customerTopLayout.Size = new Size(842, 184);
        customerTopLayout.TabIndex = 0;
        // 
        // customerInfoLayout
        // 
        customerInfoLayout.ColumnCount = 4;
        customerInfoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94F));
        customerInfoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        customerInfoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
        customerInfoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        customerInfoLayout.Controls.Add(lblCustomerId, 0, 0);
        customerInfoLayout.Controls.Add(txtCustomerId, 1, 0);
        customerInfoLayout.Controls.Add(lblFirstName, 0, 1);
        customerInfoLayout.Controls.Add(txtFirstName, 1, 1);
        customerInfoLayout.Controls.Add(lblLastName, 2, 1);
        customerInfoLayout.Controls.Add(txtLastName, 3, 1);
        customerInfoLayout.Controls.Add(lblPhone, 0, 2);
        customerInfoLayout.Controls.Add(txtPhone, 1, 2);
        customerInfoLayout.Controls.Add(lblIdentity, 0, 3);
        customerInfoLayout.Controls.Add(txtIdentity, 1, 3);
        customerInfoLayout.Controls.Add(lblBirthday, 0, 4);
        customerInfoLayout.Controls.Add(txtBirthday, 1, 4);
        customerInfoLayout.Dock = DockStyle.Fill;
        customerInfoLayout.Location = new Point(3, 3);
        customerInfoLayout.Name = "customerInfoLayout";
        customerInfoLayout.RowCount = 5;
        customerInfoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        customerInfoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        customerInfoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        customerInfoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        customerInfoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        customerInfoLayout.Size = new Size(482, 178);
        customerInfoLayout.TabIndex = 0;
        // 
        // lblCustomerId
        // 
        lblCustomerId.Dock = DockStyle.Fill;
        lblCustomerId.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblCustomerId.Location = new Point(3, 0);
        lblCustomerId.Name = "lblCustomerId";
        lblCustomerId.Size = new Size(88, 34);
        lblCustomerId.TabIndex = 0;
        lblCustomerId.Text = "Mã KH";
        lblCustomerId.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtCustomerId
        // 
        txtCustomerId.Dock = DockStyle.Fill;
        txtCustomerId.Location = new Point(97, 3);
        txtCustomerId.Name = "txtCustomerId";
        txtCustomerId.Size = new Size(158, 23);
        txtCustomerId.TabIndex = 1;
        // 
        // lblFirstName
        // 
        lblFirstName.Dock = DockStyle.Fill;
        lblFirstName.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblFirstName.Location = new Point(3, 34);
        lblFirstName.Name = "lblFirstName";
        lblFirstName.Size = new Size(88, 34);
        lblFirstName.TabIndex = 2;
        lblFirstName.Text = "Họ";
        lblFirstName.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtFirstName
        // 
        txtFirstName.Dock = DockStyle.Fill;
        txtFirstName.Location = new Point(97, 37);
        txtFirstName.Name = "txtFirstName";
        txtFirstName.Size = new Size(158, 23);
        txtFirstName.TabIndex = 3;
        // 
        // lblLastName
        // 
        lblLastName.Dock = DockStyle.Fill;
        lblLastName.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblLastName.Location = new Point(261, 34);
        lblLastName.Name = "lblLastName";
        lblLastName.Size = new Size(54, 34);
        lblLastName.TabIndex = 4;
        lblLastName.Text = "Tên";
        lblLastName.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtLastName
        // 
        txtLastName.Dock = DockStyle.Fill;
        txtLastName.Location = new Point(321, 37);
        txtLastName.Name = "txtLastName";
        txtLastName.Size = new Size(158, 23);
        txtLastName.TabIndex = 5;
        // 
        // lblPhone
        // 
        lblPhone.Dock = DockStyle.Fill;
        lblPhone.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblPhone.Location = new Point(3, 68);
        lblPhone.Name = "lblPhone";
        lblPhone.Size = new Size(88, 34);
        lblPhone.TabIndex = 6;
        lblPhone.Text = "SĐT";
        lblPhone.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtPhone
        // 
        txtPhone.Dock = DockStyle.Fill;
        txtPhone.Location = new Point(97, 71);
        txtPhone.Name = "txtPhone";
        txtPhone.Size = new Size(158, 23);
        txtPhone.TabIndex = 7;
        // 
        // lblIdentity
        // 
        lblIdentity.Dock = DockStyle.Fill;
        lblIdentity.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblIdentity.Location = new Point(3, 102);
        lblIdentity.Name = "lblIdentity";
        lblIdentity.Size = new Size(88, 34);
        lblIdentity.TabIndex = 8;
        lblIdentity.Text = "Số CMND";
        lblIdentity.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtIdentity
        // 
        txtIdentity.Dock = DockStyle.Fill;
        txtIdentity.Location = new Point(97, 105);
        txtIdentity.Name = "txtIdentity";
        txtIdentity.Size = new Size(158, 23);
        txtIdentity.TabIndex = 9;
        // 
        // lblBirthday
        // 
        lblBirthday.Dock = DockStyle.Fill;
        lblBirthday.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblBirthday.Location = new Point(3, 136);
        lblBirthday.Name = "lblBirthday";
        lblBirthday.Size = new Size(88, 42);
        lblBirthday.TabIndex = 10;
        lblBirthday.Text = "Ngày sinh";
        lblBirthday.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtBirthday
        // 
        txtBirthday.Dock = DockStyle.Fill;
        txtBirthday.Location = new Point(97, 139);
        txtBirthday.Name = "txtBirthday";
        txtBirthday.Size = new Size(158, 23);
        txtBirthday.TabIndex = 11;
        // 
        // loginInfoLayout
        // 
        loginInfoLayout.ColumnCount = 2;
        loginInfoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        loginInfoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        loginInfoLayout.Controls.Add(lblLoginTitle, 0, 0);
        loginInfoLayout.Controls.Add(lblUsername, 0, 1);
        loginInfoLayout.Controls.Add(txtUsername, 1, 1);
        loginInfoLayout.Controls.Add(lblPassword, 0, 2);
        loginInfoLayout.Controls.Add(txtPassword, 1, 2);
        loginInfoLayout.Controls.Add(lblAccountBalance, 0, 3);
        loginInfoLayout.Controls.Add(txtAccountBalance, 1, 3);
        loginInfoLayout.Dock = DockStyle.Fill;
        loginInfoLayout.Location = new Point(491, 3);
        loginInfoLayout.Name = "loginInfoLayout";
        loginInfoLayout.RowCount = 4;
        loginInfoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        loginInfoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        loginInfoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        loginInfoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        loginInfoLayout.Size = new Size(348, 178);
        loginInfoLayout.TabIndex = 1;
        // 
        // lblLoginTitle
        // 
        loginInfoLayout.SetColumnSpan(lblLoginTitle, 2);
        lblLoginTitle.Dock = DockStyle.Fill;
        lblLoginTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblLoginTitle.Location = new Point(3, 0);
        lblLoginTitle.Name = "lblLoginTitle";
        lblLoginTitle.Size = new Size(342, 45);
        lblLoginTitle.TabIndex = 0;
        lblLoginTitle.Text = "THÔNG TIN ĐĂNG NHẬP";
        lblLoginTitle.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // lblUsername
        // 
        lblUsername.Dock = DockStyle.Fill;
        lblUsername.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblUsername.Location = new Point(3, 45);
        lblUsername.Name = "lblUsername";
        lblUsername.Size = new Size(114, 36);
        lblUsername.TabIndex = 1;
        lblUsername.Text = "Tên đăng nhập";
        lblUsername.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtUsername
        // 
        txtUsername.Dock = DockStyle.Fill;
        txtUsername.Location = new Point(123, 48);
        txtUsername.Name = "txtUsername";
        txtUsername.Size = new Size(222, 23);
        txtUsername.TabIndex = 2;
        // 
        // lblPassword
        // 
        lblPassword.Dock = DockStyle.Fill;
        lblPassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblPassword.Location = new Point(3, 81);
        lblPassword.Name = "lblPassword";
        lblPassword.Size = new Size(114, 36);
        lblPassword.TabIndex = 3;
        lblPassword.Text = "Mật khẩu";
        lblPassword.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtPassword
        // 
        txtPassword.Dock = DockStyle.Fill;
        txtPassword.Location = new Point(123, 84);
        txtPassword.Name = "txtPassword";
        txtPassword.PasswordChar = '*';
        txtPassword.Size = new Size(222, 23);
        txtPassword.TabIndex = 4;
        // 
        // lblAccountBalance
        // 
        lblAccountBalance.Dock = DockStyle.Fill;
        lblAccountBalance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblAccountBalance.Location = new Point(3, 117);
        lblAccountBalance.Name = "lblAccountBalance";
        lblAccountBalance.Size = new Size(114, 61);
        lblAccountBalance.TabIndex = 5;
        lblAccountBalance.Text = "Tài khoản";
        lblAccountBalance.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtAccountBalance
        // 
        txtAccountBalance.Dock = DockStyle.Fill;
        txtAccountBalance.Location = new Point(123, 120);
        txtAccountBalance.Name = "txtAccountBalance";
        txtAccountBalance.Size = new Size(222, 23);
        txtAccountBalance.TabIndex = 6;
        // 
        // dgvCustomers
        // 
        dgvCustomers.AllowUserToAddRows = false;
        dgvCustomers.AllowUserToDeleteRows = false;
        dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvCustomers.BackgroundColor = Color.White;
        dgvCustomers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvCustomers.Columns.AddRange(new DataGridViewColumn[] { CustomerIdColumn, FirstNameColumn, LastNameColumn, PhoneColumn, IdentityColumn, BirthdayColumn, UsernameColumn, PasswordColumn, AccountBalanceColumn });
        dgvCustomers.Dock = DockStyle.Fill;
        dgvCustomers.Location = new Point(3, 193);
        dgvCustomers.MultiSelect = false;
        dgvCustomers.Name = "dgvCustomers";
        dgvCustomers.ReadOnly = true;
        dgvCustomers.RowHeadersWidth = 32;
        dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvCustomers.Size = new Size(842, 237);
        dgvCustomers.TabIndex = 1;
        // 
        // CustomerIdColumn
        // 
        CustomerIdColumn.HeaderText = "Mã KH";
        CustomerIdColumn.Name = "CustomerIdColumn";
        CustomerIdColumn.ReadOnly = true;
        // 
        // FirstNameColumn
        // 
        FirstNameColumn.HeaderText = "Tên KH";
        FirstNameColumn.Name = "FirstNameColumn";
        FirstNameColumn.ReadOnly = true;
        // 
        // LastNameColumn
        // 
        LastNameColumn.HeaderText = "Họ KH";
        LastNameColumn.Name = "LastNameColumn";
        LastNameColumn.ReadOnly = true;
        // 
        // PhoneColumn
        // 
        PhoneColumn.HeaderText = "SĐT";
        PhoneColumn.Name = "PhoneColumn";
        PhoneColumn.ReadOnly = true;
        // 
        // IdentityColumn
        // 
        IdentityColumn.HeaderText = "Số CMND";
        IdentityColumn.Name = "IdentityColumn";
        IdentityColumn.ReadOnly = true;
        // 
        // BirthdayColumn
        // 
        BirthdayColumn.HeaderText = "Ngày sinh";
        BirthdayColumn.Name = "BirthdayColumn";
        BirthdayColumn.ReadOnly = true;
        // 
        // UsernameColumn
        // 
        UsernameColumn.HeaderText = "Tên đăng nhập";
        UsernameColumn.Name = "UsernameColumn";
        UsernameColumn.ReadOnly = true;
        // 
        // PasswordColumn
        // 
        PasswordColumn.HeaderText = "Mật khẩu";
        PasswordColumn.Name = "PasswordColumn";
        PasswordColumn.ReadOnly = true;
        // 
        // AccountBalanceColumn
        // 
        AccountBalanceColumn.HeaderText = "Tài khoản";
        AccountBalanceColumn.Name = "AccountBalanceColumn";
        AccountBalanceColumn.ReadOnly = true;
        // 
        // customerButtons
        // 
        customerButtons.Controls.Add(btnAddCustomer);
        customerButtons.Controls.Add(btnEditCustomer);
        customerButtons.Controls.Add(btnDeleteCustomer);
        customerButtons.Controls.Add(btnCancelCustomer);
        customerButtons.Dock = DockStyle.Fill;
        customerButtons.Location = new Point(3, 436);
        customerButtons.Name = "customerButtons";
        customerButtons.Padding = new Padding(0, 12, 0, 0);
        customerButtons.Size = new Size(842, 64);
        customerButtons.TabIndex = 2;
        // 
        // btnAddCustomer
        // 
        btnAddCustomer.Location = new Point(10, 12);
        btnAddCustomer.Margin = new Padding(10, 0, 60, 0);
        btnAddCustomer.Name = "btnAddCustomer";
        btnAddCustomer.Size = new Size(160, 42);
        btnAddCustomer.TabIndex = 0;
        btnAddCustomer.Text = "Thêm";
        btnAddCustomer.UseVisualStyleBackColor = true;
        btnAddCustomer.Click += CustomerAction_Click;
        // 
        // btnEditCustomer
        // 
        btnEditCustomer.Location = new Point(230, 12);
        btnEditCustomer.Margin = new Padding(0, 0, 60, 0);
        btnEditCustomer.Name = "btnEditCustomer";
        btnEditCustomer.Size = new Size(160, 42);
        btnEditCustomer.TabIndex = 1;
        btnEditCustomer.Text = "Sửa";
        btnEditCustomer.UseVisualStyleBackColor = true;
        btnEditCustomer.Click += CustomerAction_Click;
        // 
        // btnDeleteCustomer
        // 
        btnDeleteCustomer.Location = new Point(450, 12);
        btnDeleteCustomer.Margin = new Padding(0, 0, 60, 0);
        btnDeleteCustomer.Name = "btnDeleteCustomer";
        btnDeleteCustomer.Size = new Size(160, 42);
        btnDeleteCustomer.TabIndex = 2;
        btnDeleteCustomer.Text = "Xóa";
        btnDeleteCustomer.UseVisualStyleBackColor = true;
        btnDeleteCustomer.Click += CustomerAction_Click;
        // 
        // btnCancelCustomer
        // 
        btnCancelCustomer.Location = new Point(673, 15);
        btnCancelCustomer.Name = "btnCancelCustomer";
        btnCancelCustomer.Size = new Size(160, 42);
        btnCancelCustomer.TabIndex = 3;
        btnCancelCustomer.Text = "Hủy";
        btnCancelCustomer.UseVisualStyleBackColor = true;
        btnCancelCustomer.Click += CustomerAction_Click;
        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new ToolStripItem[] { lblServerStatus });
        statusStrip.Location = new Point(0, 559);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(884, 22);
        statusStrip.TabIndex = 1;
        // 
        // lblServerStatus
        // 
        lblServerStatus.Name = "lblServerStatus";
        lblServerStatus.Size = new Size(207, 17);
        lblServerStatus.Text = "Máy chủ: đang thiết kế giao diện mẫu";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(884, 581);
        Controls.Add(mainTabs);
        Controls.Add(statusStrip);
        MaximizeBox = false;
        MinimumSize = new Size(900, 620);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MÁY CHỦ";
        Load += MainForm_Load;
        mainTabs.ResumeLayout(false);
        tabMachines.ResumeLayout(false);
        machineLayout.ResumeLayout(false);
        machineActions.ResumeLayout(false);
        machineSplit.Panel1.ResumeLayout(false);
        machineSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)machineSplit).EndInit();
        machineSplit.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvMachines).EndInit();
        chatGroup.ResumeLayout(false);
        chatLayout.ResumeLayout(false);
        chatLayout.PerformLayout();
        chatInputLayout.ResumeLayout(false);
        chatInputLayout.PerformLayout();
        tabCustomers.ResumeLayout(false);
        customerLayout.ResumeLayout(false);
        customerTopLayout.ResumeLayout(false);
        customerInfoLayout.ResumeLayout(false);
        customerInfoLayout.PerformLayout();
        loginInfoLayout.ResumeLayout(false);
        loginInfoLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)dgvCustomers).EndInit();
        customerButtons.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TabControl mainTabs;
    private TabPage tabMachines;
    private TabPage tabCustomers;
    private TableLayoutPanel machineLayout;
    private Label lblMachineTitle;
    private FlowLayoutPanel pnlMachineCards;
    private SplitContainer machineSplit;
    private DataGridView dgvMachines;
    private DataGridViewTextBoxColumn MaMayColumn;
    private DataGridViewTextBoxColumn SoMayColumn;
    private DataGridViewTextBoxColumn TinhTrangColumn;
    private DataGridViewTextBoxColumn MachineNameColumn;
    private GroupBox chatGroup;
    private TableLayoutPanel chatLayout;
    private Label lblSelectedClient;
    private TextBox txtChatHistory;
    private TableLayoutPanel chatInputLayout;
    private TextBox txtChatMessage;
    private Button btnSendChat;
    private FlowLayoutPanel machineActions;
    private Button btnLockMachine;
    private Button btnUnlockMachine;
    private Button btnShutdownMachine;
    private TableLayoutPanel customerLayout;
    private TableLayoutPanel customerTopLayout;
    private TableLayoutPanel customerInfoLayout;
    private Label lblCustomerId;
    private TextBox txtCustomerId;
    private Label lblFirstName;
    private TextBox txtFirstName;
    private Label lblPhone;
    private TextBox txtPhone;
    private Label lblIdentity;
    private TextBox txtIdentity;
    private Label lblBirthday;
    private TextBox txtBirthday;
    private TableLayoutPanel loginInfoLayout;
    private Label lblLoginTitle;
    private Label lblUsername;
    private TextBox txtUsername;
    private Label lblPassword;
    private TextBox txtPassword;
    private Label lblAccountBalance;
    private TextBox txtAccountBalance;
    private Label lblLastName;
    private TextBox txtLastName;
    private DataGridView dgvCustomers;
    private DataGridViewTextBoxColumn CustomerIdColumn;
    private DataGridViewTextBoxColumn FirstNameColumn;
    private DataGridViewTextBoxColumn LastNameColumn;
    private DataGridViewTextBoxColumn PhoneColumn;
    private DataGridViewTextBoxColumn IdentityColumn;
    private DataGridViewTextBoxColumn BirthdayColumn;
    private DataGridViewTextBoxColumn UsernameColumn;
    private DataGridViewTextBoxColumn PasswordColumn;
    private DataGridViewTextBoxColumn AccountBalanceColumn;
    private FlowLayoutPanel customerButtons;
    private Button btnAddCustomer;
    private Button btnEditCustomer;
    private Button btnDeleteCustomer;
    private Button btnCancelCustomer;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblServerStatus;
}
