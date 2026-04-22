using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ICustomerProfileRepository
{
    Task<IReadOnlyList<CustomerProfile>> ListAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<CustomerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerProfile?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CustomerCodeExistsAsync(string customerCode, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default);
}
