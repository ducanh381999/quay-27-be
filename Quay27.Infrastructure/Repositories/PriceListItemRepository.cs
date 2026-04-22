using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class PriceListItemRepository : IPriceListItemRepository
{
    private readonly ApplicationDbContext _db;

    public PriceListItemRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PriceListItem>> ListByPriceListIdsAsync(
        IReadOnlyList<Guid> priceListIds,
        string? search,
        string? groupId,
        string? stock,
        CancellationToken cancellationToken = default)
    {
        if (priceListIds.Count == 0)
        {
            return Array.Empty<PriceListItem>();
        }

        var query = _db.PriceListItems
            .AsNoTracking()
            .Include(x => x.Product)!.ThenInclude(x => x!.Group)
            .Where(x =>
                priceListIds.Contains(x.PriceListId) &&
                x.Product != null &&
                !x.Product.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Product!.Code.Contains(term) ||
                x.Product.Name.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(groupId) && groupId != "all")
        {
            var term = groupId.Trim();
            var hasGuid = Guid.TryParse(term, out var groupGuid);
            query = query.Where(x =>
                (x.Product!.Group != null && x.Product.Group.Name.Contains(term)) ||
                (hasGuid && x.Product!.GroupId == groupGuid));
        }

        if (stock == "in_stock")
            query = query.Where(x => x.Product!.Stock > 0);
        else if (stock == "out_of_stock")
            query = query.Where(x => x.Product!.Stock <= 0);

        return await query.ToListAsync(cancellationToken);
    }

    public Task<PriceListItem?> GetTrackedAsync(
        Guid priceListId,
        Guid productId,
        CancellationToken cancellationToken = default) =>
        _db.PriceListItems.FirstOrDefaultAsync(
            x => x.PriceListId == priceListId && x.ProductId == productId,
            cancellationToken);

    public Task AddRangeAsync(
        IReadOnlyList<PriceListItem> items,
        CancellationToken cancellationToken = default) =>
        _db.PriceListItems.AddRangeAsync(items, cancellationToken);
}
