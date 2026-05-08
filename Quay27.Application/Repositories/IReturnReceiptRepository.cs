using Quay27.Application.Purchasing;
using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IReturnReceiptRepository
{
    Task AddAsync(ReturnReceipt entity, CancellationToken cancellationToken = default);
    Task<ReturnReceipt?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReturnReceipt?> GetProjectedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnReceiptListItemDto>> ListAsync(ReceiptListQuery query, CancellationToken cancellationToken = default);
    Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken = default);
}
