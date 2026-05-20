using Quay27.Application.Orders;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface ISalesReturnRepository
{
    Task<IReadOnlyList<SalesReturnListItemDto>> ListAsync(SalesReturnListQuery query,
        CancellationToken cancellationToken = default);

    Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default);

    Task AddAsync(SalesReturn entity, CancellationToken cancellationToken = default);

    Task<SalesReturn?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SalesReturn?> GetByIdNoTrackingAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SalesReturnDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesReturnCashbookRowDto>> ListCashbookEntriesAsync(Guid returnId,
        CancellationToken cancellationToken = default);
}
