using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ICustomerGroupRepository
{
    Task<IReadOnlyList<CustomerGroup>> ListAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerGroup>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<CustomerGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerGroup?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerGroup group, CancellationToken cancellationToken = default);
}
