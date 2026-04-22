using Microsoft.EntityFrameworkCore;
using Quay27.Application.Products;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _db;

    public ProductRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<Product> Items, int Total)> ListAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.Products
            .AsNoTracking()
            .Include(x => x.Group)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.GroupId) && query.GroupId != "all")
        {
            var group = query.GroupId.Trim();
            var hasGroupGuid = Guid.TryParse(group, out var groupGuid);
            q = q.Where(x =>
                (x.Group != null && x.Group.Name.Contains(group)) ||
                (hasGroupGuid && x.GroupId == groupGuid));
        }

        if (query.Stock == "in_stock")
            q = q.Where(x => x.Stock > 0);
        else if (query.Stock == "out_of_stock")
            q = q.Where(x => x.Stock <= 0);

        if (query.DirectSale == "yes")
            q = q.Where(x => x.DirectSale);
        else if (query.DirectSale == "no")
            q = q.Where(x => !x.DirectSale);

        if (!string.IsNullOrWhiteSpace(query.Status) && query.Status != "all")
            q = q.Where(x => x.RowStatus == query.Status);

        if (query.CreatedFrom.HasValue) q = q.Where(x => x.CreatedAt >= query.CreatedFrom.Value);
        if (query.CreatedTo.HasValue) q = q.Where(x => x.CreatedAt <= query.CreatedTo.Value);
        if (query.ExpectedFrom.HasValue) q = q.Where(x => x.ExpectedStockoutAt >= query.ExpectedFrom.Value);
        if (query.ExpectedTo.HasValue) q = q.Where(x => x.ExpectedStockoutAt <= query.ExpectedTo.Value);

        var total = await q.CountAsync(cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 100 : Math.Min(query.PageSize, 500);

        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Products.AsNoTracking().Include(x => x.Group).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Product>> ListAllActiveAsync(CancellationToken cancellationToken = default) =>
        await _db.Products
            .AsNoTracking()
            .Include(x => x.Group)
            .Where(x => !x.IsDeleted && x.RowStatus == "active")
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> ListByGroupIdsAsync(
        IReadOnlyList<Guid> groupIds,
        CancellationToken cancellationToken = default)
    {
        if (groupIds.Count == 0)
        {
            return Array.Empty<Product>();
        }

        return await _db.Products
            .AsNoTracking()
            .Include(x => x.Group)
            .Where(x => !x.IsDeleted && x.RowStatus == "active" && x.GroupId.HasValue && groupIds.Contains(x.GroupId.Value))
            .ToListAsync(cancellationToken);
    }

    public Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Products.Include(x => x.Group).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        _db.Products.AnyAsync(x => !x.IsDeleted && x.Code == code && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

    public Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        _db.Products.AnyAsync(x => !x.IsDeleted && x.Barcode == barcode && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        _db.Products.AddAsync(product, cancellationToken).AsTask();
}
