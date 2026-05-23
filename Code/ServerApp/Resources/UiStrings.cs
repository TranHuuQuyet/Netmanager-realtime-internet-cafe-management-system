using System.Globalization;
using System.Resources;

namespace ServerApp;

internal static class UiStrings
{
    private static readonly ResourceManager ResourceManager = new("ServerApp.Resources.UiStrings", typeof(UiStrings).Assembly);

    private static string Get(string name, string fallback)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? fallback;
    }

    public static string LoginTitle => Get(nameof(LoginTitle), "ĐĂNG NHẬP MÁY CHỦ");
    public static string LoginUsernameLabel => Get(nameof(LoginUsernameLabel), "Tài khoản");
    public static string LoginPasswordLabel => Get(nameof(LoginPasswordLabel), "Mật khẩu");
    public static string LoginMachineIdLabel => Get(nameof(LoginMachineIdLabel), "Mã máy");
    public static string LoginButtonText => Get(nameof(LoginButtonText), "Đăng nhập");
    public static string ExitButtonText => Get(nameof(ExitButtonText), "Thoát");
    public static string LoginFormTitle => Get(nameof(LoginFormTitle), "Đăng nhập");

    public static string LoginUsernameRequired => Get(nameof(LoginUsernameRequired), "Vui lòng nhập tài khoản.");
    public static string LoginPasswordRequired => Get(nameof(LoginPasswordRequired), "Vui lòng nhập mật khẩu.");
    public static string LoginMachineIdRequired => Get(nameof(LoginMachineIdRequired), "Vui lòng nhập mã máy.");

    public static string MainTabMachines => Get(nameof(MainTabMachines), "Quản lý máy");
    public static string MainMachineTitle => Get(nameof(MainMachineTitle), "DANH SÁCH MÁY");
    public static string MainLockMachine => Get(nameof(MainLockMachine), "Khóa máy");
    public static string MainUnlockMachine => Get(nameof(MainUnlockMachine), "Mở khóa");
    public static string MainShutdownMachine => Get(nameof(MainShutdownMachine), "Tắt máy");
    public static string MainChatGroup => Get(nameof(MainChatGroup), "Chat máy");
    public static string MainChatSelectedClientDefault => Get(nameof(MainChatSelectedClientDefault), "Chat với PC01");
    public static string MainTabCustomers => Get(nameof(MainTabCustomers), "Quản lý khách hàng");
    public static string MainCustomerIdLabel => Get(nameof(MainCustomerIdLabel), "Mã KH");
    public static string MainFirstNameLabel => Get(nameof(MainFirstNameLabel), "Họ");
    public static string MainLastNameLabel => Get(nameof(MainLastNameLabel), "Tên");
    public static string MainPhoneLabel => Get(nameof(MainPhoneLabel), "SĐT");
    public static string MainIdentityLabel => Get(nameof(MainIdentityLabel), "Số CMND");
    public static string MainBirthdayLabel => Get(nameof(MainBirthdayLabel), "Ngày sinh");
    public static string MainLoginTitle => Get(nameof(MainLoginTitle), "THÔNG TIN ĐĂNG NHẬP");
    public static string MainUsernameLabel => Get(nameof(MainUsernameLabel), "Tên đăng nhập");
    public static string MainPasswordLabel => Get(nameof(MainPasswordLabel), "Mật khẩu");
    public static string MainAccountBalanceLabel => Get(nameof(MainAccountBalanceLabel), "Tài khoản");
    public static string MainMachineColumn => Get(nameof(MainMachineColumn), "Mã máy");
    public static string MainSeatColumn => Get(nameof(MainSeatColumn), "Số máy");
    public static string MainStatusColumn => Get(nameof(MainStatusColumn), "Tình trạng");
    public static string MainCustomerIdColumn => Get(nameof(MainCustomerIdColumn), "Mã KH");
    public static string MainCustomerFirstNameColumn => Get(nameof(MainCustomerFirstNameColumn), "Tên KH");
    public static string MainCustomerLastNameColumn => Get(nameof(MainCustomerLastNameColumn), "Họ KH");
    public static string MainCustomerPhoneColumn => Get(nameof(MainCustomerPhoneColumn), "SĐT");
    public static string MainCustomerIdentityColumn => Get(nameof(MainCustomerIdentityColumn), "Số CMND");
    public static string MainCustomerBirthdayColumn => Get(nameof(MainCustomerBirthdayColumn), "Ngày sinh");
    public static string MainCustomerUsernameColumn => Get(nameof(MainCustomerUsernameColumn), "Tên đăng nhập");
    public static string MainCustomerPasswordColumn => Get(nameof(MainCustomerPasswordColumn), "Mật khẩu");
    public static string MainCustomerAccountColumn => Get(nameof(MainCustomerAccountColumn), "Tài khoản");
    public static string MainAddCustomerButton => Get(nameof(MainAddCustomerButton), "Thêm");
    public static string MainEditCustomerButton => Get(nameof(MainEditCustomerButton), "Sửa");
    public static string MainDeleteCustomerButton => Get(nameof(MainDeleteCustomerButton), "Xóa");
    public static string MainCancelCustomerButton => Get(nameof(MainCancelCustomerButton), "Hủy");
    public static string MainServerStatus => Get(nameof(MainServerStatus), "Máy chủ: đang thiết kế giao diện mẫu");
    public static string MainFormTitle => Get(nameof(MainFormTitle), "MÁY CHỦ");
    public static string MainChatPlaceholder => Get(nameof(MainChatPlaceholder), "Nhập tin nhắn...");
    public static string MainSendButton => Get(nameof(MainSendButton), "Gửi");
    public static string ChatWithMachineTemplate => Get(nameof(ChatWithMachineTemplate), "Chat với máy {0}");
    public static string ChatHistoryTemplate => Get(nameof(ChatHistoryTemplate), "[{0}] Sẵn sàng nhận tin nhắn từ máy chủ.");
    public static string ServerPrefix => Get(nameof(ServerPrefix), "Máy chủ");
    public static string MainSelectedMachineStatusTemplate => Get(nameof(MainSelectedMachineStatusTemplate), "Đang chọn máy {0}.");
    public static string MainNoMachineSelectedStatus => Get(nameof(MainNoMachineSelectedStatus), "Vui lòng chọn một máy trước.");
    public static string MainActionPendingTemplate => Get(nameof(MainActionPendingTemplate), "{0} cho {1}: đang chờ backend.");
    public static string MainCustomerActionPendingTemplate => Get(nameof(MainCustomerActionPendingTemplate), "{0} khách hàng: đang chờ backend.");
    public static string MainPendingAction => Get(nameof(MainPendingAction), "Thao tác");
}
