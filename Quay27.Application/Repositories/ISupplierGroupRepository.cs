using Quay27.Application.Suppliers;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ISupplierGroupRepository
{
    Task<SupplierGroup?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierGroupDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default);
    Task AddAsync(SupplierGroup group, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, string updatedBy, CancellationToken cancellationToken = default);
}
