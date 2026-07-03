using ServerApp.Database.Models;

// Namespace chua cac interface truy cap du lieu database.
namespace ServerApp.Database.Contracts;

// Interface mo ta cac thao tac CRUD cho khach hang.
public interface ICustomerRepository
{
    // Lay danh sach tat ca khach hang.
    Task<IReadOnlyList<CustomerRecord>> ListAsync(
        CancellationToken cancellationToken = default);

    // Tim khach hang theo CustomerId.
    Task<CustomerRecord?> GetByIdAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    // Tim khach hang theo username.
    Task<CustomerRecord?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    // Them khach hang moi.
    Task AddAsync(
        CustomerRecord customer,
        CancellationToken cancellationToken = default);

    // Cap nhat thong tin khach hang.
    Task UpdateAsync(
        CustomerRecord customer,
        CancellationToken cancellationToken = default);

    // Xoa khach hang theo Id.
    Task DeleteAsync(
        string customerId,
        CancellationToken cancellationToken = default);
}
