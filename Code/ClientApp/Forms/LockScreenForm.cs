namespace ClientApp.Forms;

public sealed class LockScreenForm : Form
{
    // Chỉ lệnh UNLOCK từ server mới đặt cờ này. Handler FormClosing dựa vào cờ để
    // phân biệt đóng hợp lệ với thao tác đóng của người dùng.
    private bool _unlockedByServer;

    // Toàn bộ giao diện khóa được tạo bằng code vì form nhỏ và không cần Designer.
    public LockScreenForm()
    {
        // Cửa sổ không có nút thu nhỏ/phóng to/đóng, luôn nổi trên form cha và mở ở
        // giữa máy trạm để người dùng thấy rõ trạng thái bị khóa.
        Text = "Máy trạm bị khóa";
        ClientSize = new Size(258, 190);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        StartPosition = FormStartPosition.CenterParent;
        TopMost = true;
        Padding = new Padding(12);

        // Layout ba hàng: tiêu đề cố định, nội dung co giãn và trạng thái chờ cố định.
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

        // Hàng đầu hiển thị cảnh báo khóa bằng font đậm.
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "MÁY ĐANG BỊ KHÓA",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 0);

        // Hàng giữa giải thích lý do và hướng dẫn người dùng liên hệ quầy quản trị.
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Máy trạm đã bị quản trị viên khóa.\nVui lòng liên hệ quầy để tiếp tục sử dụng.",
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 1);

        // Hàng cuối cho biết client đang chờ packet UNLOCK từ server.
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Đang chờ lệnh mở khóa từ máy chủ.",
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 2);

        Controls.Add(layout);

        // Chặn yêu cầu đóng từ người dùng trong khi server chưa mở khóa.
        FormClosing += LockScreenForm_FormClosing;
    }

    // ClientMainForm gọi hàm này sau khi nhận UNLOCK. Hàm tự chuyển về UI thread,
    // đánh dấu thao tác đóng hợp lệ rồi mới đóng form.
    public void UnlockFromServer()
    {
        // Packet có thể được xử lý trên worker thread nên không đóng form trực tiếp.
        if (InvokeRequired)
        {
            BeginInvoke(UnlockFromServer);
            return;
        }

        // Đặt cờ trước Close để sự kiện FormClosing không hủy thao tác.
        _unlockedByServer = true;
        Close();
    }

    // Chỉ hủy thao tác UserClosing khi chưa nhận UNLOCK. Các lý do đóng do vòng đời
    // ứng dụng vẫn được phép để chương trình có thể thoát sạch.
    private void LockScreenForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_unlockedByServer && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
        }
    }
}
