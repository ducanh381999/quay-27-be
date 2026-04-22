using Microsoft.EntityFrameworkCore;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;

namespace Quay27.Infrastructure.Repositories;

public class PriceListRepository : IPriceListRepository
{
    private readonly ApplicationDbContext _db;

    public PriceListRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PriceList>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PriceLists.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term));
        }

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<PriceList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.PriceLists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<PriceList?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.PriceLists.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.PriceLists.AnyAsync(
            x => !x.IsDeleted && x.Name == name && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task AddAsync(PriceList entity, CancellationToken cancellationToken = default) =>
        _db.PriceLists.AddAsync(entity, cancellationToken).AsTask();
}
