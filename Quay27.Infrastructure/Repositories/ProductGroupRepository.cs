using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class ProductGroupRepository : IProductGroupRepository
{
    private readonly ApplicationDbContext _db;

    public ProductGroupRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProductGroup>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.ProductGroups.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return items;
    }

    public Task<ProductGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _db.ProductGroups.FirstOrDefaultAsync(x => !x.IsDeleted && x.Name == name.Trim(), cancellationToken);

    public Task<ProductGroup?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ProductGroups.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task AddAsync(ProductGroup group, CancellationToken cancellationToken = default) =>
        _db.ProductGroups.AddAsync(group, cancellationToken).AsTask();
}
