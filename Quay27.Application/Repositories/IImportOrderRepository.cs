using Quay27.Application.Purchasing;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IImportOrderRepository
{
    Task AddAsync(ImportOrder entity, CancellationToken cancellationToken = default);
    Task<ImportOrder?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ImportOrder?> GetProjectedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportOrderListItemDto>> ListAsync(ImportOrderListQuery query, CancellationToken cancellationToken = default);
    Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<decimal?> GetLastOrderedQuantityAsync(Guid productId, CancellationToken cancellationToken = default);
}
