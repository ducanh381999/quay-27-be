using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IPriceListItemRepository
{
    Task<IReadOnlyList<PriceListItem>> ListByPriceListIdsAsync(
        IReadOnlyList<Guid> priceListIds,
        string? search,
        string? groupId,
        string? stock,
        CancellationToken cancellationToken = default);

    Task<PriceListItem?> GetTrackedAsync(
        Guid priceListId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyList<PriceListItem> items,
        CancellationToken cancellationToken = default);
}
