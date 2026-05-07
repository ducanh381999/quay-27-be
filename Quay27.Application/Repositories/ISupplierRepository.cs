using Quay27.Application.Suppliers;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ISupplierRepository
{
    Task<Supplier?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Supplier?> GetTrackedByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetProjectedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierDto>> ListAsync(ListSuppliersQuery query, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<string?> GetMaxNumericCodeSuffixAsync(string prefix, CancellationToken cancellationToken = default);
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, string updatedBy, CancellationToken cancellationToken = default);
    Task<bool> HasSuppliersInGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
}
