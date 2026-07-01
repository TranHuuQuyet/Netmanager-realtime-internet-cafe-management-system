using ServerApp.Database.Models;

namespace ServerApp.Database.Contracts;

public interface ICustomerRepository
{
    Task<IReadOnlyList<CustomerRecord>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<CustomerRecord?> GetByIdAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerRecord?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CustomerRecord customer,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        CustomerRecord customer,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string customerId,
        CancellationToken cancellationToken = default);
}
