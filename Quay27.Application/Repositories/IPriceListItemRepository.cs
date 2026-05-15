using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IPriceListItemRepository
{
    Task<IReadOnlyList<PriceListItem>> ListByPriceListIdsAsync(
        IReadOnlyList<Guid> priceListIds,
        string? search,
        string? groupId,
        string? stock,
        IReadOnlyList<Guid>? filterGroupIds,
        CancellationToken cancellationToken = default);

    Task<PriceListItem?> GetTrackedAsync(
        Guid priceListId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyList<PriceListItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>Map product id to list price for the given price list (missing products omitted).</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetPricesByProductIdsAsync(
        Guid priceListId,
        IReadOnlyList<Guid> productIds,
        CancellationToken cancellationToken = default);
}
