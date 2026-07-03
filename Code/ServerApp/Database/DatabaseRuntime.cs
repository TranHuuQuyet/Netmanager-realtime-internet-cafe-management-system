using ServerApp.Database.Contracts;

// Namespace cua tang Database.
namespace ServerApp.Database;

// Goi cac repository dang duoc app su dung tai runtime.
// Thay vi truyen tung repository rieng le, app co the truyen mot DatabaseRuntime.
public sealed record DatabaseRuntime(
    // Repository quan ly user dang nhap/tai khoan.
    IUserRepository Users,

    // Repository quan ly session xac thuc.
    ISessionRepository Sessions,

    // Repository quan ly may tram.
    IMachineRepository Machines,

    // Repository quan ly phien tinh tien.
    IBillingSessionRepository BillingSessions,

    // Repository quan ly thong tin khach hang.
    ICustomerRepository Customers);
