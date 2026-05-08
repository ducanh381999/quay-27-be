using Quay27.Application.Purchasing;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IGoodsReceiptRepository
{
    Task AddAsync(GoodsReceipt entity, CancellationToken cancellationToken = default);
    Task<GoodsReceipt?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GoodsReceipt?> GetProjectedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoodsReceiptListItemDto>> ListAsync(ReceiptListQuery query, CancellationToken cancellationToken = default);
    Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default);
}
